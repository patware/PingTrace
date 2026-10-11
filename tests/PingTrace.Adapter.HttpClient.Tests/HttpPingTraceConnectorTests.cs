using System.Net;
using System.Text;
using System.Text.Json;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using PingTrace.Core;
using PingTrace.Runtime;

namespace PingTrace.Adapter.HttpClient.Tests;

/// <summary>
/// Verifies HTTP destinations through their public registration API.
/// </summary>
/// <remarks>
/// In-memory handlers exercise client configuration and deserialization
/// without depending on a running server.
/// </remarks>
[TestClass]
public sealed class HttpPingTraceConnectorTests
{
    /// <summary>
    /// Gets or sets the context supplied by MSTest.
    /// </summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// Verifies independent HTTP clients and preservation of remote names.
    /// </summary>
    [TestMethod]
    public async Task MultipleDestinations_UseTheirOwnClients()
    {
        // Arrange
        ServiceCollection services = new();
        List<string> requests = new();

        services.AddPingTrace(builder =>
        {
            builder.AddHttpDestination("First", client =>
            {
                client.BaseAddress = new Uri("https://first.test/");
            }).ConfigurePrimaryHttpMessageHandler(() =>
                new StubHandler((request, _) =>
                {
                    requests.Add(request.RequestUri!.AbsoluteUri);
                    request.Method.Should().Be(HttpMethod.Get);
                    return Task.FromResult(JsonResponse(
                        """{"name":"FirstRemote","payload":"one"}"""));
                }));

            builder.AddHttpDestination("Second", client =>
            {
                client.BaseAddress = new Uri("https://second.test/");
            }).ConfigurePrimaryHttpMessageHandler(() =>
                new StubHandler((request, _) =>
                {
                    requests.Add(request.RequestUri!.AbsoluteUri);
                    return Task.FromResult(JsonResponse(
                        """{"name":"SecondRemote","payload":"two"}"""));
                }));
        });

        using ServiceProvider provider = BuildProvider(services);
        using IServiceScope scope = provider.CreateScope();
        IPingTrace sut = scope.ServiceProvider.GetRequiredService<IPingTrace>();

        // Act
        IReadOnlyList<TraceResult> first = await sut.TraceAsync("FIRST", TestContext.CancellationToken);
        IReadOnlyList<TraceResult> second = await sut.TraceAsync("Second", TestContext.CancellationToken);

        // Assert
        requests.Should().Equal(
            "https://first.test/pingtrace/Trace",
            "https://second.test/pingtrace/Trace");

        first.Should().HaveCount(2);
        first[1].Name.Should().Be("FirstRemote");
        first[1].Payload.Should().Be("one");

        second.Should().HaveCount(2);
        second[1].Name.Should().Be("SecondRemote");
        second[1].Payload.Should().Be("two");
    }

    /// <summary>
    /// Verifies that Ping and local Trace send no HTTP requests.
    /// </summary>
    [TestMethod]
    public async Task LocalOperations_DoNotSendHttpRequests()
    {
        // Arrange
        int requestCount = 0;
        using ServiceProvider provider = CreateProvider((_, _) =>
        {
            requestCount++;
            return Task.FromResult(JsonResponse("""{"name":"Remote"}"""));
        });

        using IServiceScope scope = provider.CreateScope();
        IPingTrace sut = scope.ServiceProvider.GetRequiredService<IPingTrace>();

        // Act
        string ping = sut.Ping();
        TraceResult local = await sut.TraceAsync(TestContext.CancellationToken);

        // Assert
        ping.Should().Be("Pong");
        local.Name.Should().NotBeNullOrWhiteSpace();
        requestCount.Should().Be(0);
    }

    /// <summary>
    /// Verifies server-root and base-path-relative endpoint resolution.
    /// </summary>
    /// <param name="path">The configured trace path.</param>
    /// <param name="expectedUri">The expected outgoing URI.</param>
    [TestMethod]
    [DataRow("/pingtrace/Trace", "https://remote.test/pingtrace/Trace")]
    [DataRow("diagnostics/Trace", "https://remote.test/application/diagnostics/Trace")]
    public async Task TracePath_ResolvesAgainstBaseAddress(
        string path,
        string expectedUri)
    {
        // Arrange
        Uri? observedUri = null;
        using ServiceProvider provider = CreateProvider(
            (request, _) =>
            {
                observedUri = request.RequestUri;
                return Task.FromResult(JsonResponse("""{"name":"Remote"}"""));
            },
            path);

        using IServiceScope scope = provider.CreateScope();
        IPingTrace sut = scope.ServiceProvider.GetRequiredService<IPingTrace>();

        // Act
        await sut.TraceAsync("ApiService", TestContext.CancellationToken);

        // Assert
        observedUri.Should().Be(new Uri(expectedUri));
    }

    /// <summary>
    /// Verifies that unsuccessful responses remain HTTP failures.
    /// </summary>
    /// <param name="statusCode">The remote failure status.</param>
    [TestMethod]
    [DataRow(401)]
    [DataRow(404)]
    [DataRow(503)]
    public async Task UnsuccessfulResponse_ThrowsHttpRequestException(int statusCode)
    {
        // Arrange
        using ServiceProvider provider = CreateProvider((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)statusCode)));

        using IServiceScope scope = provider.CreateScope();
        IPingTrace sut = scope.ServiceProvider.GetRequiredService<IPingTrace>();

        // Act
        Func<Task> act = () => sut.TraceAsync("ApiService", TestContext.CancellationToken);

        // Assert
        var assertion =
            await act.Should().ThrowAsync<HttpRequestException>();

        assertion.Which.StatusCode.Should().Be((HttpStatusCode)statusCode);
    }

    /// <summary>
    /// Verifies rejection of missing or unnamed remote results.
    /// </summary>
    /// <param name="json">The invalid response body.</param>
    [TestMethod]
    [DataRow("null")]
    [DataRow("{}")]
    [DataRow("{\"name\":\" \"}")]
    public async Task InvalidResult_IsRejected(string json)
    {
        // Arrange
        using ServiceProvider provider = CreateProvider((_, _) => Task.FromResult(JsonResponse(json)));

        using IServiceScope scope = provider.CreateScope();
        IPingTrace sut = scope.ServiceProvider.GetRequiredService<IPingTrace>();

        // Act
        Func<Task> act = () => sut.TraceAsync("ApiService", TestContext.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Verifies that malformed JSON is not accepted as a successful trace.
    /// </summary>
    [TestMethod]
    public async Task MalformedJson_IsRejected()
    {
        // Arrange
        using ServiceProvider provider = CreateProvider((_, _) => Task.FromResult(JsonResponse("not-json")));

        using IServiceScope scope = provider.CreateScope();
        IPingTrace sut = scope.ServiceProvider.GetRequiredService<IPingTrace>();

        // Act
        Func<Task> act = () => sut.TraceAsync("ApiService", TestContext.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<JsonException>();
    }

    /// <summary>
    /// Verifies cancellation of a request already executing in the handler.
    /// </summary>
    /// <remarks>
    /// The handler signals when the request starts. Cancellation therefore
    /// occurs during transport execution rather than before invocation.
    /// </remarks>
    [TestMethod]
    public async Task Cancellation_ReachesExecutingHttpRequest()
    {
        // Arrange
        TaskCompletionSource<bool> entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

        using CancellationTokenSource cancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                TestContext.CancellationToken);

        using ServiceProvider provider = CreateProvider(async (_, token) =>
        {
            entered.TrySetResult(true);
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return JsonResponse("""{"name":"Remote"}""");
        });

        using IServiceScope scope = provider.CreateScope();
        IPingTrace sut = scope.ServiceProvider.GetRequiredService<IPingTrace>();

        // Act
        Task<IReadOnlyList<TraceResult>> pending = sut.TraceAsync("ApiService", cancellation.Token);

        try
        {
            await entered.Task.WaitAsync(
                TimeSpan.FromSeconds(5),
                TestContext.CancellationToken);
        }
        finally
        {
            cancellation.Cancel();
        }

        Func<Task> act = async () =>
        {
            await pending.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        };

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Verifies that missing client configuration fails before sending.
    /// </summary>
    [TestMethod]
    public async Task MissingBaseAddress_IsRejectedBeforeSending()
    {
        // Arrange
        ServiceCollection services = new();
        int requestCount = 0;

        services.AddPingTrace(builder =>
        {
            builder.AddHttpDestination("ApiService", _ => { })
                .ConfigurePrimaryHttpMessageHandler(() =>
                    new StubHandler((_, _) =>
                    {
                        requestCount++;
                        return Task.FromResult(JsonResponse(
                            """{"name":"Remote"}"""));
                    }));
        });

        using ServiceProvider provider = BuildProvider(services);
        using IServiceScope scope = provider.CreateScope();
        IPingTrace sut = scope.ServiceProvider.GetRequiredService<IPingTrace>();

        // Act
        Func<Task> act = () => sut.TraceAsync("ApiService", TestContext.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        requestCount.Should().Be(0);
    }

    /// <summary>
    /// Verifies rejection of paths that can select another server.
    /// </summary>
    /// <param name="path">The invalid endpoint path.</param>
    [TestMethod]
    [DataRow("https://other.test/Trace")]
    [DataRow("//other.test/Trace")]
    [DataRow(@"diagnostics\Trace")]
    public void InvalidTracePath_IsRejected(string path)
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        Action act = () => services.AddPingTrace(builder =>
        {
            builder.AddHttpDestination("ApiService", client =>
            {
                client.BaseAddress = new Uri("https://remote.test/");
            }, path);
        });

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tracePath");
    }

    private static ServiceProvider CreateProvider(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send, string tracePath = "/pingtrace/Trace")
    {
        ServiceCollection services = new();

        services.AddPingTrace(builder =>
        {
            builder.AddHttpDestination("ApiService", client =>
            {
                client.BaseAddress =
                    new Uri("https://remote.test/application/");
            }, tracePath)
            .ConfigurePrimaryHttpMessageHandler(() => new StubHandler(send));
        });

        return BuildProvider(services);
    }

    private static ServiceProvider BuildProvider(IServiceCollection services)
    {
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return send(request, cancellationToken);
        }
    }
}
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using PingTrace.Adapter.HttpClient;
using PingTrace.Core;
using PingTrace.Runtime;

namespace PingTrace.AspNetCore.Tests;

/// <summary>
/// Verifies HTTP endpoints using an in-memory ASP.NET Core server.
/// </summary>
[TestClass]
public sealed class PingTraceEndpointTests
{
    /// <summary>
    /// Gets or sets the context supplied by MSTest.
    /// </summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// Verifies the Ping status, media type, and response body.
    /// </summary>
    [TestMethod]
    public async Task Ping_ReturnsPlainTextPong()
    {
        // Arrange
        await using WebApplication app = await CreateApplicationAsync();
        using HttpClient client = app.GetTestClient();

        // Act
        using HttpResponseMessage response = await client.GetAsync("/pingtrace/Ping", TestContext.CancellationToken);

        string body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
        body.Should().Be("Pong");
    }

    /// <summary>
    /// Verifies the current local-object and destination-array contract.
    /// </summary>
    [TestMethod]
    public async Task Trace_ReturnsCurrentResponseShapes()
    {
        // Arrange
        await using WebApplication app = await CreateApplicationAsync();
        using HttpClient client = app.GetTestClient();

        // Act
        using HttpResponseMessage localResponse = await client.GetAsync(
            "/pingtrace/Trace", TestContext.CancellationToken);

        string localBody = await localResponse.Content.ReadAsStringAsync(
            TestContext.CancellationToken);

        using JsonDocument localJson = JsonDocument.Parse(localBody);
        string name = localJson.RootElement.GetProperty("name").GetString()!;

        using HttpResponseMessage namedResponse = await client.GetAsync(
            $"/pingtrace/Trace?destination={Uri.EscapeDataString(name)}",
            TestContext.CancellationToken);

        string namedBody = await namedResponse.Content.ReadAsStringAsync(
            TestContext.CancellationToken);

        using JsonDocument namedJson = JsonDocument.Parse(namedBody);

        // Assert
        localResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        localJson.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
        name.Should().NotBeNullOrWhiteSpace();

        namedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        namedJson.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        namedJson.RootElement.GetArrayLength().Should().Be(1);
        namedJson.RootElement[0].GetProperty("name").GetString()
            .Should().Be(name);
    }

    /// <summary>
    /// Verifies destination validation and lookup status codes.
    /// </summary>
    /// <param name="query">The destination query string.</param>
    /// <param name="expectedStatus">The expected HTTP status code.</param>
    [TestMethod]
    [DataRow("?destination=", 400)]
    [DataRow("?destination=%20", 400)]
    [DataRow("?destination=Missing", 404)]
    public async Task InvalidDestination_ReturnsExpectedStatus(
        string query,
        int expectedStatus)
    {
        // Arrange
        await using WebApplication app = await CreateApplicationAsync();
        using HttpClient client = app.GetTestClient();

        // Act
        using HttpResponseMessage response = await client.GetAsync("/pingtrace/Trace" + query, TestContext.CancellationToken);

        // Assert
        ((int)response.StatusCode).Should().Be(expectedStatus);
    }

    /// <summary>
    /// Verifies that dependency failures reach host exception handling.
    /// </summary>
    /// <remarks>
    /// A test-only middleware produces status 500 when the endpoint lets
    /// the exception propagate. This detects an accidental catch-all 404.
    /// </remarks>
    [TestMethod]
    public async Task ConnectorFailure_IsNotReportedAsUnknownDestination()
    {
        // Arrange
        IPingTraceConnector connector = Substitute.For<IPingTraceConnector>();

        connector.Name.Returns("ApiService");
        connector.TraceAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<TraceResult>>(
                new HttpRequestException("Remote service failed.")));

        await using WebApplication app = await CreateApplicationAsync(
            services => services.AddScoped<IPingTraceConnector>(_ => connector),
            handleHttpFailure: true);

        using HttpClient client = app.GetTestClient();

        // Act
        using HttpResponseMessage response = await client.GetAsync("/pingtrace/Trace?destination=ApiService", TestContext.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    /// <summary>
    /// Verifies that a custom prefix replaces the default prefix.
    /// </summary>
    [TestMethod]
    public async Task CustomPrefix_MapsRequestedRoutes()
    {
        // Arrange
        await using WebApplication app = await CreateApplicationAsync(prefix: "/diagnostics");

        using HttpClient client = app.GetTestClient();

        // Act
        using HttpResponseMessage custom = await client.GetAsync("/diagnostics/Ping", TestContext.CancellationToken);

        using HttpResponseMessage original = await client.GetAsync("/pingtrace/Ping", TestContext.CancellationToken);

        // Assert
        custom.StatusCode.Should().Be(HttpStatusCode.OK);
        original.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Verifies that the HTTP adapter consumes the actual endpoint response.
    /// </summary>
    /// <remarks>
    /// Both applications run in the test process, so their entry-assembly
    /// names are identical. Payload markers distinguish their observations.
    /// </remarks>
    [TestMethod]
    public async Task HttpDestination_ConsumesRemoteEndpoint()
    {
        // Arrange
        TraceResult remoteResult = new()
        {
            Name = "RemoteApi",
            MachineName = "remote-machine",
            Identity = "remote-identity"
        };
        remoteResult.Finish("Remote payload");

        IPingTrace remoteService = Substitute.For<IPingTrace>();
        remoteService.TraceAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(remoteResult));

        await using WebApplication remote = await CreateApplicationAsync(
            services => services.AddScoped<IPingTrace>(_ => remoteService));

        await using WebApplication frontend = await CreateApplicationAsync(
            services => services.AddPingTrace(builder =>
            {
                builder.AddHttpDestination("ApiService", client =>
                {
                    client.BaseAddress = new Uri("http://remote.test/");
                }).ConfigurePrimaryHttpMessageHandler(
                    () => remote.GetTestServer().CreateHandler());
            }));

        using HttpClient client = frontend.GetTestClient();

        // Act
        using HttpResponseMessage response = await client.GetAsync("/pingtrace/Trace?destination=ApiService", TestContext.CancellationToken);

        TraceResult[]? results =
            await response.Content.ReadFromJsonAsync<TraceResult[]>(
                cancellationToken: TestContext.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        results.Should().NotBeNull();
        results!.Should().HaveCount(2);
        results[0].Payload.Should().Be("Pong [ApiService]");
        results[1].Name.Should().Be("RemoteApi");
        results[1].MachineName.Should().Be("remote-machine");
        results[1].Identity.Should().Be("remote-identity");
        results[1].Payload.Should().Be("Remote payload");

        await remoteService.Received(1)
            .TraceAsync(Arg.Any<CancellationToken>());
    }

    private async Task<WebApplication> CreateApplicationAsync(
        Action<IServiceCollection>? configure = null,
        string prefix = "/pingtrace",
        bool handleHttpFailure = false)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        configure?.Invoke(builder.Services);
        builder.Services.AddPingTrace();

        WebApplication app = builder.Build();

        if (handleHttpFailure)
        {
            app.Use(async (context, next) =>
            {
                try
                {
                    await next(context);
                }
                catch (HttpRequestException)
                {
                    context.Response.StatusCode =
                        StatusCodes.Status500InternalServerError;
                }
            });
        }

        app.MapPingTrace(prefix);

        try
        {
            await app.StartAsync(TestContext.CancellationToken);
            return app;
        }
        catch
        {
            await app.DisposeAsync();
            throw;
        }
    }
}
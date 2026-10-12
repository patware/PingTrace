using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using PingTrace.Core;

namespace PingTrace.Runtime.Tests;

/// <summary>
/// Verifies PingTrace dependency-injection registration.
/// </summary>
[TestClass]
public sealed class PingTraceRegistrationTests
{
    /// <summary>
    /// Gets or sets the context supplied by MSTest.
    /// </summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// Verifies parameterless registration and scoped resolution.
    /// </summary>
    [TestMethod]
    public void ParameterlessRegistration_ProvidesScopedDefault()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddPingTrace();

        using ServiceProvider provider = BuildProvider(services);
        using IServiceScope firstScope = provider.CreateScope();
        using IServiceScope secondScope = provider.CreateScope();

        // Act
        IPingTrace first = firstScope.ServiceProvider.GetRequiredService<IPingTrace>();
        IPingTrace firstAgain = firstScope.ServiceProvider.GetRequiredService<IPingTrace>();
        IPingTrace second = secondScope.ServiceProvider.GetRequiredService<IPingTrace>();

        // Assert
        first.Should().BeOfType<DefaultPingTrace>();
        firstAgain.Should().BeSameAs(first);
        second.Should().NotBeSameAs(first);
        first.Ping().Should().Be("Pong");
    }

    /// <summary>
    /// Verifies that the callback registers dependencies used by the runtime.
    /// </summary>
    [TestMethod]
    public async Task RegistrationCallback_AddsUsableConnector()
    {
        // Arrange
        ServiceCollection services = new();
        IPingTraceConnector connector = Substitute.For<IPingTraceConnector>();

        connector.Name.Returns("ApiService");

        TraceResult remote = new() { Name = "RemoteApi" };
        remote.Finish("Remote payload");

        connector.TraceAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TraceResult>>(
                new[] { remote }));

        int callbackCount = 0;
        IServiceCollection? configuredServices = null;

        // Act
        IServiceCollection returned = services.AddPingTrace(builder =>
        {
            callbackCount++;
            configuredServices = builder.Services;
            builder.Services.AddScoped<IPingTraceConnector>(_ => connector);
        });

        using ServiceProvider provider = BuildProvider(services);
        using IServiceScope scope = provider.CreateScope();

        IReadOnlyList<TraceResult> results =
            await scope.ServiceProvider.GetRequiredService<IPingTrace>()
                .TraceAsync("ApiService", TestContext.CancellationToken);

        // Assert
        returned.Should().BeSameAs(services);
        configuredServices.Should().BeSameAs(services);
        callbackCount.Should().Be(1);
        results.Should().HaveCount(2);
        results[1].Should().BeSameAs(remote);
    }

    /// <summary>
    /// Verifies that repeated registration adds only one default service.
    /// </summary>
    [TestMethod]
    public void RepeatedRegistration_DoesNotDuplicateDefault()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddPingTrace();
        services.AddPingTrace();

        using ServiceProvider provider = BuildProvider(services);
        using IServiceScope scope = provider.CreateScope();

        IPingTrace[] implementations = scope.ServiceProvider.GetServices<IPingTrace>().ToArray();

        // Assert
        implementations.Should().ContainSingle()
            .Which.Should().BeOfType<DefaultPingTrace>();
    }

    /// <summary>
    /// Verifies preservation of an application-provided implementation.
    /// </summary>
    [TestMethod]
    public void ExistingImplementation_IsPreserved()
    {
        // Arrange
        ServiceCollection services = new();
        IPingTrace custom = Substitute.For<IPingTrace>();
        services.AddScoped<IPingTrace>(_ => custom);

        // Act
        services.AddPingTrace();

        using ServiceProvider provider = BuildProvider(services);
        using IServiceScope scope = provider.CreateScope();

        IPingTrace resolved = scope.ServiceProvider.GetRequiredService<IPingTrace>();

        // Assert
        resolved.Should().BeSameAs(custom);
    }

    /// <summary>
    /// Verifies immediate rejection of a missing service collection.
    /// </summary>
    [TestMethod]
    public void NullServices_AreRejected()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        Action act = () => services.AddPingTrace();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    /// <summary>
    /// Verifies that different request scopes use the same traversal identity.
    /// </summary>
    /// <remarks>
    /// Cycle detection must survive a new HTTP request scope. Merely resolving
    /// a singleton node would not prove that DefaultPingTrace uses that node.
    /// </remarks>
    [TestMethod]
    public async Task TraceAll_NewScope_RecognizesPreviouslyVisitedNode()
    {
        // Arrange
        ServiceCollection services = new();

        IRecursivePingTraceConnector connector = Substitute.For<IRecursivePingTraceConnector>();
        connector.Name.Returns("RemoteApi");

        TraceAllRequest? forwarded = null;

        connector.TraceAllAsync(
                Arg.Any<TraceAllRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                forwarded = call.Arg<TraceAllRequest>();

                return Task.FromResult<IReadOnlyList<TraceResult>>(
                    new[] { new TraceResult { Name = "RemoteApi" } });
            });

        services.AddPingTrace(builder => builder.Services.AddScoped<IPingTraceConnector>(_ => connector));

        using ServiceProvider provider = BuildProvider(services);
        using IServiceScope firstScope = provider.CreateScope();
        using IServiceScope secondScope = provider.CreateScope();

        IPingTrace first = firstScope.ServiceProvider.GetRequiredService<IPingTrace>();
        IPingTrace second = secondScope.ServiceProvider.GetRequiredService<IPingTrace>();

        // Act
        await first.TraceAllAsync(cancellationToken: TestContext.CancellationToken);

        Func<Task> revisit = () => second.TraceAllAsync(forwarded, TestContext.CancellationToken);

        // Assert
        forwarded.Should().NotBeNull();
        first.Should().NotBeSameAs(second);

        await revisit.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*cycle*");

        await connector.Received(1).TraceAllAsync(
            Arg.Any<TraceAllRequest>(),
            Arg.Any<CancellationToken>());
    }

    private static ServiceProvider BuildProvider(IServiceCollection services)
    {
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
    }
}
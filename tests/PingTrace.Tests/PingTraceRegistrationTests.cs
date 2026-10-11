using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NSubstitute;

using PingTrace.Core;
using PingTrace.Runtime;

namespace PingTrace.Tests;

[TestClass]
public sealed class PingTraceRegistrationTests
{
    private static readonly Guid ApplicationId = Guid.Parse("A110F236-5A01-440D-B5F1-12D8901642D3");

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task AddPingTrace_ResolvesConfiguredImplementation()
    {
        ServiceCollection services = new();

        services.AddPingTrace(Configure);

        using ServiceProvider provider = BuildProvider(services);
        using IServiceScope scope = provider.CreateScope();

        IPingTrace sut = scope.ServiceProvider
          .GetRequiredService<IPingTrace>();

        TraceResult result = await sut.TraceAsync(TestContext.CancellationToken);

        sut.Should().BeOfType<DefaultPingTrace>();
        result.Name.Should().Be("TestApp");
        result.Payload.Should().Be("Pong");
    }

    [TestMethod]
    public void AddPingTrace_UsesScopedLifetime()
    {
        ServiceCollection services = new();

        services.AddPingTrace(Configure);

        using ServiceProvider provider = BuildProvider(services);
        using IServiceScope firstScope = provider.CreateScope();
        using IServiceScope secondScope = provider.CreateScope();

        IPingTrace first = firstScope.ServiceProvider.GetRequiredService<IPingTrace>();

        IPingTrace firstAgain = firstScope.ServiceProvider.GetRequiredService<IPingTrace>();

        IPingTrace second = secondScope.ServiceProvider.GetRequiredService<IPingTrace>();

        firstAgain.Should().BeSameAs(first);
        second.Should().NotBeSameAs(first);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddPingTrace_RejectsInvalidOptions(bool missingId)
    {
        ServiceCollection services = new();

        services.AddPingTrace(options =>
        {
            options.Name = missingId ? "TestApp" : " ";
        });

        using ServiceProvider provider = BuildProvider(services);
        using IServiceScope scope = provider.CreateScope();

        Action act = () =>
        {
            _ = scope.ServiceProvider.GetRequiredService<IPingTrace>();
        };

        act.Should().Throw<OptionsValidationException>();
    }

    [TestMethod]
    public async Task AddDestination_ResolvesDependenciesAndParticipatesInTrace()
    {
        ServiceCollection services = new();

        services.AddScoped<DestinationDependency>();
        services.AddPingTrace(Configure);
        services.AddPingTraceDestination<TestDestination>();

        using ServiceProvider provider = BuildProvider(services);
        using IServiceScope scope = provider.CreateScope();

        IPingTrace sut = scope.ServiceProvider.GetRequiredService<IPingTrace>();

        IReadOnlyList<TraceResult> results = await sut.TraceAsync("Database1", TestContext.CancellationToken);

        results.Select(result => result.Name)
          .Should().Equal("TestApp", "Database1");

        results[1].Payload.Should().Be("Dependency resolved");
    }

    [TestMethod]
    public void AddDestination_DoesNotRegisterSameTypeTwice()
    {
        ServiceCollection services = new();

        services.AddScoped<DestinationDependency>();
        services.AddPingTrace(Configure);
        services.AddPingTraceDestination<TestDestination>();
        services.AddPingTraceDestination<TestDestination>();

        using ServiceProvider provider = BuildProvider(services);
        using IServiceScope scope = provider.CreateScope();

        IEnumerable<IPingTraceDestination> destinations = scope.ServiceProvider.GetServices<IPingTraceDestination>();

        destinations.Should().ContainSingle();
    }

    [TestMethod]
    public void AddPingTrace_PreservesExistingImplementation()
    {
        IPingTrace custom = Substitute.For<IPingTrace>();

        ServiceCollection services = new();

        services.AddScoped<IPingTrace>(_ => custom);
        services.AddPingTrace(Configure);

        using ServiceProvider provider = BuildProvider(services);
        using IServiceScope scope = provider.CreateScope();

        IPingTrace resolved = scope.ServiceProvider.GetRequiredService<IPingTrace>();

        resolved.Should().BeSameAs(custom);
    }

    private static void Configure(PingTraceOptions options)
    {
        options.Name = "TestApp";
    }

    private static ServiceProvider BuildProvider(IServiceCollection services)
    {
        return services.BuildServiceProvider(
          new ServiceProviderOptions
          {
              ValidateScopes = true,
              ValidateOnBuild = true
          });
    }

    public sealed class DestinationDependency
    {
        public string Payload => "Dependency resolved";
    }

    public sealed class TestDestination(DestinationDependency dependency) : IPingTraceDestination
    {
        public string Name => "Database1";

        public Task<IReadOnlyList<TraceResult>> TraceAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TraceResult result = new()
            {
                Name = Name
            };

            result.Finish(dependency.Payload);

            return Task.FromResult<IReadOnlyList<TraceResult>>([result]);
        }
    }
}
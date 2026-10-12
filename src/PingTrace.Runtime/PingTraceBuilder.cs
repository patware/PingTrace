using Microsoft.Extensions.DependencyInjection;

namespace PingTrace.Runtime;

/// <summary>
/// Provides access to services while registering PingTrace adapters.
/// </summary>
/// <remarks>
/// This builder is used during application startup. It is not a runtime
/// options object and does not execute diagnostic requests.
/// </remarks>
public sealed class PingTraceBuilder
{
    internal PingTraceBuilder(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    /// <summary>
    /// Gets the service collection used to register adapters and their
    /// dependencies.
    /// </summary>
    public IServiceCollection Services { get; }
}
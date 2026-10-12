using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using PingTrace.Core;
using PingTrace.Runtime;

namespace PingTrace.Adapter.EntityFramework;

/// <summary>
/// Registers Entity Framework Core destinations for PingTrace.
/// </summary>
public static class EntityFrameworkExtensions
{
    /// <summary>
    /// Registers a named destination that checks database connectivity
    /// through an existing DbContext registration.
    /// </summary>
    /// <typeparam name="TContext">
    /// The registered DbContext type used to access the database.
    /// </typeparam>
    /// <param name="builder">The PingTrace registration builder.</param>
    /// <param name="name">The destination name used when tracing.</param>
    /// <returns>The builder for further destination registrations.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="name"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is empty or consists only of whitespace.
    /// </exception>
    /// <remarks>
    /// Register TContext separately with its database provider and options.
    /// The connector shares the application's dependency-injection scope.
    /// Context resolution is deferred until this destination is traced,
    /// so local diagnostics do not construct database contexts.
    /// The dependency-injection container owns context disposal.
    /// </remarks>
    public static PingTraceBuilder AddDbContextDestination<TContext>(
        this PingTraceBuilder builder,
        string name)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        builder.Services.AddScoped<IPingTraceConnector>(provider =>
            new DbContextPingTraceConnector<TContext>(
                name,
                () => provider.GetRequiredService<TContext>()));

        return builder;
    }
}
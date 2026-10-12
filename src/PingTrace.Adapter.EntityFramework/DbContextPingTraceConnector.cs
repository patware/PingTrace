using Microsoft.EntityFrameworkCore;

using PingTrace.Core;

namespace PingTrace.Adapter.EntityFramework;

/// <summary>
/// Queries SQL Server diagnostic values through a registered DbContext.
/// </summary>
/// <typeparam name="TContext">
/// The registered context configured to use SQL Server.
/// </typeparam>
/// <remarks>
/// The diagnostic query returns the server instance name, current login
/// name, and database name without reading application tables or changing
/// application data. Its SQL Server-specific functions limit provider
/// support despite the generic DbContext constraint.
///
/// The dependency-injection scope owns the context. Resolution is deferred
/// until tracing begins so local diagnostics do not construct the context.
/// The connector must not run concurrently with other operations using
/// the same context.
///
/// Timing is measured by the calling application and includes context
/// resolution and query execution.
/// </remarks>
internal sealed class DbContextPingTraceConnector<TContext> : IPingTraceConnector where TContext : DbContext
{
    private readonly Func<TContext> _resolveContext;

    internal DbContextPingTraceConnector(string name, Func<TContext> resolveContext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(resolveContext);

        Name = name;
        _resolveContext = resolveContext;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// The context cannot be resolved or configured, or the diagnostic query
    /// does not return exactly one result.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation is canceled.
    /// </exception>
    /// <remarks>
    /// Executes a SQL Server-specific diagnostic query without reading
    /// application tables or modifying the database.
    /// MachineName contains the server instance name, Identity contains
    /// the current login name, and Payload contains the database name.
    ///
    /// The dependency-injection scope owns the context. This connector
    /// neither disposes it nor supports concurrent use of that context.
    /// </remarks>
    public async Task<IReadOnlyList<TraceResult>> TraceAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        TraceResult result = new()
        {
            Name = Name,
            MachineName = string.Empty,
            Identity = string.Empty
        };


        TContext context = _resolveContext();

        cancellationToken.ThrowIfCancellationRequested();

        DatabaseDiagnostics diagnostics = await context.Database
            .SqlQuery<DatabaseDiagnostics>($"""
                SELECT
                    @@SERVERNAME AS [MachineName],
                    SUSER_NAME() AS [Identity],
                    DB_NAME() AS [Payload]
                """)
            .SingleAsync(cancellationToken);

        result.MachineName = diagnostics.MachineName ?? string.Empty;
        result.Identity = diagnostics.Identity ?? string.Empty;
        result.Finish(diagnostics.Payload ?? string.Empty);

        return [result];
    }
}
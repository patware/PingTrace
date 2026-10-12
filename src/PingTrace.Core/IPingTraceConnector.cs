using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PingTrace.Core;

/// <summary>
/// Invokes tracing on a configured dependency.
/// </summary>
/// <remarks>
/// Connectors own communication with dependencies. They do not add the
/// calling application's result; that responsibility belongs to the runtime.
/// </remarks>
public interface IPingTraceConnector
{
    /// <summary>
    /// Gets the destination name used by the calling application.
    /// </summary>
    /// <remarks>
    /// This is a local registration name and need not match the remote
    /// application's assembly name.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Requests diagnostic results from the dependency.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token used to cancel the operation.
    /// </param>
    /// <returns>The diagnostic results returned by the dependency.</returns>
    Task<IReadOnlyList<TraceResult>> TraceAsync(CancellationToken cancellationToken = default);
}
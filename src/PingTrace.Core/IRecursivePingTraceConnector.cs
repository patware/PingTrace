using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PingTrace.Core;

/// <summary>
/// Invokes recursive tracing on a participating dependency.
/// </summary>
/// <remarks>
/// Connectors implementing only IPingTraceConnector are treated as leaves.
/// This keeps database diagnostics and existing custom connectors usable
/// without requiring them to implement recursive traversal.
/// </remarks>
public interface IRecursivePingTraceConnector : IPingTraceConnector
{
    /// <summary>
    /// Recursively traces the dependency using the supplied traversal state.
    /// </summary>
    /// <param name="request">
    /// The remaining limits and ancestor path, including the caller.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The dependency's complete subtree.</returns>
    Task<IReadOnlyList<TraceResult>> TraceAllAsync(
        TraceAllRequest request,
        CancellationToken cancellationToken = default);
}
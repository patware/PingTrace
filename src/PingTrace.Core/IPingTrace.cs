#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PingTrace.Core;

/// <summary>
/// Provides local and dependency diagnostics for an application.
/// </summary>
public interface IPingTrace
{
    /// <summary>
    /// Returns the application's predictable response.
    /// </summary>
    /// <returns>The application response.</returns>
    string Ping();

    /// <summary>
    /// Traces the local application.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed local result.</returns>
    Task<TraceResult> TraceAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Traces the local application and the selected destination.
    /// </summary>
    /// <param name="destination">The destination name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The local result followed by destination results.</returns>
    Task<IReadOnlyList<TraceResult>> TraceAsync(
        string destination,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recursively traces the application and its registered destinations.
    /// </summary>
    /// <param name="request">
    /// The traversal state, or null to begin a new traversal.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The local result followed by each destination's subtree.
    /// </returns>
    /// <remarks>
    /// Destinations execute sequentially. The local elapsed time includes
    /// descendant execution. Failures, detected cycles, exhausted depth,
    /// and cancellation propagate instead of producing partial success.
    /// </remarks>
    Task<IReadOnlyList<TraceResult>> TraceAllAsync(
        TraceAllRequest? request = null,
        CancellationToken cancellationToken = default);
}
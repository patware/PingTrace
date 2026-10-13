namespace PingTrace.Adapter.ActiveDirectory;

/// <summary>
/// Reads diagnostic information from a configured directory connection.
/// </summary>
/// <remarks>
/// Applications may provide another implementation to reuse their existing
/// directory access layer. Sample applications can supply simulated results
/// without changing the PingTrace connector.
/// </remarks>
public interface IActiveDirectoryDiagnosticsReader
{
    /// <summary>
    /// Reads information about the connected domain.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token used to request cancellation.
    /// </param>
    /// <returns>The domain diagnostic information.</returns>
    /// <remarks>
    /// Cancellation capabilities depend on the underlying directory API.
    /// Implementations must document operations that cannot be interrupted.
    /// </remarks>
    Task<ActiveDirectoryDiagnostics> ReadDomainAsync(CancellationToken cancellationToken = default);
}
using System.Reflection;

using PingTrace.Core;

namespace PingTrace.Runtime;

/// <summary>
/// Provides local diagnostics and invokes explicitly registered dependencies.
/// </summary>
/// <remarks>
/// Ping and local Trace do not invoke connectors. Destination Trace includes
/// the local result followed by the selected connector's results.
/// </remarks>
public sealed class DefaultPingTrace : IPingTrace
{
    private readonly string _name =
        Assembly.GetEntryAssembly()?.GetName().Name
        ?? throw new InvalidOperationException(
            "The host assembly name could not be determined.");

    private readonly Dictionary<string, IPingTraceConnector> _connectors;

    /// <summary>
    /// Initializes a default implementation without dependencies.
    /// </summary>
    public DefaultPingTrace() : this(Array.Empty<IPingTraceConnector>())
    {
    }

    /// <summary>
    /// Initializes a default implementation with configured dependencies.
    /// </summary>
    /// <param name="connectors">
    /// The connectors available to this application.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// The collection or one of its connectors is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// A connector name is empty, duplicates another name, or conflicts
    /// with the local application name.
    /// </exception>
    /// <remarks>
    /// Names are compared without regard to case. Rejecting ambiguous names
    /// prevents a request from silently selecting the wrong dependency.
    /// Connector constructors should not perform network operations.
    /// </remarks>
    public DefaultPingTrace(IEnumerable<IPingTraceConnector> connectors)
    {
        ArgumentNullException.ThrowIfNull(connectors);

        _connectors = new Dictionary<string, IPingTraceConnector>(StringComparer.OrdinalIgnoreCase);

        foreach (IPingTraceConnector connector in connectors)
        {
            ArgumentNullException.ThrowIfNull(connector);
            ArgumentException.ThrowIfNullOrWhiteSpace(connector.Name);

            if (string.Equals(connector.Name, _name, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"Destination '{connector.Name}' conflicts with " +
                    "the local application name.",
                    nameof(connectors));
            }

            if (!_connectors.TryAdd(connector.Name, connector))
            {
                throw new ArgumentException(
                    $"Destination '{connector.Name}' is registered more than once.",
                    nameof(connectors));
            }
        }
    }

    /// <summary>
    /// Returns the predictable application response.
    /// </summary>
    /// <returns>The string <c>Pong</c>.</returns>
    public string Ping()
    {
        return "Pong";
    }

    /// <summary>
    /// Returns diagnostic information about this application.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token used to cancel the operation.
    /// </param>
    /// <returns>The completed local trace result.</returns>
    /// <remarks>
    /// The payload contains the managed entry assembly's version, allowing
    /// callers to identify the responding application build. This is the
    /// assembly version rather than the informational or package version.
    /// If unavailable, the payload falls back to Pong.
    /// </remarks>
    public Task<TraceResult> TraceAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        TraceResult current = CreateLocalResult();

        var payload = Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
            ?? "Pong";

        current.Finish(payload);

        return Task.FromResult(current);
    }

    /// <summary>
    /// Traces this application and the selected destination.
    /// </summary>
    /// <param name="destination">
    /// The local application name or a registered destination name.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel the operation.
    /// </param>
    /// <returns>
    /// The local result followed by any dependency results.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The destination is empty or consists only of whitespace.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// The destination is not registered and is not the local application.
    /// </exception>
    /// <remarks>
    /// A destination matching the local application name returns only the
    /// local result with its version payload.
    ///
    /// For a connector destination, the local payload contains the entry
    /// assembly version followed by " --> [destination]". The version portion
    /// falls back to Pong when unavailable.
    ///
    /// The local result is completed after the dependency responds so its
    /// elapsed time includes the dependency call. Connector results retain
    /// their own payloads. Failures and cancellation propagate to the caller.
    /// </remarks>
    public async Task<IReadOnlyList<TraceResult>> TraceAsync(string destination, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);

        if (string.Equals(destination, _name, StringComparison.OrdinalIgnoreCase))
        {
            TraceResult local = await TraceAsync(cancellationToken);
            return new[] { local };
        }

        if (!_connectors.TryGetValue(destination, out IPingTraceConnector? connector))
        {
            throw new KeyNotFoundException(
                $"Unknown trace destination '{destination}'.");
        }

        TraceResult current = CreateLocalResult();

        IReadOnlyList<TraceResult> downstream = await connector.TraceAsync(cancellationToken);

        List<TraceResult> results = new() { current };
        results.AddRange(downstream);

        var v = Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
                    ?? "Pong";

        current.Finish($"{v} --> [{destination}]");

        return results;
    }

    private TraceResult CreateLocalResult()
    {
        return new TraceResult
        {
            Name = _name,
            MachineName = Environment.MachineName,
            Identity = $"{Environment.UserDomainName}\\{Environment.UserName}"
        };
    }
}
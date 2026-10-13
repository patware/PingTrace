using System.Text.Json;

using PingTrace.Core;

namespace PingTrace.Adapter.ActiveDirectory;

/// <summary>
/// Maps directory diagnostics into a PingTrace dependency result.
/// </summary>
/// <remarks>
/// Reader resolution is deferred so Ping and local Trace do not construct
/// directory connections. The application owns the reader and its resources.
/// </remarks>
internal sealed class ActiveDirectoryPingTraceConnector
    : IPingTraceConnector
{
    private readonly Func<IActiveDirectoryDiagnosticsReader> _resolveReader;
    private readonly string _configuredIdentity;

    internal ActiveDirectoryPingTraceConnector(
        string name,
        Func<IActiveDirectoryDiagnosticsReader> resolveReader,
        string configuredIdentity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(resolveReader);
        ArgumentException.ThrowIfNullOrWhiteSpace(configuredIdentity);

        Name = name;
        _resolveReader = resolveReader;
        _configuredIdentity = configuredIdentity;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    /// <remarks>
    /// Identity is configuration metadata, not a directory-returned identity
    /// assertion. Failures propagate without conversion into successful traces.
    /// Timing includes reader resolution and the diagnostic operation.
    /// </remarks>
    public async Task<IReadOnlyList<TraceResult>> TraceAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        TraceResult result = new()
        {
            Name = Name,
            MachineName = string.Empty,
            Identity = _configuredIdentity
        };

        IActiveDirectoryDiagnosticsReader reader = _resolveReader();

        cancellationToken.ThrowIfCancellationRequested();

        ActiveDirectoryDiagnostics diagnostics = await reader.ReadDomainAsync(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        if (diagnostics is null ||
            string.IsNullOrWhiteSpace(diagnostics.DomainController) ||
            string.IsNullOrWhiteSpace(diagnostics.DomainDistinguishedName) ||
            string.IsNullOrWhiteSpace(diagnostics.DomainObjectName))
        {
            throw new InvalidOperationException(
                "The directory reader returned incomplete domain diagnostics.");
        }

        result.MachineName = diagnostics.DomainController;

        result.Finish(JsonSerializer.Serialize(new
        {
            diagnostics.DomainDistinguishedName,
            diagnostics.DomainObjectName,
            IdentitySource = "ConfiguredAuthentication",
            Probe = "DomainRead"
        }));

        return [result];
    }
}
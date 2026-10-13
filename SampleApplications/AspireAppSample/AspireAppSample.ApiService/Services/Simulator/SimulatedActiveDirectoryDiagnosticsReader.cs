using PingTrace.Adapter.ActiveDirectory;

namespace AspireAppSample.ApiService.Services.Simulator;

/// <summary>
/// Supplies directory diagnostics without a domain controller.
/// </summary>
/// <remarks>
/// Simulation belongs to the sample application. It exercises the real
/// connector but does not instantiate an LDAP connection or validate
/// LDAP authentication, queries, or response parsing.
/// </remarks>
internal sealed class SimulatedActiveDirectoryDiagnosticsReader
    : IActiveDirectoryDiagnosticsReader
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes the reader with the sample's configuration.
    /// </summary>
    /// <param name="configuration">The sample configuration.</param>
    public SimulatedActiveDirectoryDiagnosticsReader(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<ActiveDirectoryDiagnostics> ReadDomainAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int delayMilliseconds = _configuration.GetValue<int?>(
            "ActiveDirectorySimulation:DelayMilliseconds") ?? 100;

        if (delayMilliseconds < 0)
        {
            throw new InvalidOperationException(
                "ActiveDirectorySimulation:DelayMilliseconds must not be negative.");
        }

        await Task.Delay(delayMilliseconds, cancellationToken);

        string scenario =
            _configuration["ActiveDirectorySimulation:Scenario"] ?? "Success";

        return scenario.Trim().ToLowerInvariant() switch
        {
            "success" => new ActiveDirectoryDiagnostics
            {
                DomainController = "dc01.contoso.example",
                DomainDistinguishedName = "DC=contoso,DC=example",
                DomainObjectName = "contoso"
            },

            "incomplete" => new ActiveDirectoryDiagnostics
            {
                DomainController = string.Empty,
                DomainDistinguishedName = "DC=contoso,DC=example",
                DomainObjectName = "contoso"
            },

            "unauthorized" => throw new UnauthorizedAccessException(
                "Simulated directory access failure."),

            "unavailable" => throw new IOException(
                "Simulated domain controller unavailability."),

            _ => throw new InvalidOperationException(
                $"Unknown Active Directory simulation scenario '{scenario}'.")
        };
    }
}
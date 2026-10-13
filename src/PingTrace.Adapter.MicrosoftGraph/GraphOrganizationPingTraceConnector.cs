using System.Text.Json;

using Microsoft.Graph;

using PingTrace.Core;

namespace PingTrace.Adapter.MicrosoftGraph;

/// <summary>
/// Reads organization diagnostics using an existing Microsoft Graph client.
/// </summary>
/// <remarks>
/// Reusing the application's client exercises its configured connection.
/// Client resolution is deferred until tracing begins, matching the
/// Entity Framework adapter's behavior.
///
/// A successful organization read does not establish access to Teams
/// resources or other Microsoft Graph operations.
/// </remarks>
internal sealed class GraphOrganizationPingTraceConnector : IPingTraceConnector
{
    private readonly Func<GraphServiceClient> _resolveClient;
    private readonly string _configuredIdentity;

    internal GraphOrganizationPingTraceConnector(
        string name,
        Func<GraphServiceClient> resolveClient,
        string configuredIdentity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(resolveClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(configuredIdentity);

        Name = name;
        _resolveClient = resolveClient;
        _configuredIdentity = configuredIdentity;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    /// <remarks>
    /// MachineName contains the configured API host, because Microsoft
    /// Graph does not expose the physical server handling the request.
    ///
    /// Identity describes the configured authentication identity; it is
    /// not a server-returned assertion of the authenticated principal.
    ///
    /// Timing includes client resolution and the organization request.
    /// Failures and cancellation propagate to the caller. The application
    /// owns the client, so this connector does not dispose it.
    ///
    /// The configured base URL must be absolute and contain a host.
    /// Validation ensures MachineName describes the configured endpoint
    /// and reports invalid configuration before making a request.
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

        GraphServiceClient client = _resolveClient();

        cancellationToken.ThrowIfCancellationRequested();

        if (!Uri.TryCreate(client.RequestAdapter.BaseUrl, UriKind.Absolute, out Uri? endpoint) || string.IsNullOrWhiteSpace(endpoint.Host))
        {
            throw new InvalidOperationException(
                "The Microsoft Graph client must have an absolute base URL with a host.");
        }

        result.MachineName = endpoint.Host;

        var response = await client.Organization.GetAsync(
            request =>
            {
                request.QueryParameters.Select =
                    ["id", "displayName", "verifiedDomains"];
            },
            cancellationToken: cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        if (response?.Value is not { Count: 1 } organizations)
        {
            throw new InvalidOperationException(
                "Microsoft Graph did not return exactly one organization.");
        }

        var organization = organizations[0];

        if (organization is null || string.IsNullOrWhiteSpace(organization.Id))
        {
            throw new InvalidOperationException(
                "Microsoft Graph returned an organization without an ID.");
        }

        result.Finish(JsonSerializer.Serialize(new
        {
            TenantId = organization.Id,
            organization.DisplayName,
            VerifiedDomains = organization.VerifiedDomains?
                .Select(domain => domain.Name)
                .ToArray(),
            IdentitySource = "ConfiguredAuthentication",
            Probe = "OrganizationRead"
        }));

        return [result];
    }
}
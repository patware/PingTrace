using Microsoft.Extensions.DependencyInjection;

using PingTrace.Core;
using PingTrace.Runtime;

using NetHttpClient = System.Net.Http.HttpClient;

namespace PingTrace.Adapter.HttpClient;

/// <summary>
/// Registers HTTP destinations for PingTrace.
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Registers a named dependency reached through an HTTP client.
    /// </summary>
    /// <param name="builder">
    /// The PingTrace registration builder.
    /// </param>
    /// <param name="name">
    /// The destination name used by the calling application.
    /// </param>
    /// <param name="configureClient">
    /// Configures the client, including its base address.
    /// </param>
    /// <param name="tracePath">
    /// The relative path to the remote application's local Trace endpoint.
    /// </param>
    /// <returns>
    /// The HTTP client builder for configuring handlers and other
    /// HTTP client services.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The builder or client configuration callback is
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The destination name or trace path is empty, or the path is absolute.
    /// </exception>
    /// <remarks>
    /// Each registration receives a separate named HTTP client, allowing
    /// multiple HTTP destinations to use the same connector implementation.
    /// The default path begins at the server root. To preserve a path prefix
    /// in BaseAddress, supply a relative path without a leading slash and
    /// give BaseAddress a trailing slash.
    /// </remarks>
    public static IHttpClientBuilder AddHttpDestination(
        this PingTraceBuilder builder,
        string name,
        Action<NetHttpClient> configureClient,
        string tracePath = "/pingtrace/Trace")
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configureClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(tracePath);

        if (tracePath.StartsWith("//", StringComparison.Ordinal) ||
            tracePath.Contains('\\') ||
            Uri.TryCreate(tracePath, UriKind.Absolute, out _))
        {
            throw new ArgumentException(
                "The trace path must be relative to the configured server.",
                nameof(tracePath));
        }

        string httpClientName = $"PingTrace.Http:{name}";

        IHttpClientBuilder httpClientBuilder = builder.Services.AddHttpClient(httpClientName, configureClient);

        builder.Services.AddScoped<IPingTraceConnector>(provider =>
            new HttpPingTraceConnector(
                name,
                provider.GetRequiredService<IHttpClientFactory>(),
                httpClientName,
                tracePath));

        return httpClientBuilder;
    }
}
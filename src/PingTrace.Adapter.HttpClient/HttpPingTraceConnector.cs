using System.Net.Http.Json;

using PingTrace.Core;

namespace PingTrace.Adapter.HttpClient;

/// <summary>
/// Requests a local trace from a remote PingTrace HTTP endpoint.
/// </summary>
internal sealed class HttpPingTraceConnector : IPingTraceConnector
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly string _httpClientName;
    private readonly string _tracePath;

    internal HttpPingTraceConnector(
        string name,
        IHttpClientFactory clientFactory,
        string httpClientName,
        string tracePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(httpClientName);
        ArgumentException.ThrowIfNullOrWhiteSpace(tracePath);

        Name = name;
        _clientFactory = clientFactory;
        _httpClientName = httpClientName;
        _tracePath = tracePath;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TraceResult>> TraceAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var client = _clientFactory.CreateClient(_httpClientName);

        if (client.BaseAddress is null)
        {
            throw new InvalidOperationException(
                $"HTTP destination '{Name}' requires a BaseAddress.");
        }

        TraceResult? result = await client.GetFromJsonAsync<TraceResult>(_tracePath, cancellationToken);

        if (result is null || string.IsNullOrWhiteSpace(result.Name))
        {
            throw new InvalidOperationException(
                $"HTTP destination '{Name}' returned no valid named trace result.");
        }

        return new[] { result };
    }
}
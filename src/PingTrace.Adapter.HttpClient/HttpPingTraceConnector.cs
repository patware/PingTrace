using System.Net.Http.Json;

using PingTrace.Core;

namespace PingTrace.Adapter.HttpClient;

/// <summary>
/// Requests local or recursive diagnostics from remote PingTrace HTTP endpoints.
/// </summary>
internal sealed class HttpPingTraceConnector : IPingTraceConnector, IRecursivePingTraceConnector
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly string _httpClientName;
    private readonly string _tracePath;
    private readonly string _traceAllPath;

    internal HttpPingTraceConnector(
        string name,
        IHttpClientFactory clientFactory,
        string httpClientName,
        string tracePath,
        string traceAllPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(httpClientName);
        ArgumentException.ThrowIfNullOrWhiteSpace(tracePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(traceAllPath);

        Name = name;
        _clientFactory = clientFactory;
        _httpClientName = httpClientName;
        _tracePath = tracePath;
        _traceAllPath = traceAllPath;
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

        return [result];
    }

    /// <inheritdoc />
    /// <remarks>
    /// POST carries traversal state in a JSON body. The response is an array,
    /// unlike the existing local Trace endpoint's single-object response.
    ///
    /// An unavailable TraceAll endpoint remains a failure. Falling back to a
    /// local Trace would silently omit the remote application's dependencies.
    /// </remarks>
    public async Task<IReadOnlyList<TraceResult>> TraceAllAsync(TraceAllRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        cancellationToken.ThrowIfCancellationRequested();

        using var client = _clientFactory.CreateClient(_httpClientName);

        if (client.BaseAddress is null)
        {
            throw new InvalidOperationException(
                $"HTTP destination '{Name}' requires a BaseAddress.");
        }

        using HttpResponseMessage response = await client.PostAsJsonAsync(_traceAllPath, request, cancellationToken);

        response.EnsureSuccessStatusCode();

        TraceResult[]? results = await response.Content.ReadFromJsonAsync<TraceResult[]>(cancellationToken: cancellationToken);

        if (results is null ||
            results.Length == 0 ||
            results.Any(result =>
                result is null || string.IsNullOrWhiteSpace(result.Name)))
        {
            throw new InvalidOperationException(
                $"HTTP destination '{Name}' returned no valid trace subtree.");
        }

        return results;
    }

}

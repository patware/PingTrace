using System.Net.Http.Json;

using PingTrace.Core;

using NetHttpClient = System.Net.Http.HttpClient;

namespace PingTrace.Adapter.HttpClient;

/// <summary>
/// Requests local or recursive diagnostics from remote PingTrace HTTP endpoints.
/// </summary>
/// <remarks>
/// HTTP transport failures, HTTP error responses, and client timeouts become
/// diagnostic results. Caller cancellation, configuration errors, and invalid
/// response data continue to propagate.
/// </remarks>
internal sealed class HttpPingTraceConnector : IPingTraceConnector,
    IRecursivePingTraceConnector
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
    public async Task<IReadOnlyList<TraceResult>> TraceAsync(
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            async (client, token) =>
            {
                TraceResult? result = await client.GetFromJsonAsync<TraceResult>(_tracePath, token);

                if (result is null || string.IsNullOrWhiteSpace(result.Name))
                {
                    throw new InvalidOperationException(
                        $"HTTP destination '{Name}' returned no valid named trace result.");
                }

                return [result];
            },
            cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// A failed recursive request produces a diagnostic entry for the
    /// destination. It does not fall back to local Trace or claim that
    /// the destination's dependencies were inspected.
    /// </remarks>
    public async Task<IReadOnlyList<TraceResult>> TraceAllAsync(TraceAllRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        return await ExecuteAsync(
            async (client, token) =>
            {
                using HttpResponseMessage response =
                    await client.PostAsJsonAsync(
                        _traceAllPath,
                        request,
                        token);

                response.EnsureSuccessStatusCode();

                TraceResult[]? results =
                    await response.Content.ReadFromJsonAsync<TraceResult[]>(
                        cancellationToken: token);

                if (results is null ||
                    results.Length == 0 ||
                    results.Any(result =>
                        result is null ||
                        string.IsNullOrWhiteSpace(result.Name)))
                {
                    throw new InvalidOperationException(
                        $"HTTP destination '{Name}' returned no valid trace subtree.");
                }

                return results;
            },
            cancellationToken);
    }

    private async Task<IReadOnlyList<TraceResult>> ExecuteAsync(
        Func<NetHttpClient, CancellationToken,
            Task<IReadOnlyList<TraceResult>>> operation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using NetHttpClient client = _clientFactory.CreateClient(_httpClientName);

        if (client.BaseAddress is null)
        {
            throw new InvalidOperationException(
                $"HTTP destination '{Name}' requires a BaseAddress.");
        }

        // Construct before the request so failure timing includes the attempt.
        // The host is the configured destination, not a verified remote machine.
        TraceResult failure = new()
        {
            Name = Name,
            MachineName = client.BaseAddress.Host,
            Identity = string.Empty
        };

        try
        {
            IReadOnlyList<TraceResult> results = await operation(client, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            return results;
        }
        catch (HttpRequestException exception)
        {
            // Prefer cancellation if it occurred alongside a transport failure.
            cancellationToken.ThrowIfCancellationRequested();

            string payload = exception.StatusCode is { } statusCode
                ? $"Destination returned HTTP {(int)statusCode} ({statusCode})."
                : "Destination not reachable: HTTP transport failed.";

            failure.Finish(payload);
            return [failure];
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested &&
                  exception.InnerException is TimeoutException)
        {
            cancellationToken.ThrowIfCancellationRequested();

            failure.Finish("Destination did not respond before the HTTP timeout.");

            return [failure];
        }
    }
}
using System.Net;
using System.Text;

namespace AspireAppSample.ApiService.Services.Simulator;

/// <summary>
/// Supplies simulated Microsoft Graph responses for the sample application.
/// </summary>
/// <remarks>
/// This terminal handler never opens a network connection. Keeping it in
/// the sample project allows the production adapter to remain independent
/// of simulation behavior.
///
/// The handler supports only the organization read used by PingTrace.
/// Unexpected requests fail explicitly so the simulation does not silently
/// approve operations it does not implement.
/// </remarks>
internal sealed class SimulatedGraphHttpMessageHandler : HttpMessageHandler
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes the handler with the sample's configuration.
    /// </summary>
    /// <param name="configuration">
    /// Configuration containing the simulation scenario and delay.
    /// </param>
    public SimulatedGraphHttpMessageHandler(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _configuration = configuration;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Configuration is read for each request so reloadable configuration
    /// can change the scenario without replacing the handler.
    /// Artificial latency observes cancellation to exercise request
    /// cancellation and PingTrace's cooperative execution deadline.
    /// </remarks>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.Method != HttpMethod.Get ||
            request.RequestUri is not
            {
                Scheme: "https",
                Host: "graph.simulated",
                AbsolutePath: "/v1.0/organization"
            })
        {
            throw new InvalidOperationException(
                "The Graph simulation supports only " +
                "GET https://graph.simulated/v1.0/organization.");
        }

        int delayMilliseconds =
            _configuration.GetValue<int?>(
                "GraphSimulation:DelayMilliseconds") ?? 100;

        if (delayMilliseconds < 0)
        {
            throw new InvalidOperationException(
                "GraphSimulation:DelayMilliseconds must not be negative.");
        }

        await Task.Delay(delayMilliseconds, cancellationToken);

        string scenario =
            _configuration["GraphSimulation:Scenario"] ?? "Success";

        return scenario.Trim().ToLowerInvariant() switch
        {
            "success" => JsonResponse(
                HttpStatusCode.OK,
                """
                {
                  "value": [
                    {
                      "id": "11111111-1111-1111-1111-111111111111",
                      "displayName": "Contoso — simulated organization",
                      "verifiedDomains": [
                        {
                          "name": "contoso.example",
                          "isDefault": true,
                          "isVerified": true
                        }
                      ]
                    }
                  ]
                }
                """),

            "unauthorized" => JsonResponse(
                HttpStatusCode.Unauthorized,
                """
                {
                  "error": {
                    "code": "InvalidAuthenticationToken",
                    "message": "Simulated authentication failure."
                  }
                }
                """),

            "forbidden" => JsonResponse(
                HttpStatusCode.Forbidden,
                """
                {
                  "error": {
                    "code": "Authorization_RequestDenied",
                    "message": "Simulated insufficient permissions."
                  }
                }
                """),

            "unavailable" => JsonResponse(
                HttpStatusCode.ServiceUnavailable,
                """
                {
                  "error": {
                    "code": "ServiceUnavailable",
                    "message": "Simulated service outage."
                  }
                }
                """),

            "empty" => JsonResponse(
                HttpStatusCode.OK,
                """{"value":[]}"""),

            "malformed" => JsonResponse(
                HttpStatusCode.OK,
                "not-json"),

            _ => throw new InvalidOperationException(
                $"Unknown Graph simulation scenario '{scenario}'.")
        };
    }

    private static HttpResponseMessage JsonResponse(
        HttpStatusCode statusCode,
        string json)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json")
        };
    }
}
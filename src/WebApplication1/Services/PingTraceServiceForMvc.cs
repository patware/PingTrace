using Patware.PingTrace.Core;

namespace WebApplication1.Services
{
  public sealed class PingTraceServiceForMvc(HttpClient client, IConfiguration configuration) : IPingTrace, IDiscovery
  {
    private static readonly Guid ThisId = new("A110F236-5A01-440D-B5F1-12D8901642D3");

    private const string ThisName = "WebApplication1";


    public string Ping()
    {
      return "Pong";
    }

    public async Task<TraceResult> TraceAsync(CancellationToken cancellationToken)
    {

      cancellationToken.ThrowIfCancellationRequested();

      TraceResult current = new(ThisId)
      {
        Name = ThisName,
        MachineName = Environment.MachineName,
        Identity = $"{Environment.UserDomainName}\\{Environment.UserName}"
      };

      current.Finish($"Pong");
      return current;
    }

    public async Task<IReadOnlyList<TraceResult>> TraceAsync(string destination, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();

      TraceResult current = new(ThisId)
      {
        Name = ThisName,
        MachineName = Environment.MachineName,
        Identity = $"{Environment.UserDomainName}\\{Environment.UserName}"
      };

      List<TraceResult> results = [current];

      if (!string.IsNullOrEmpty(destination)
          && !string.Equals(
            destination, ThisName, StringComparison.OrdinalIgnoreCase))
      {
        string path = "api/WebService1/Trace?destination="
          + Uri.EscapeDataString(destination);

        List<TraceResult> downstream =
          await client.GetFromJsonAsync<List<TraceResult>>(path, cancellationToken)
            ?? throw new InvalidOperationException(
              "WebService1 returned a null trace response.");

        results.AddRange(downstream);
      }

      current.Finish($"Pong [{destination}]");
      return results;
    }

    public async Task<IReadOnlyList<TraceDestination>> GetDestinationsAsync(CancellationToken cancellationToken)
    {
      List<TraceDestination> destinations =
      [
        new TraceDestination(ThisId, ThisName)
        {
          ExpectedElapsedMilisecondsAverage = 1,
          ExpectedElapsedMilisecondsMax = 1,
          ExpectedIdentity = configuration["PingTrace:ExpectedIdentityMvc"] ?? string.Empty,
          ExpectedMachineNames =
          [
            configuration["PingTrace:ExpectedMachineNameMvc"] ?? string.Empty
          ],
          PayloadDescription = "Pong and the destination in square brackets",
          PayloadRegex = @"Pong \[.*\]"
        }
      ];

      List<TraceDestination> downstream =
        await client.GetFromJsonAsync<List<TraceDestination>>("api/WebService1/Destinations", cancellationToken)
        ?? throw new InvalidOperationException(
          "WebService1 returned a null destinations response.");

      destinations.AddRange(downstream);
      return destinations;
    }
  }
}
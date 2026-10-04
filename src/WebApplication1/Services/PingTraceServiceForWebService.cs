using Patware.PingTrace.Core;
using WebApplication1.Data;

namespace WebApplication1.Services
{
  public sealed class PingTraceServiceForWebService(IRepository repository, IConfiguration configuration) : IPingTrace, IDiscovery
  {
    private static readonly Guid ThisId = new("32DE64E4-6FB4-4F8B-A2C5-C42BF1313141");

    private const string ThisName = "WebService1";

    public string Ping()
    {
      return "Pong";
    }


    public Task<TraceResult> TraceAsync(CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();

      TraceResult current = CreateLocalResult();

      current.Finish("Pong");

      return Task.FromResult(current);
    }

    public async Task<IList<TraceResult>> TraceAsync(string destination, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      ArgumentException.ThrowIfNullOrWhiteSpace(destination);

      bool isLocal = string.Equals(destination, ThisName, StringComparison.OrdinalIgnoreCase);

      bool isDatabase = string.Equals(destination, "Database1", StringComparison.OrdinalIgnoreCase);

      if (!isLocal && !isDatabase)
      {
        throw new KeyNotFoundException($"Unknown trace destination '{destination}'.");
      }

      TraceResult current = CreateLocalResult();

      List<TraceResult> results = [current];

      if (isDatabase)
      {
        results.AddRange(await repository.RunTraceAsync(destination, cancellationToken));
      }

      current.Finish($"Pong [{destination}]");

      return results;
    }

    public async Task<IList<TraceDestination>> GetDestinationsAsync(CancellationToken cancellationToken = default)
    {
      List<TraceDestination> destinations =
      [
        new TraceDestination(ThisId, ThisName)
        {
          ExpectedElapsedMilisecondsAverage = 1,
          ExpectedElapsedMilisecondsMax = 1,
          ExpectedIdentity = configuration["PingTrace:ExpectedIdentityWebService"]
            ?? string.Empty,
          ExpectedMachineNames =
          [
            configuration["PingTrace:ExpectedMachineNameWebService"]
            ?? string.Empty
          ],
          PayloadDescription = "Pong and the destination in square brackets",
          PayloadRegex = @"Pong \[.*\]"
        },
        .. await repository.GetDestinationsAsync(cancellationToken),
      ];

      return destinations;
    }

    private static TraceResult CreateLocalResult()
    {
      return new TraceResult(ThisId)
      {
        Name = ThisName,
        MachineName = Environment.MachineName,
        Identity = $"{Environment.UserDomainName}\\{Environment.UserName}"
      };
    }

  }
}
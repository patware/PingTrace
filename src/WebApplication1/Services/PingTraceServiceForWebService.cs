using Patware.PingTrace.Core;
using WebApplication1.Data;

namespace WebApplication1.Services
{
  public sealed class PingTraceServiceForWebService(IRepository repository, IConfiguration configuration) : IPingTraceService
  {
    private static readonly Guid ThisId =
      new Guid("32DE64E4-6FB4-4F8B-A2C5-C42BF1313141");

    private const string ThisName = "WebService1";

    public string Ping()
    {
      return "Pong";
    }

    public List<TraceResult> Trace(string destination)
    {
      TraceResult current = new TraceResult(ThisId)
      {
        Name = ThisName,
        MachineName = Environment.MachineName,
        Identity = Environment.UserDomainName
      };

      List<TraceResult> results = new List<TraceResult> { current };

      if (string.Equals(destination, "Database1", StringComparison.OrdinalIgnoreCase))
      {
        results.Add(repository.RunTrace(destination));
      }

      current.Finish($"Pong [{destination}]");
      return results;
    }

    public List<TraceDestination> Traces()
    {
      List<TraceDestination> destinations = new List<TraceDestination>
      {
        new TraceDestination(ThisId, ThisName)
        {
          ExpectedElapsedMilisecondsAverage = 1,
          ExpectedElapsedMilisecondsMax = 1,
          ExpectedIdentity = configuration["PingTrace:ExpectedIdentityWebService"]
            ?? string.Empty,
          ExpectedMachineNames = new[]
          {
            configuration["PingTrace:ExpectedMachineNameWebService"]
            ?? string.Empty
          },
          PayloadDescription = "Pong and the destination in square brackets",
          PayloadRegex = @"Pong \[.*\]"
        }
      };

      destinations.AddRange(repository.RunTraces());
      return destinations;
    }
  }
}
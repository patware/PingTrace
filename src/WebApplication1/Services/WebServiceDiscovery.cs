using Microsoft.Extensions.Options;
using Patware.PingTrace;
using Patware.PingTrace.Core;
using WebApplication1.Data;

namespace WebApplication1.Services
{
  public sealed class WebServiceDiscovery(IRepository repository,
    IConfiguration configuration,
    IOptions<PingTraceOptions> options) : IDiscovery
  {
    public async Task<IReadOnlyList<TraceDestination>> GetDestinationsAsync(CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();

      PingTraceOptions application = options.Value;

      List<TraceDestination> destinations =
        [
          new TraceDestination(application.Id, application.Name)
          {
            ExpectedElapsedMilisecondsAverage = 1,
            ExpectedElapsedMilisecondsMax = 1,
            ExpectedIdentity = configuration["PingTrace:ExpectedIdentityWebService"] ?? string.Empty,
            ExpectedMachineNames = [configuration["PingTrace:ExpectedMachineNameWebService"] ?? string.Empty ],
            PayloadDescription = "Pong and the destination in square brackets",
            PayloadRegex = @"Pong \[.*\]"
          }
        ];

      IReadOnlyList<TraceDestination> downstream = await repository.GetDestinationsAsync(cancellationToken);

      destinations.AddRange(downstream);

      return destinations;
    }
  }
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PingTrace.Core
{
  public interface IDiscovery
  {
    Task<IReadOnlyList<TraceDestination>> GetDestinationsAsync(CancellationToken cancellationToken = default);
  }
}

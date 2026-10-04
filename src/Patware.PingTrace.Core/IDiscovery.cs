using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Patware.PingTrace.Core
{
  public interface IDiscovery
  {
    Task<IList<TraceDestination>> GetDestinationsAsync(CancellationToken cancellationToken = default);
  }
}

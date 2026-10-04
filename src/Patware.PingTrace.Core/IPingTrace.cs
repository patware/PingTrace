using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Patware.PingTrace.Core
{
  public interface IPingTrace
  {
    string Ping();

    Task<Patware.PingTrace.Core.TraceResult> TraceAsync(CancellationToken cancellationToken = default);

    Task<IList<Patware.PingTrace.Core.TraceResult>> TraceAsync(string destination, CancellationToken cancellationToken = default);
  }
}

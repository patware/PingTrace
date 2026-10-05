using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Patware.PingTrace.Core
{
  public interface IPingTraceDestination
  {
    string Name { get; }

    Task<IReadOnlyList<TraceResult>> TraceAsync(CancellationToken cancellationToken = default);
  }
}

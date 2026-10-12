using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PingTrace.Core;

public interface IPingTrace
{
    string Ping();

    Task<PingTrace.Core.TraceResult> TraceAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PingTrace.Core.TraceResult>> TraceAsync(string destination, CancellationToken cancellationToken = default);
}

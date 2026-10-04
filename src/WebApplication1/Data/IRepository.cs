using Patware.PingTrace.Core;

namespace WebApplication1.Data
{
  public interface IRepository : IDiscovery
  {
    Task<string> RunPingAsync(CancellationToken cancellationToken = default);

    Task<TraceResult> RunTraceAsync(CancellationToken cancellationToken = default);
    Task<IList<TraceResult>> RunTraceAsync(string destination, CancellationToken cancellationToken = default);
  }
}
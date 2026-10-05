using Patware.PingTrace.Core;
using WebApplication1.Data;

namespace WebApplication1.Services
{
  public sealed class DatabaseTraceDestination(IRepository repository) : IPingTraceDestination
  {
    public string Name => "Database1";

    public async Task<IReadOnlyList<TraceResult>> TraceAsync(CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();

      TraceResult result = await repository.RunTraceAsync(cancellationToken);

      return [result];
    }
  }
}
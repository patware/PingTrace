using Patware.PingTrace.Core;

namespace Patware.PingTrace
{
  public sealed class DefaultPingTrace : IPingTrace
  {
    private readonly Guid _id;
    private readonly string _name;
    private readonly Dictionary<string, IPingTraceDestination> _destinations;

    public DefaultPingTrace(PingTraceOptions options, IEnumerable<IPingTraceDestination> destinations)
    {
      ArgumentNullException.ThrowIfNull(options);
      ArgumentNullException.ThrowIfNull(destinations);
      ArgumentException.ThrowIfNullOrWhiteSpace(options.Name);

      if (options.Id == Guid.Empty)
      {
        throw new ArgumentException("An Application ID is required.", nameof(options));
      }

      _id = options.Id;
      _name = options.Name;
      _destinations = new(StringComparer.OrdinalIgnoreCase);

      foreach (IPingTraceDestination destination in destinations)
      {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination.Name);

        if (string.Equals(destination.Name, _name, StringComparison.OrdinalIgnoreCase))
        {
          throw new ArgumentException(
            $"Destination '{destination.Name}' conflicts with the application name.",
            nameof(destinations));
        }

        if (!_destinations.TryAdd(destination.Name, destination))
        {
          throw new ArgumentException(
            $"Destination '{destination.Name}' is registered more than once.",
            nameof(destinations));
        }
      }

    }

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

    public async Task<IReadOnlyList<TraceResult>> TraceAsync(string destination, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      ArgumentException.ThrowIfNullOrWhiteSpace(destination);

      bool isLocal = string.Equals(destination, _name, StringComparison.OrdinalIgnoreCase);

      IPingTraceDestination? handler = null;

      if (!isLocal && !_destinations.TryGetValue(destination, out handler))
      {
        throw new KeyNotFoundException($"Unknown trace destination '{destination}'.");
      }

      TraceResult current = CreateLocalResult();

      List<TraceResult> results = [current];

      if (handler is not null)
      {
        IReadOnlyList<TraceResult> downstream = await handler.TraceAsync(cancellationToken);

        results.AddRange(downstream);
      }

      current.Finish($"Pong [{destination}]");

      return results;
    }

    private TraceResult CreateLocalResult()
    {
      return new TraceResult(_id)
      {
        Name = _name,
        MachineName = Environment.MachineName,
        Identity = $"{Environment.UserDomainName}\\{Environment.UserName}"
      };
    }

  }
}

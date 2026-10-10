using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using PingTrace.Core;

namespace PingTrace.Runtime
{
  public static class PingTraceServiceCollectionExtensions
  {
    public static IServiceCollection AddPingTrace(this IServiceCollection services, Action<PingTraceOptions> configure)
    {
      ArgumentNullException.ThrowIfNull(services);
      ArgumentNullException.ThrowIfNull(configure);

      services
        .AddOptions<PingTraceOptions>()
        .Configure(configure)
        .Validate(
          options => options.Id != Guid.Empty,
          "An application ID is required.")
        .Validate(
          options => !string.IsNullOrWhiteSpace(options.Name),
          "An application name is required.")
        .ValidateOnStart();

      services.TryAddScoped<IPingTrace>(provider =>
      {
        PingTraceOptions options = provider
          .GetRequiredService<IOptions<PingTraceOptions>>()
          .Value;

        IEnumerable<IPingTraceDestination> destinations = provider.GetServices<IPingTraceDestination>();

        return new DefaultPingTrace(options, destinations);
      });

      return services;
    }

    public static IServiceCollection AddPingTraceDestination<TDestination>(this IServiceCollection services)
      where TDestination : class, IPingTraceDestination
    {
      ArgumentNullException.ThrowIfNull(services);

      services.TryAddEnumerable(ServiceDescriptor.Scoped<IPingTraceDestination, TDestination>());

      return services;
    }
  }
}
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Patware.PingTrace.Core;

namespace Patware.PingTrace.AspNetCore
{
  public static class PingTraceEndpointRouteBuilderExtensions
  {
    public static RouteGroupBuilder MapPingTrace(this IEndpointRouteBuilder endpoints, string prefix = "/pingtrace")
    {
      ArgumentNullException.ThrowIfNull(endpoints);
      ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

      RouteGroupBuilder group = endpoints.MapGroup(prefix);

      group.MapGet("/Ping", HandlePing);
      group.MapGet("/Trace", HandleTraceAsync);

      return group;

    }

    public static RouteGroupBuilder MapPingTraceDiscovery(this IEndpointRouteBuilder endpoints, string prefix = "/pingtrace")
    {
      ArgumentNullException.ThrowIfNull(endpoints);
      ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

      RouteGroupBuilder group = endpoints.MapGroup(prefix);

      group.MapGet("/Destinations", HandleDestinationsAsync);

      return group;
    }

    private static IResult HandlePing([FromServices] IPingTrace pingTrace)
    {
      return Results.Text(pingTrace.Ping(), "text/plain");
    }


    private static async Task<IResult> HandleTraceAsync(HttpContext context, [FromServices] IPingTrace pingTrace)
    {
      CancellationToken cancellationToken = context.RequestAborted;

      if (!context.Request.Query.TryGetValue("destination", out var values))
      {
        TraceResult result = await pingTrace.TraceAsync(cancellationToken);

        return Results.Ok(result);
      }

      string destination = values.ToString();

      if (string.IsNullOrWhiteSpace(destination))
      {
        return Results.BadRequest("Destination must not be empty.");
      }

      try
      {
        IReadOnlyList<TraceResult> results = await pingTrace.TraceAsync(destination, cancellationToken);

        return Results.Ok(results);
      }
      catch
      {
        return Results.NotFound("The requested destination is not known.");
      }
    }
    private static async Task<IResult> HandleDestinationsAsync(HttpContext context, [FromServices] IDiscovery discovery)
    {
      IReadOnlyList<TraceDestination> destinations = await discovery.GetDestinationsAsync(context.RequestAborted);

      return Results.Ok(destinations);
    }
  }
}

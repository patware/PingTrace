# PingTrace

**Is it responding? Where is it running? What is it reaching?**

PingTrace brings the idea behind ping and traceroute into your applications:
a lightweight response to confirm that an application endpoint is reachable,
and diagnostic results to help identify the systems involved in a request.

Use it to verify deployments, investigate unexpected destinations, and narrow
down delays between application components.

## Why PingTrace?

An application can be running and still be pointing to the wrong backend,
using an unexpected identity, or waiting on a slow connection.

PingTrace helps answer practical questions:

- Does this application endpoint respond with the expected value?
- Which machine handled the request?
- Which runtime identity does the application report?
- Did the request reach the expected application or dependency?
- Where should I investigate an unexpected delay?

The same diagnostic endpoints can serve both monitoring tools and developers
investigating a deployment.

## Ping and Trace

| Operation | Purpose |
| --- | --- |
| **Ping** | Return a predictable string with minimal work. The default implementation returns `Pong`. |
| **Trace** | Return diagnostic information about the application handling the request. |
| **Trace(destination)** | Return diagnostic information from the local application and a selected registered destination handler. |

A trace result contains an application identifier and name, machine name,
runtime identity, start and finish times, elapsed time, and a payload.

The default local trace does not contact dependencies. A destination trace
invokes the selected handler, which can return results from downstream systems.

This distinction is useful during troubleshooting: compare a local trace with
a destination trace to determine whether the delay appears when a dependency
is involved.

## Get started with ASP.NET Core

The current libraries target .NET 10. Reference both `PingTrace.Runtime` and
`PingTrace.AspNetCore` from your ASP.NET Core application.

Register the runtime and map the endpoints in `Program.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using PingTrace.AspNetCore;
using PingTrace.Runtime;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddPingTrace(options =>
{
    options.Id = Guid.Parse("C21DC569-5A97-41D4-A8EF-64A6C48BFB97");
    options.Name = "CustomerPortal";
});

WebApplication app = builder.Build();

app.MapPingTrace();

app.Run();
```

Choose an application identifier once and retain it across restarts so that
trace results can be associated with the same logical application.

The default route prefix is `/pingtrace`:

| Request | Current response |
| --- | --- |
| `GET /pingtrace/Ping` | Plain text: `Pong` |
| `GET /pingtrace/Trace` | One JSON trace-result object describing the local application |
| `GET /pingtrace/Trace?destination=CustomerPortal` | A JSON array containing the local application's result |
| `GET /pingtrace/Trace?destination=Database1` | A JSON array containing the local result followed by results from a registered `Database1` handler |

The example registers no downstream handlers. To support `Database1` or
another dependency, implement `IPingTraceDestination` and register it with
`AddPingTraceDestination<TDestination>()`.

Destination names are matched without regard to case by the default runtime.

To use a different route prefix:

```csharp
app.MapPingTrace("/diagnostics");
```

Trace responses include machine and runtime identity information. Configure
access through the hosting application's authentication, authorization, and
network policies.

## Fit the check to the question

Different checks have different costs. These intervals illustrate the intended
usage model; PingTrace does not schedule probes or enforce polling intervals.

| Level | Example interval | Question | PingTrace scope |
| --- | --- | --- | --- |
| A | Every second | Does a minimal endpoint respond? | Explicit HEAD support is under consideration. |
| B | Every 10 seconds | Is the application responding? | Ping returns a predictable string. |
| C | Every minute | Can the application reach its dependencies? | Destination Trace provides the current extension point; whole-graph traversal is a design goal. |
| D | Every hour | Can a representative business operation be evaluated safely? | Application-owned synthetic or what-if checks, outside the initial scope. |

Select intervals and timeouts according to the application and the cost of
its dependencies. A trace that fans out across several systems costs more
than a local response.

PingTrace complements .NET Health Checks, Docker HEALTHCHECK, and Kubernetes
liveness and readiness probes. It supplies diagnostic responses that an
application or monitoring integration can interpret. The host and monitoring
system decide thresholds, health policy, notifications, and recovery actions.

## Verify deployments one layer at a time

Simple diagnostic resources can help isolate deployment problems:

| Resource | What a successful request helps establish |
| --- | --- |
| `ping.html` | The requested static HTML resource can be served through the configured hosting path. |
| `ping.png` | The requested image resource can be served with the expected content type. |
| An application Ping endpoint | The request reaches application code and receives the expected response. |
| A local Trace | The responding application reports its machine, runtime identity, and payload. |
| A destination Trace | The configured destination handler runs and returns its diagnostic results. |

These checks provide evidence about the path exercised by that request.
For identity and file-permission investigations, account for caching, proxies,
and the identity actually used to access the resource.

The same approach can be implemented through other technologies: `ping.asp`,
`ping.aspx`, a database procedure, or a gRPC service. Those are integration
patterns; this repository currently provides the .NET contracts, runtime,
and ASP.NET Core endpoint adapter.

## Investigate unexpected delays

Consider an application chain:

```text
Frontend → Backend → Database
```

If tracing from the frontend becomes slow:

1. Trace the frontend locally.
2. Trace the backend locally.
3. Trace from the backend toward the database.
4. Compare with a diagnostic request made directly to the database.

Fast local responses combined with a slow destination trace help narrow the
investigation to the dependency call and the work around it.

A node's elapsed time includes the downstream work it awaits. It is not a
direct measurement of network latency. Authentication, connection setup,
serialization, and scheduling can also contribute to the observed delay.

## Libraries

| Library | Responsibility |
| --- | --- |
| **PingTrace.Core** | Contracts and models for Ping, Trace, destinations, and discovery. |
| **PingTrace.Runtime** | Default implementation, application options, and dependency-injection registration. |
| **PingTrace.AspNetCore** | HTTP endpoint mapping for Ping, Trace, and optional destination discovery. |

Discovery is separate from the basic Ping and Trace endpoints. Mapping
discovery also requires an application-provided `IDiscovery` implementation.

## Direction

The intended adoption experience is simple service registration and endpoint
mapping, with predictable responses for monitoring tools.

Areas planned for further design include:

- A consistent array response for both local and destination Trace endpoints.
- Convenient registration defaults.
- Controlled traversal of known application dependencies.
- Explicit HEAD endpoint support, if justified by the monitoring use cases.
- A Blazor component showing results as a log, table, or dependency graph.
- On-demand destination probes to help investigate delays along individual paths.

A graphical investigation may require separate probes for paths such as
`A`, `A → B`, `A → B → C`, and `A → E`. That additional work belongs in an
on-demand diagnostic workflow rather than automatically increasing the cost
of routine monitoring.

These items describe the direction of the project, not completed features.

## Documentation and contributing

The documentation is being developed alongside the library:

- [Documentation home](docs/index.md)
- [Architecture](docs/architecture/ARCHITECTURE.md)
- [Capability boundaries](docs/design/CAPABILITY-BOUNDARIES.md)
- [Development setup](docs/development/GETTING-STARTED.md)
- [Contributing](CONTRIBUTING.md)
- [License](LICENSE)
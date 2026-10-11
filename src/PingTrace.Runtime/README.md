# PingTrace.Runtime

**Default Ping and Trace behavior, with explicit dependency registration.**

`PingTrace.Runtime` provides the default implementation of `IPingTrace` and
the `AddPingTrace` service registration extension.

Use it to respond to lightweight diagnostic requests and trace dependencies
through registered connectors.

The library targets .NET 10. It does not require ASP.NET Core or expose
endpoints by itself.

## Register PingTrace

For an application without dependencies:

```csharp
using PingTrace.Runtime;

builder.Services.AddPingTrace();
```

This registers `DefaultPingTrace` as a scoped `IPingTrace` service.

Consumers can receive `IPingTrace` through dependency injection. Its operations
are:

| Operation | Result |
| --- | --- |
| `Ping()` | The string `Pong`. |
| `TraceAsync(cancellationToken)` | One local `TraceResult`. |
| `TraceAsync(destination, cancellationToken)` | A collection containing the local result followed by any selected dependency results. |

Ping and local Trace do not invoke connectors.

Registration does not contact dependencies. Connector constructors should also
remain inexpensive because the runtime receives the registered connectors
when its scoped instance is constructed.

## Expose ASP.NET Core endpoints

Reference `PingTrace.AspNetCore` in addition to this package:

```csharp
using PingTrace.AspNetCore;
using PingTrace.Runtime;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddPingTrace();

WebApplication app = builder.Build();

app.MapPingTrace();

app.Run();
```

`AddPingTrace` registers the implementation. `MapPingTrace`, supplied by
`PingTrace.AspNetCore`, exposes it over HTTP.

Authorization policies belong to the hosting application. Trace results include
machine and execution identity information, so configure access appropriately
before exposing them.

## Register dependencies

The optional registration callback receives a `PingTraceBuilder`. Adapter
extensions use that builder to register connectors and their dependencies.

For an HTTP destination, also reference `PingTrace.Adapter.HttpClient`:

```csharp
using PingTrace.Adapter.HttpClient;
using PingTrace.Runtime;

builder.Services.AddPingTrace(pingTrace =>
{
    pingTrace.AddHttpDestination("ApiService", client =>
    {
        client.BaseAddress = new Uri("https://api.example.com/");
    });
});
```

Replace the address with your API's address. The remote application must expose
a compatible local Trace endpoint; the HTTP adapter's default path is
`/pingtrace/Trace`.

The callback executes immediately during registration, before the service
provider is built. It configures services rather than executing diagnostics.

`AddPingTrace` returns `IServiceCollection`. Adapter registration belongs inside
the callback.

### Destination names

`ApiService` is a local destination alias. The remote application reports its
own application name in its trace result.

The default runtime compares destination names using
`StringComparer.OrdinalIgnoreCase`.

Connector names must:

- Contain a non-whitespace value.
- Be unique without regard to case.
- Differ from the local application name without regard to case.

The default runtime validates these rules when it is constructed, normally when
`IPingTrace` is first resolved within a scope. Successful service registration
alone does not establish that every destination name is valid.

## Trace behavior

### Local Trace

`TraceAsync(cancellationToken)` creates and completes one local result without
invoking a dependency.

The default values are:

| Field | Value |
| --- | --- |
| `Name` | `Assembly.GetEntryAssembly()?.GetName().Name` |
| `MachineName` | `Environment.MachineName` |
| `Identity` | `Environment.UserDomainName` and `Environment.UserName`, separated by a backslash |
| `Payload` | `Pong` |

The name identifies the managed host's entry assembly, not the
`PingTrace.Runtime` assembly. Construction fails with `InvalidOperationException`
if the host assembly name cannot be determined.

The identity describes the process environment; it is not the authenticated
HTTP caller.

### Destination Trace

`TraceAsync(destination, cancellationToken)` selects one destination.

If the destination matches the local application name, it returns a collection
containing only the completed local result.

For a registered dependency, it:

1. Creates the local result.
2. Invokes the selected connector.
3. Places the local result before the connector's results.
4. Completes the local result with the payload `Pong [destination]`.
5. Returns the combined collection.

The connector owns communication with the dependency. The runtime owns the
calling application's result and preserves the order of the connector's
returned results.

The built-in HTTP connector requests the remote application's local Trace.
It does not automatically trace that application's dependencies.

### Timing

The local result is completed after the selected connector responds. Its
elapsed time therefore includes the connector operation and local processing.

This is not a direct measurement of network latency.

`TraceResult` calculates elapsed time from wall-clock timestamps. Clock
adjustments can affect the result.

## Failures and cancellation

Both Trace overloads check cancellation before performing their work.
Destination Trace passes the cancellation token to the selected connector.

For destination requests:

| Condition | Behavior |
| --- | --- |
| Null destination | Throws `ArgumentNullException`. |
| Empty or whitespace destination | Throws `ArgumentException`. |
| Unknown destination | Throws `KeyNotFoundException`. |
| Connector failure | Propagates the failure to the caller. |
| Cancellation | Propagates cancellation to the caller. |

A connector failure does not produce a successful response containing partial
results.

The runtime does not translate exceptions into HTTP status codes. That
responsibility belongs to the hosting integration.

## Extend the runtime

### Custom connectors

Implement `IPingTraceConnector` and register it with dependency injection.
The registration callback exposes the service collection through
`PingTraceBuilder.Services`.

A connector supplies its local destination name and returns the dependency's
trace results. It must not add the calling application's local result.

Keep connection configuration and protocol-specific behavior in the connector
or its supporting services.

### Custom application behavior

Applications can provide their own `IPingTrace` implementation through
dependency injection.

`AddPingTrace` uses `TryAddScoped`, so an existing `IPingTrace` registration is
preserved. Register the custom implementation before calling `AddPingTrace`
when using this behavior.

There is currently no `AddPingTrace<T>()` overload.

Calling `AddPingTrace` repeatedly preserves an existing runtime registration,
but executes each supplied callback. Repeated callbacks can therefore add
duplicate connector registrations.

## Current scope

The runtime supports local tracing and explicitly named destinations.

It does not currently provide:

- Trace All or automatic recursive traversal.
- Dependency discovery.
- Destination-path traversal.
- Application IDs.
- Built-in `IHealthCheck` integration.

`PingTraceOptions` is currently unused. Configuring its `Name` property does
not change the default runtime's application name.

The registration callback is an adapter-registration callback, not an
`IOptions<PingTraceOptions>` configuration API.

## Learn more

- [PingTrace overview](https://github.com/patware/Patware.PingTrace/blob/main/README.md)
- [Core contracts](https://github.com/patware/Patware.PingTrace/blob/main/src/PingTrace.Core/README.md)
- [Execution model](https://github.com/patware/Patware.PingTrace/blob/main/docs/architecture/PINGTRACE.md)
- [Connector architecture](https://github.com/patware/Patware.PingTrace/blob/main/docs/architecture/PLUGIN-ARCHITECTURE.md)

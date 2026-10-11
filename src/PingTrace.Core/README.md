# PingTrace.Core

**The shared contracts for application Ping and Trace diagnostics.**

PingTrace helps applications answer practical questions: Are you responding?
Where are you running? Which dependency did you reach?

`PingTrace.Core` defines the interfaces and result model used by implementations,
adapters, and consumers. It targets .NET 10 and has no package dependencies.

## When to use this package

Reference `PingTrace.Core` when implementing a custom PingTrace service,
building a connector, or consuming trace results without depending on a
particular host or transport.

For the default implementation and dependency registration, use
`PingTrace.Runtime`. For ASP.NET Core endpoints, also use
`PingTrace.AspNetCore`.

HTTP destination support is provided by `PingTrace.Adapter.HttpClient`.

## Application contract

`IPingTrace` exposes three operations:

| Operation | Return type | Purpose |
| --- | --- | --- |
| `Ping()` | `string` | Returns a lightweight application response. |
| `TraceAsync(cancellationToken)` | `Task<TraceResult>` | Returns the local application's diagnostic result. |
| `TraceAsync(destination, cancellationToken)` | `Task<IReadOnlyList<TraceResult>>` | Returns results for a named destination trace. |

Keep Ping inexpensive. It should not invoke dependencies or perform business
operations.

Local Trace describes the current application. Destination Trace allows an
implementation to invoke a configured dependency and return the relevant
results.

Both asynchronous operations accept a cancellation token. Implementations
should propagate it to downstream operations.

These interfaces do not define HTTP routes, response status codes, authorization
policies, or dependency injection registration.

### Default runtime behavior

The implementation in `PingTrace.Runtime` supplies the following behavior:

- Ping returns `Pong`.
- The local application name comes from the managed host's entry assembly.
- Local Trace does not invoke dependencies.
- A destination matching the local application name returns only the local
  result in a collection.
- A registered dependency returns the local result followed by the connector's
  results.
- Destination names are matched without regard to case.
- An unknown destination throws `KeyNotFoundException`.

These are behaviors of the default runtime, rather than implementations
provided by this package.

## Connector contract

`IPingTraceConnector` represents a configured dependency.

| Member | Purpose |
| --- | --- |
| `Name` | Identifies the destination within the calling application. |
| `TraceAsync(cancellationToken)` | Invokes the dependency and returns its trace results. |

A connector owns the communication needed to reach its dependency, including
the use of its configured client and protocol.

The connector returns the dependency's results. It does not add the calling
application's local result; the runtime owns that responsibility.

The connector name is a local alias. It does not need to match the name
reported by the remote application.

This separation allows different transports to participate in tracing without
introducing transport-specific requirements into the core contracts.

## Trace results

`TraceResult` describes one application's contribution to a trace.

| Member | Meaning |
| --- | --- |
| `Name` | Application name supplied by the implementation. |
| `MachineName` | Machine name supplied by the implementation. |
| `Identity` | Execution identity supplied by the implementation. |
| `StartedAt` | Start timestamp, initialized to `DateTime.Now` by the constructor. |
| `FinishedAt` | Completion timestamp, assigned by `Finish`. |
| `Payload` | Diagnostic string assigned by `Finish`. |
| `Elapsed` | Difference between `FinishedAt` and `StartedAt`. |

The constructor initializes the start timestamp. It does not populate the
application name, machine name, identity, or payload.

For example, an implementation can construct a local result as follows:

```csharp
using System;
using PingTrace.Core;

TraceResult result = new TraceResult
{
    Name = "ExampleApplication",
    MachineName = Environment.MachineName,
    Identity = $"{Environment.UserDomainName}\\{Environment.UserName}"
};

result.Finish("Pong");
```

`Finish(string payload)` assigns the payload and captures the completion time.
Read `Elapsed` after finishing the result.

The model is mutable. Its payload is currently a string, and it has no
application ID or graph relationship fields.

### Interpreting elapsed time

Elapsed time covers the interval between the result's start and completion.
When an implementation finishes its local result after awaiting a dependency,
that interval includes the dependency operation.

It is not a direct measurement of network latency.

The current implementation uses wall-clock timestamps rather than a monotonic
timer. Clock adjustments can affect the calculated duration.

## Scope

The current contracts do not expose Trace All, automatic dependency discovery,
or destination-path traversal.

A connector returning multiple results does not itself imply recursive
traversal. Traversal behavior depends on the implementation and connector.

PingTrace complements health checks and deployment diagnostics. These
interfaces do not inherit from `IHealthCheck` or define application readiness
or liveness policies.

## Diagnostic information

Machine names, execution identities, and payloads can contain sensitive
information. Applications exposing trace results should apply authorization
appropriate to their environment and keep secrets out of payloads.

Authorization belongs to the hosting application; this package is
transport-independent.

## Learn more

- [PingTrace overview](https://github.com/patware/Patware.PingTrace/blob/main/README.md)
- [Architecture](https://github.com/patware/Patware.PingTrace/blob/main/docs/architecture/ARCHITECTURE.md)
- [Execution model](https://github.com/patware/Patware.PingTrace/blob/main/docs/architecture/PINGTRACE.md)
- [Connector architecture](https://github.com/patware/Patware.PingTrace/blob/main/docs/architecture/PLUGIN-ARCHITECTURE.md)

# Capability boundaries

This page separates implemented behavior from product direction.

## Implemented

| Capability | Current behavior |
| --- | --- |
| Ping | Returns `Pong` without invoking dependencies |
| Local Trace | Returns one local `TraceResult` |
| Named local Trace | Returns a one-element collection |
| Named dependency Trace | Returns the local result followed by connector results |
| Default application name | Managed entry-assembly name |
| Registration | Parameterless or a `PingTraceBuilder` callback |
| Custom implementation | An existing DI registration of `IPingTrace` is preserved |
| HTTP endpoints | GET Ping and Trace, with an optional string route prefix |
| HTTP dependencies | Named clients request a remote local Trace endpoint |
| Cancellation | Passed from HTTP requests through runtime and connector calls |
| Default local payload | Entry-assembly version, falling back to Pong |
| EF Core destination | SQL Server diagnostic query through a registered DbContext |
| Tests | Separate Runtime, AspNetCore, HttpClient, and EntityFramework projects |
| Sample | Aspire frontend tracing its API; API tracing its configured SQL Server database |

The HTTP adapter expects the remote local Trace endpoint's current object
response. Changing that response to an array requires coordinated adapter,
endpoint, and test changes.

## Excluded from version 1

Destination discovery and the `TraceDestination` metadata model are excluded.

Applications explicitly register the dependencies they want to trace.

Aspire service discovery is a separate concern: it resolves service addresses
for HTTP clients. Using it does not introduce a PingTrace destination catalog.

## Intended next capabilities

| Capability | Direction |
| --- | --- |
| Trace All | Recursively invoke configured dependencies |
| Uniform Trace responses | Return collections consistently |
| Structured payload | Support an object payload |
| Trace helpers | Centralize result construction and completion |
| HTTP HEAD | Support HEAD on Ping in the ASP.NET Core adapter |
| Endpoint authorization | Allow anonymous Ping and protected Trace independently |
| Blazor viewer | Present results as a log, table, or dependency graph |

These capabilities are not available merely because they appear in examples
or design discussions.

## Open design decisions

### Recursive traversal

Define cycle detection, maximum depth, execution budgets, cancellation,
partial failures, and whether shared dependencies are visited per path.

Traversal context must work across process boundaries.

### Destination routing

The current runtime selects an immediate named connector. It does not
automatically route a request to a dependency behind another application.

A future path representation must make that routing explicit.

### Graph representation

A flat result collection does not encode a branching topology.

Future visualization needs a way to associate observations with paths or
parent-child relationships. That does not necessarily require restoring
permanent application IDs.

### Health Checks integration

PingTrace should complement health evaluation and follow familiar
registration conventions.

Whether to introduce an optional adapter or inherit from `IHealthCheck`
remains undecided. No integration is currently implemented.

### Database provider expansion

The implemented EF Core adapter reads SQL Server's server instance name,
current login name, and database name. The application measures timing.

Support for additional providers and configurable diagnostic queries remains
future design work. The existing generic DbContext constraint does not imply
provider-independent SQL.

### Custom registration

A generic `AddPingTrace<T>()` overload has been discussed but is not
implemented. Applications can currently register `IPingTrace` directly
before calling `AddPingTrace()`.

## Outside the initial scope

- Scheduling monitoring calls.
- Restarting or scaling applications.
- Defining universal health thresholds.
- Running business transactions as synthetic checks.
- Persisting diagnostic history.
- Providing a distributed tracing backend.

Applications and monitoring systems may build these capabilities around
PingTrace.

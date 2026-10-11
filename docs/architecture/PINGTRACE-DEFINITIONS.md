# PingTrace definitions

PingTrace provides application-level Ping and Trace operations for deployment
verification, monitoring, and troubleshooting.

This document describes the intended design. The implementation does not yet
provide every capability described here.

## Ping

Ping returns a predictable string with minimal work. The default response
is `Pong`.

Ping does not invoke dependencies or return diagnostic information such as
machine names, runtime identities, or payloads.

A successful Ping establishes that the application endpoint responds.
It does not establish that its dependencies are available or that a business
operation will succeed.

### HTTP HEAD support

The ASP.NET Core adapter is intended to support both GET and HEAD on the
Ping endpoint. Both invoke the same lightweight Ping operation. HEAD returns
the corresponding status and headers without a response body.

This supports monitoring clients that use HEAD while keeping HTTP concerns
outside the core PingTrace contracts. HEAD and GET share the endpoint's
authorization policy and do not invoke dependencies.

HEAD support does not imply a separate diagnostic level or a substantially
cheaper application execution path. Monitoring systems choose the method
and polling interval appropriate to their needs.

## Trace operations

All Trace operations return a collection of trace results, including a result
for the application receiving the request.

### Local Trace

Local Trace describes only the responding application. It does not invoke
dependencies.

Its result collection contains one element.

Local Trace provides a baseline when investigating whether a delay appears
locally or when contacting another system.

### Destination Trace

Destination Trace describes the local application and follows a selected
configured dependency path.

A dependency connector knows how to contact the next system and invoke its
Trace operation. Depending on the integration, this may involve HTTP, gRPC,
a database operation, or another protocol.

A destination identifies a configured target. It is not an arbitrary network
address supplied by the caller.

An unknown destination must be reported explicitly rather than silently
falling back to Local Trace.

### Trace All

Trace All describes the local application and recursively traces every
configured dependency known to each participating application.

A leaf application returns only its local result.

“All” refers to configured dependencies. It does not imply network scanning
or automatic discovery of unrelated systems.

The public method syntax remains to be finalized. Separate methods are
recommended:

- `TraceAsync()` for Local Trace.
- `TraceAsync(destination)` for Destination Trace.
- `TraceAllAsync()` for Trace All.

A distinct Trace All method makes its broader scope and greater potential
cost visible at the call site.

## Applications, instances, and destinations

An application is a logical component participating in PingTrace.

An instance is a running copy of an application. Multiple instances may
share an application name.

A destination is a configured target that an application knows how to invoke.

PingTrace does not require an application or destination ID. The proposed
design removes the existing `Id` properties.

Destination names identify configured targets. Machine names provide
additional information about responding instances, but are not guaranteed
to uniquely identify a process.

A future graph representation must preserve paths or parent-child
relationships. Names and array order alone are insufficient to reconstruct
a branching graph reliably.

## Trace results

A trace result contains observations reported by a participating component.

| Value | Meaning | Default |
| --- | --- | --- |
| Name | Application name | Application assembly name |
| Machine name | Machine reported by the responding process | `Environment.MachineName` |
| Identity | Runtime identity reported by the responding process | `$"{Environment.UserDomainName}\\{Environment.UserName}"` |
| Payload | Additional diagnostic information | Application assembly version |
| Started at | Time the local trace began | Captured when tracing begins |
| Finished at | Time the local trace completed | Captured when tracing ends |
| Elapsed | Duration of the local trace | Measured across the operation |

Assembly defaults refer to the application assembly, not the assembly
containing the PingTrace helper.

The implementation must establish how the application assembly is resolved
and allow overrides for hosting environments where it cannot be inferred
reliably.

The payload supports an object rather than only a string. Payloads returned
through HTTP must be serializable by the configured serializer.

The reported runtime identity does not necessarily identify the credential
used for an outgoing connection. A dependency connector may use a separate
database credential, certificate, or access token.

## Trace helpers

Trace helpers provide a consistent way to construct local and composite
results.

The following examples express the intended usage. They are pseudocode,
not examples supported by the current implementation.

### Local result

An application without dependencies can return a completed local trace:

```text
return TraceHelper.Trace()
```

`TraceHelper.Trace()` accepts optional overrides for:

- Name, defaulting to the application assembly name.
- Machine name, defaulting to `Environment.MachineName`.
- Identity, defaulting to the environment domain and user name.
- Payload, defaulting to the application assembly version.

### Composite result

An application invoking dependencies can begin a trace, add dependency
results, and complete its local result:

```text
trace = TraceHelper.Begin()

await trace.AddTraceAsync<DestinationA>()
await trace.AddTraceAsync<DestinationB>()

return trace.End()
```

`Begin()` accepts an optional application name and starts the local timing.

`End()` accepts optional machine name, identity, and payload overrides,
using the same defaults as the local helper. It completes the local timing
and returns the collected results.

The local result is included in the returned collection.

### Dependency invocation

Two dependency operations are proposed:

```text
await trace.AddTraceAsync<T>(destination)
await trace.AddTraceAllAsync<T>()
```

`T` identifies a connector that knows how to contact a dependency.

The recommended meaning of an omitted destination is to request the
target's Local Trace. `AddTraceAllAsync<T>()` requests recursive tracing
through that target.

Connectors are resolved through dependency injection so that they can
receive clients, database contexts, configuration, and other dependencies.

The recommended initial behavior is to execute and await each dependency
operation when it is added. Under this model, `End()` completes work that
has already been awaited.

Parallel execution remains a separate design decision.

## Registration

A leaf application using defaults should need only:

```csharp
builder.Services.AddPingTrace();
app.MapPingTrace();
```

An application requiring custom behavior can supply an implementation
and options:

```text
builder.Services.AddPingTrace<T>(options)
app.MapPingTrace(options)
```

In service registration, `T` represents the local application's PingTrace
implementation.

In a helper's dependency invocation, `T` represents a dependency connector.
These are distinct responsibilities.

An options-only registration overload is also recommended so that changing
a default does not require creating a custom implementation.

Service options configure runtime behavior. Endpoint options configure
HTTP exposure.

These examples describe the intended registration API.

## Timing

Local Trace measures local diagnostic work.

A composite trace includes the time spent awaiting its dependencies.
Parent and child elapsed times therefore overlap and must not simply
be added together.

Elapsed time is not a direct measurement of network latency. It can include:

- Connection establishment.
- Authentication.
- Request and response serialization.
- Network transit.
- Queuing and scheduling.
- Remote processing.

Comparing Local Trace with Destination Trace helps narrow an investigation.
Separate probes can help identify which dependency path introduces delay.

Differences between independent probes remain estimates because workload
and connection state may change between requests.

A monotonic timer is recommended for elapsed duration. Start and finish
timestamps provide time context, but system-clock adjustments should not
change the measured duration.

## Recursive execution

Trace All must account for dependency graphs that contain cycles or shared
dependencies.

For example, `A → B → A` contains a cycle. The paths `A → B → D` and
`A → C → D` share a dependency but do not necessarily contain a cycle.

Before recursive execution is implemented, the design must define:

- How traversal context is propagated between applications.
- How cycles are detected and reported.
- How execution depth and duration are bounded.
- How cancellation is propagated.
- How dependency failures and partial results are represented.
- Whether shared dependencies are invoked once per path or once per traversal.

These policies must work across process boundaries, not only within
one application's helper.

## Authorization

Ping returns a fixed response without diagnostic details and is intended
to support anonymous monitoring.

Trace and Trace All expose machine names, runtime identities, and potentially
sensitive payloads. Destination discovery can also expose application
topology.

Hosts should be able to protect diagnostic endpoints independently of Ping,
using familiar ASP.NET Core conventions such as `RequireAuthorization()`.

Endpoint mapping does not itself configure an authentication scheme or
create an authorization policy.

The exact mapping API remains to be finalized.

## Relationship to Health Checks

PingTrace complements health checks by reporting diagnostic observations.

Health checks interpret observations according to application policy and
produce a health classification.

PingTrace should follow familiar dependency-injection, typed-registration,
and endpoint-configuration patterns.

Integration with `IHealthCheck` remains under consideration. An optional
adapter is recommended initially so that health classification does not
become a mandatory responsibility of every PingTrace implementation.

Such an adapter must explicitly define which trace operation it invokes
and how it interprets failures or unexpected results.

## Database integration

A database connector may verify connectivity or invoke a diagnostic
operation implemented by the database.

An Entity Framework Core integration is a useful candidate because it can
use the application's configured `DbContext`.

Connectivity verification alone does not provide the database server's
machine name, execution identity, or diagnostic payload.

A richer database trace may require a dedicated query or stored procedure.
The connector must distinguish observations made by the caller from
information returned by the database.

## Monitoring and on-demand investigation

Ping supports frequent, inexpensive polling.

Trace All supports broader dependency verification at an interval appropriate
to its cost.

Additional destination probes used to investigate individual paths or build
a latency visualization belong to an explicit on-demand workflow.

PingTrace supplies responses and observations. Monitoring systems own
schedules, thresholds, notifications, and recovery actions.

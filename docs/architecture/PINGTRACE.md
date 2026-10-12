# Execution model

This page describes how the current default runtime processes Ping,
local Trace, and destination Trace requests.

Examples use the default `/pingtrace` route prefix and default ASP.NET Core
JSON settings. Hosts can change the route prefix, serialization settings,
and authorization policy.

## Operations at a glance

| Request | Dependency calls | Successful response |
| --- | --- | --- |
| `GET /pingtrace/Ping` | None | Plain-text `Pong` |
| `GET /pingtrace/Trace` | None | One local trace object |
| `GET /pingtrace/Trace?destination=LOCAL_NAME` | None | Array containing one local result |
| `GET /pingtrace/Trace?destination=DEPENDENCY_NAME` | Selected connector only | Array containing the local result followed by connector results |

The current implementation does not provide Trace All or automatic
recursive traversal.

## Before requests arrive

Service registration and endpoint mapping have separate responsibilities:

```csharp
builder.Services.AddPingTrace();
```

Registers the default runtime with dependency injection.

```csharp
app.MapPingTrace();
```

Maps incoming HTTP requests to the registered `IPingTrace` implementation.

For dependencies, the registration callback adds connectors:

```csharp
builder.Services.AddPingTrace(pingTrace =>
{
    pingTrace.AddHttpDestination("ApiService", client =>
    {
        client.BaseAddress = new Uri("https://api.example.com/");
    });
});
```

The callback runs during registration. It does not contact the dependency.

When the default runtime is resolved, it receives the registered connectors
and validates their names. Invalid connector registrations can therefore
prevent even a local operation from executing.

## Ping

Request:

```http
GET /pingtrace/Ping
```

Successful response:

```http
HTTP/1.1 200 OK
Content-Type: text/plain; charset=utf-8

Pong
```

The endpoint resolves `IPingTrace` and calls `Ping()`.

The default implementation returns a constant string. It does not create
trace results or invoke connector tracing methods.

Use this operation when the question is whether the application endpoint
responds. A successful response does not establish dependency availability.

## Local Trace

Request:

```http
GET /pingtrace/Trace
```

The absence of the `destination` query parameter selects local Trace.

The default runtime:

1. Checks cancellation.
2. Creates a result, capturing its start time.
3. Populates the application name, machine name, and runtime identity.
4. Calls `Finish` with the entry-assembly version, falling back to `Pong`, and captures the finish time.
5. Returns the result.

Example successful JSON response:

```json
{
  "name": "AspireAppSample.Web",
  "identity": "EXAMPLE\\web-service",
  "machineName": "WEB01",
  "startedAt": "2026-10-11T10:00:00.000-04:00",
  "finishedAt": "2026-10-11T10:00:00.001-04:00",
  "payload": "1.2.3.0",
  "elapsed": "00:00:00.0010000"
}
```

The response has status 200 and an `application/json` media type.

Values above are illustrative. Actual identity formatting and timestamp
offsets depend on the execution environment.

Version numbers in these examples are illustrative. Each application's
default payload comes from its own managed entry assembly, with `Pong`
used when its version is unavailable.

No dependency is contacted, even when connectors are registered.

## Named local Trace

Request:

```http
GET /pingtrace/Trace?destination=AspireAppSample.Web
```

If the destination matches the local application's name, ignoring case,
the runtime performs local Trace and wraps its result in a collection.

Example successful response:

```json
[
  {
    "name": "AspireAppSample.Web",
    "identity": "EXAMPLE\\web-service",
    "machineName": "WEB01",
    "startedAt": "2026-10-11T10:00:00.000-04:00",
    "finishedAt": "2026-10-11T10:00:00.001-04:00",
    "payload": "1.2.3.0",
    "elapsed": "00:00:00.0010000"
  }
]
```

The payload remains the local application version. The destination suffix
is added only when the runtime invokes a dependency connector.

The current difference between local-object and destination-array responses
is intentional in the implemented contract. Consumers must handle it.

A consistent collection response is planned but not implemented.

## Dependency Trace

In the Aspire sample, the frontend registers the API dependency under
the name `WeatherApiClient`.

Request to the frontend:

```http
GET /pingtrace/Trace?destination=WeatherApiClient
```

The runtime:

1. Checks cancellation and validates the destination argument.
2. Looks up the connector by name, ignoring case.
3. Creates the local result.
4. Awaits the connector's `TraceAsync` method.
5. Places the local result first and appends the connector results.
6. Completes the local result.
7. Returns the collection.

Example successful response:

```json
[
  {
    "name": "AspireAppSample.Web",
    "identity": "EXAMPLE\\web-service",
    "machineName": "WEB01",
    "startedAt": "2026-10-11T10:00:00.000-04:00",
    "finishedAt": "2026-10-11T10:00:00.025-04:00",
    "payload": "1.2.3.0 --> [WeatherApiClient]",
    "elapsed": "00:00:00.0250000"
  },
  {
    "name": "AspireAppSample.ApiService",
    "identity": "EXAMPLE\\api-service",
    "machineName": "API01",
    "startedAt": "2026-10-11T10:00:00.010-04:00",
    "finishedAt": "2026-10-11T10:00:00.011-04:00",
    "payload": "2.0.0.0",
    "elapsed": "00:00:00.0010000"
  }
]
```

The local alias `WeatherApiClient` selects the connector. It does not
replace the remote result's application name.

Only the selected connector is invoked. Other registered dependencies
are not traced.

## What the HTTP connector does

The built-in HTTP connector:

1. Checks cancellation.
2. Creates its named client through `IHttpClientFactory`.
3. Requires a configured base address.
4. Requests its configured trace path.
5. Deserializes one `TraceResult`.
6. Rejects a null result or an empty result name.
7. Returns a collection containing that result.

The default request to the dependency is:

```http
GET /pingtrace/Trace
```

The connector does not append the frontend's destination name to that request.

Consequently, the remote application performs local Trace. Its own
dependencies are not invoked.

A custom connector may return multiple results. The runtime preserves
their order but does not infer a graph from them.

## Destination interpretation

Destination names are application-owned aliases for registered connectors.

Matching is case-insensitive but does not trim surrounding whitespace.
For example, `WeatherApiClient` and `weatherapiclient` select the same
connector; ` WeatherApiClient ` does not.

Supply one destination query value and URL-encode it when necessary.

The current handler combines repeated query values into a string before
lookup. Repeating `destination` does not request multiple traces.

A destination value is not interpreted as an arbitrary URL or a discovered
route to another application.

## Errors

### Empty destination

Request:

```http
GET /pingtrace/Trace?destination=
```

Response status: 400.

With default JSON serialization, the response body is a JSON string:

```json
"Destination must not be empty."
```

A whitespace-only destination also produces status 400.

An empty parameter does not mean local Trace. Omit the parameter to request
local Trace.

### Unknown destination

Request:

```http
GET /pingtrace/Trace?destination=Missing
```

Response status: 404.

Default JSON response body:

```json
"The requested destination is not known."
```

The default runtime throws `KeyNotFoundException` for an unknown name.
The endpoint translates that exception into this response.

The catch surrounds destination execution, so a custom connector throwing
`KeyNotFoundException` would also be translated to 404.

### Dependency failure

Other failures propagate to the host's exception handling.

Examples include:

- An unavailable remote service.
- An unsuccessful remote HTTP status.
- Malformed JSON.
- A null or unnamed remote trace result.
- A missing HTTP client base address.

The library does not define a uniform HTTP error body or copy remote status
codes onto the frontend response.

For example, a remote HTTP 404 is an HTTP client failure. It is not treated
as proof that the frontend's destination alias is unknown.

If a connector fails, the default runtime does not return a partial
collection containing the local result.

## Cancellation

The HTTP endpoint passes `HttpContext.RequestAborted` to the runtime.

The runtime checks cancellation before executing a trace and passes the
token to the selected connector. The HTTP connector passes it to the
outgoing request and JSON-reading operation.

Cancellation propagates as an exception. PingTrace does not assign it
a dedicated HTTP response status or guarantee that a disconnected caller
receives a response.

Custom connectors must cooperate with cancellation. Passing a token does
not forcibly terminate work that ignores it.

## Timing interpretation

A result's constructor records `StartedAt`. Calling `Finish` records
`FinishedAt` and sets the payload.

`Elapsed` is calculated as:

```text
FinishedAt - StartedAt
```

For a dependency trace, the frontend's elapsed time includes awaiting the
connector. The remote application's elapsed time measures its local
diagnostic work.

These intervals overlap. Do not add them to calculate total duration.

The difference between frontend and remote elapsed times is not a pure
network-latency measurement. It can include client setup, authentication,
serialization, queuing, retries, and other processing.

The current timestamps use `DateTime.Now`. Clock adjustments can affect
duration calculations, and clocks on different machines need not agree.
Do not infer transit time by subtracting timestamps from different machines.

The result interval also excludes some HTTP work: it begins inside the
runtime and ends before final response serialization.

## Verify the Aspire sample

With the sample running, use the frontend address shown by Aspire.

1. Request `/pingtrace/Ping` and confirm `Pong`.
2. Request `/pingtrace/Trace` and inspect the frontend's local result.
3. Request `/pingtrace/Trace?destination=WeatherApiClient`.
4. Confirm that the array contains the frontend result followed by the
   API result.
5. Request `/pingtrace/Trace` directly from the API and compare its
   local observations.

Use the reported application name and machine to verify which component
answered. Use local versus dependency elapsed times to narrow a delay
investigation.

These requests verify the configured diagnostic path. They do not execute
the weather operation or establish that all application behavior is healthy.

## Related documentation

- [Architecture](ARCHITECTURE.md)
- [Definitions](PINGTRACE-DEFINITIONS.md)
- [Extension contracts](PLUGIN-ARCHITECTURE.md)
- [Distributed hosting](DISTRIBUTED-HOSTING.md)
- [Security](SECURITY.md)
- [Capability boundaries](../design/CAPABILITY-BOUNDARIES.md)

# PingTrace.Adapter.HttpClient

**Trace HTTP dependencies through explicitly configured clients.**

`PingTrace.Adapter.HttpClient` connects the PingTrace runtime to remote HTTP
Trace endpoints using `IHttpClientFactory`.

Each destination has its own local name and HTTP client configuration.
The library targets .NET 10.

## Register a destination

Reference this package and register an HTTP destination through
`AddPingTrace`:

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

Replace the example address with the remote application's address.

By default, the adapter sends a GET request to `/pingtrace/Trace`.
The remote endpoint must return one JSON object compatible with `TraceResult`.

Registration does not send a request. Communication occurs when the
application traces the registered destination.

## Expose the calling application's endpoints

For an ASP.NET Core application, also reference `PingTrace.AspNetCore`:

```csharp
using PingTrace.Adapter.HttpClient;
using PingTrace.AspNetCore;
using PingTrace.Runtime;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddPingTrace(pingTrace =>
{
    pingTrace.AddHttpDestination("ApiService", client =>
    {
        client.BaseAddress = new Uri("https://api.example.com/");
    });
});

WebApplication app = builder.Build();

app.MapPingTrace();

app.Run();
```

Then request:

```http
GET /pingtrace/Trace?destination=ApiService
```

With the default runtime, the response contains:

```text
Calling application's result
Remote application's result
```

The adapter returns only the remote result. The runtime adds and completes
the calling application's result.

Configure authorization in the hosting applications as appropriate for the
diagnostic information being exposed.

## Destination names and HTTP clients

The destination name is a local alias. It does not change the application
name returned by the remote endpoint.

Each registration creates:

- A named HTTP client using `PingTrace.Http:{name}`.
- A scoped `IPingTraceConnector` for that destination.

The default runtime compares destination names without regard to case.
Names must be unique and must not conflict with the calling application's
own name.

Register each destination once.

### Existing application clients

Using an existing class name as the alias is convenient:

```csharp
builder.Services.AddPingTrace(pingTrace =>
{
    pingTrace.AddHttpDestination(nameof(WeatherApiClient), client =>
    {
        client.BaseAddress = apiServiceUri;
    });
});
```

This does not resolve `WeatherApiClient` or copy its client configuration.
The PingTrace adapter creates a separate named client.

Share the address or configuration logic explicitly when both clients should
reach the same application. Configure any required authentication handlers
for the diagnostic client as well.

There is currently no overload that accepts an existing named or typed client.

## Configure the remote path

`AddHttpDestination` accepts an optional `tracePath`:

```csharp
builder.Services.AddPingTrace(pingTrace =>
{
    pingTrace.AddHttpDestination(
        "ApiService",
        client =>
        {
            client.BaseAddress = new Uri("https://api.example.com/");
        },
        tracePath: "/diagnostics/Trace");
});
```

Use this when the remote application maps PingTrace under a custom prefix.

### Applications hosted beneath a path

The default path begins with `/`, so it resolves from the server root.

To preserve a path in `BaseAddress`, give the base address a trailing slash
and supply a trace path without a leading slash:

```csharp
builder.Services.AddPingTrace(pingTrace =>
{
    pingTrace.AddHttpDestination(
        "ApiService",
        client =>
        {
            client.BaseAddress = new Uri("https://example.com/backend/");
        },
        tracePath: "pingtrace/Trace");
});
```

The resulting request targets:

```text
https://example.com/backend/pingtrace/Trace
```

| Base address | Trace path | Request address |
| --- | --- | --- |
| `https://example.com/backend/` | `/pingtrace/Trace` | `https://example.com/pingtrace/Trace` |
| `https://example.com/backend/` | `pingtrace/Trace` | `https://example.com/backend/pingtrace/Trace` |

The trace path must be nonblank. Registration rejects absolute URLs, paths
beginning with `//`, and paths containing backslashes.

## Configure client behavior

The client configuration callback can set properties such as the base address,
timeout, and default headers:

```csharp
builder.Services.AddPingTrace(pingTrace =>
{
    pingTrace.AddHttpDestination("ApiService", client =>
    {
        client.BaseAddress = new Uri("https://api.example.com/");
        client.Timeout = TimeSpan.FromSeconds(5);
    });
});
```

The timeout shown here is an application choice, not an adapter default.

`AddHttpDestination` returns `IHttpClientBuilder`, allowing the application to
configure HTTP handlers and other client services through standard client
registration extensions.

Authentication, resilience policies, and service discovery are configured by
the application. The adapter does not add them itself.

## Aspire service discovery

The repository's Aspire sample uses:

```csharp
Uri apiServiceUri = new("https+http://apiservice");

builder.Services.AddPingTrace(pingTrace =>
{
    pingTrace.AddHttpDestination(nameof(WeatherApiClient), client =>
    {
        client.BaseAddress = apiServiceUri;
    });
});
```

This address depends on the service discovery configured by the sample's
shared service defaults and the resource configuration supplied by its
AppHost.

Setting this URI alone does not configure service discovery.

The diagnostic client can receive application-wide HTTP client defaults,
but it does not inherit settings specific to the sample's typed
`WeatherApiClient`.

## Remote response contract

The adapter deserializes the response as one `TraceResult`.

It requires:

- A successful HTTP response.
- JSON that can be deserialized into `TraceResult`.
- A non-null result with a nonblank `Name`.

It returns that result as a one-element collection for the runtime.

The adapter does not validate every result field. For example, it does not
require a particular machine name, identity, payload, or elapsed value.

A destination Trace endpoint returns an array, so it is not the response
shape expected by this adapter. Configure the path to the remote application's
local Trace endpoint.

## Failures and cancellation

| Condition | Behavior |
| --- | --- |
| `BaseAddress` is missing | Throws `InvalidOperationException` when tracing. |
| Remote HTTP status is unsuccessful | The HTTP operation fails and its exception propagates. |
| Response JSON is invalid or incompatible | Deserialization fails and its exception propagates. |
| Deserialized result is null or has a blank name | Throws `InvalidOperationException`. |
| Network failure or timeout | The operation fails and its exception propagates. |

The connector checks cancellation before creating the client and passes the
token to the HTTP and JSON operation.

It does not replace failures with successful-looking trace results.

A client is obtained from `IHttpClientFactory` for each trace operation and
disposed when that operation finishes. Handler lifetime management remains
with the factory.

## Timing and traversal

With the default runtime, the calling application's elapsed time includes
the adapter operation, including the HTTP request and response processing.

The remote result contains the remote application's own timing.
Neither value directly measures network latency between the applications.

The adapter requests a local trace from one remote application. It does not
forward the destination alias, discover dependencies, or recursively traverse
the remote application's dependencies.

## Learn more

- [PingTrace overview](https://github.com/patware/Patware.PingTrace/blob/main/README.md)
- [Runtime registration and behavior](https://github.com/patware/Patware.PingTrace/blob/main/src/PingTrace.Runtime/README.md)
- [ASP.NET Core endpoints](https://github.com/patware/Patware.PingTrace/blob/main/src/PingTrace.AspNetCore/README.md)
- [Distributed hosting](https://github.com/patware/Patware.PingTrace/blob/main/docs/architecture/DISTRIBUTED-HOSTING.md)
- [Connector architecture](https://github.com/patware/Patware.PingTrace/blob/main/docs/architecture/PLUGIN-ARCHITECTURE.md)

# PingTrace.Adapter.EntityFramework

Trace a SQL Server database through the application's registered DbContext.

The adapter executes a diagnostic query and returns the server instance name,
current login name, and database name. It targets .NET 10.

## Provider support

The current implementation requires SQL Server. Although registration accepts
any DbContext type, the diagnostic query uses SQL Server-specific functions.

Referencing Microsoft.EntityFrameworkCore.Relational does not make this query
portable to other database providers.

## Register a destination

Reference PingTrace.Adapter.EntityFramework alongside the application's
SQL Server provider package.

Using the Aspire sample's existing ApiServiceDbContext:

```csharp
using Microsoft.EntityFrameworkCore;

using PingTrace.Adapter.EntityFramework;
using PingTrace.Runtime;

string? connectionString =
    builder.Configuration.GetConnectionString("ApiService");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'ApiService' is missing or empty.");
}

builder.Services.AddDbContext<ApiServiceDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services.AddPingTrace(pingTrace =>
{
    pingTrace.AddDbContextDestination<ApiServiceDbContext>("ApiServiceDb");
});
```

The application supplies the DbContext, provider configuration, and connection
string. AddDbContextDestination registers the diagnostic connector; it does
not configure the database or create its schema.

AddDbContextDestination returns PingTraceBuilder for further registrations.

## Invoke the destination

With the application's PingTrace endpoints mapped:

```http
GET /pingtrace/Trace?destination=ApiServiceDb
```

The default runtime returns two results:

1. The calling application's result.
2. The database result.

The application's payload contains its entry-assembly version followed by
` --> [ApiServiceDb]`. The database payload contains the database name.

The database result uses these values:

| Field | Source |
| --- | --- |
| Name | Registered destination alias, such as ApiServiceDb |
| MachineName | SQL Server's @@SERVERNAME |
| Identity | SQL Server's SUSER_NAME() |
| Payload | SQL Server's DB_NAME() |
| StartedAt and FinishedAt | Timestamps recorded by the calling application |

MachineName identifies the SQL Server instance and can include an instance
name. Identity describes the current SQL Server login context, not the
authenticated HTTP caller.

Null database values are represented as empty strings.

## What the query establishes

The adapter executes:

```sql
SELECT
    @@SERVERNAME AS [MachineName],
    SUSER_NAME() AS [Identity],
    DB_NAME() AS [Payload]
```

Success establishes that the configured connection can execute this diagnostic
query. It does not establish that application tables exist, migrations are
current, business queries succeed, or writes are permitted.

The adapter does not read application tables, create the database, apply
migrations, or modify application data.

No separate CanConnectAsync check is performed. The diagnostic query itself
exercises the connection and preserves query failures.

## Lifetime and naming

The connector is scoped. It resolves the registered DbContext only when its
destination is traced. Ping and local Trace do not resolve the context through
this connector.

The dependency-injection scope owns the context and its disposal. The
connector reuses that context and must not run concurrently with other
operations on the same instance.

Destination names are local aliases. The default runtime compares them without
regard to case and rejects duplicates or names conflicting with the local
application name.

With the default scoped AddDbContext registration, multiple aliases for the
same context type resolve the same context within a scope. Aliases do not
select different connection strings.

## Failures, cancellation, and timing

Context resolution, configuration, and query exceptions propagate to the
caller. The adapter does not convert failures into successful diagnostic rows.

The cancellation token is passed to SingleAsync. In-flight cancellation
depends on the database provider honoring it.

The database result's duration includes context resolution and query execution.
The calling application's duration also includes the connector operation.
These intervals overlap and are not direct measurements of network latency.

## Aspire sample

The API service registers ApiServiceDbContext under the alias ApiServiceDb.
Configure its ApiService connection string and request the API service's
`/pingtrace/Trace?destination=ApiServiceDb` endpoint.

Named tracing of WeatherApiClient from the frontend requests the API's local
Trace and stops there. Requesting the frontend's `/pingtrace/TraceAll` continues
through the API to ApiServiceDb.

The EF connector participates as a leaf and executes its existing diagnostic
query. No additional EF registration is required. The runtime visits destinations
sequentially, avoiding concurrent operations against a shared scoped DbContext.

## Tests

PingTrace.Adapter.EntityFramework.Tests exercises the public registration API
using an in-memory SQLite provider and a command interceptor.

The interceptor substitutes diagnostic rows or failures before the SQL
Server-specific command executes. These tests cover materialization, result
mapping, exception propagation, cancellation, and deferred context resolution.

A substituted reader must match the generated command's projection order.
The current projection uses Identity, MachineName, and Payload.

These isolated tests do not validate SQL Server syntax, authentication,
permissions, or connectivity. Validate those against an actual SQL Server
deployment.

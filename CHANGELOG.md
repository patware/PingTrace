# Changelog

User-visible changes are recorded here. `VERSION` is the current build version;
published releases are identified by package and release date below.

## Unreleased

### Changed

- Custom `IPingTrace` implementations must implement the new `TraceAllAsync` member.
- `AddHttpDestination` now accepts an optional `traceAllPath` parameter.
  Existing source calls remain valid; previously compiled consumers should
  be rebuilt against the updated packages.

- Default application traces now report the managed entry assembly's version,
  falling back to `Pong` when unavailable.
- A connector destination's local payload now uses
  `{version} --> [{destination}]`. Ping continues to return `Pong`.

### Added

- Recursive `IPingTrace.TraceAllAsync` with sequential traversal of registered
  destinations and per-path visits to shared dependencies.
- `TraceAllRequest`, `IRecursivePingTraceConnector`, and singleton `TraceAllNode`
  support for traversal context and cycle detection across request scopes.
- Default traversal limits of eight connector hops and a thirty-second
  cooperative deadline. Cycles, exhausted depth, and failures stop traversal
  without returning partial results.
- GET and POST `/pingtrace/TraceAll` endpoints and configurable outgoing
  `traceAllPath` support in the HTTP adapter.
- TraceAll participation by existing leaf connectors, including EF diagnostics.

- `PingTrace.Adapter.EntityFramework` with
  `AddDbContextDestination<TContext>(name)` for SQL Server diagnostics through
  an existing DbContext registration.
- Database results containing the server instance name, current login name,
  and database name.
- An `ApiServiceDb` destination in the Aspire API sample.
- Isolated EF adapter tests for result mapping, query failure propagation,
  cancellation, and deferred context resolution.

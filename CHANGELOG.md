# Changelog

User-visible changes are recorded here. `VERSION` is the current build version;
published releases are identified by package and release date below.

## Unreleased

### Changed

- Default application traces now report the managed entry assembly's version,
  falling back to `Pong` when unavailable.
- A connector destination's local payload now uses
  `{version} --> [{destination}]`. Ping continues to return `Pong`.

### Added

- `PingTrace.Adapter.EntityFramework` with
  `AddDbContextDestination<TContext>(name)` for SQL Server diagnostics through
  an existing DbContext registration.
- Database results containing the server instance name, current login name,
  and database name.
- An `ApiServiceDb` destination in the Aspire API sample.
- Isolated EF adapter tests for result mapping, query failure propagation,
  cancellation, and deferred context resolution.

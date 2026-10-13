# Build and run locally

Run the commands below from the repository root.

The libraries and sample applications target .NET 10. Library development,
sample execution, documentation generation, and packaging have different
requirements; use the workflow appropriate to your change.

## Prerequisites

For library development:

- A stable .NET 10 SDK compatible with `global.json`.
- Access to the package sources required for restore.
- An editor or IDE that supports the selected SDK.

The SDK configuration requests version `10.0.200`, permits roll-forward
to later .NET 10 feature bands, and excludes preview SDKs.

Check the SDK selected from the repository directory:

```powershell
dotnet --version
```

The PowerShell scripts in this guide are intended for PowerShell 7 or later.

To run the Aspire sample, also provide:

- A working container runtime supported by Aspire, such as Docker Desktop,
  to run the sample's Redis resource.
- An environment capable of running the Aspire AppHost.
- A trusted development certificate when using the HTTPS launch profile.
- The API's `ConnectionStrings:ApiService` setting. Development configuration
  supplies a SQL Server LocalDB connection string; use an accessible SQL Server
  database to exercise database tracing. The AppHost does not provision it.

The Graph destination is simulated and needs no Microsoft account, tenant,
credentials, or Microsoft network access. Other sample prerequisites still apply.

DocFX is needed only when generating the documentation site.

## Repository layout

| Directory | Purpose |
| --- | --- |
| `src` | PingTrace libraries |
| `tests` | Isolated library test projects |
| `SampleApplications/AspireAppSample` | Aspire frontend, API, shared defaults, AppHost, and sample tests |
| `docs` | Documentation source and DocFX configuration |
| `eng` | Release and packaging support scripts |

The solution file is `PingTrace.slnx`.

## Build the libraries

To build every library without building the sample applications:

```powershell
$libraryProjects = @(
    Get-ChildItem ./src -Recurse -Filter *.csproj |
        Sort-Object FullName
)

if ($libraryProjects.Count -eq 0) {
    throw "No library projects were found."
}

foreach ($project in $libraryProjects) {
    dotnet build $project.FullName --configuration Release

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed: $($project.FullName)"
    }
}
```

`dotnet build` restores dependencies unless instructed otherwise.

This builds the six current libraries:

- `PingTrace.Core`
- `PingTrace.Runtime`
- `PingTrace.AspNetCore`
- `PingTrace.Adapter.HttpClient`
- `PingTrace.Adapter.EntityFramework`
- `PingTrace.Adapter.MicrosoftGraph`

A running container runtime is not required for this workflow.

## Build the solution

To include the sample and test projects:

```powershell
dotnet restore PingTrace.slnx
```

After restore succeeds:

```powershell
dotnet build PingTrace.slnx --configuration Release --no-restore
```

Building the solution does not start the sample or run tests.

For Visual Studio, open `PingTrace.slnx` with a version that supports
the selected SDK and solution format.

## Run library tests

For one suite:

```powershell
dotnet test --project tests/PingTrace.Runtime.Tests/PingTrace.Runtime.Tests.csproj --configuration Release -- --minimum-expected-tests 1
```

See [Testing](TESTING.md) for running all library suites and writing tests.

The library tests use in-memory dependencies and do not require Aspire,
Redis, or a running API.

The separate Aspire sample test starts application resources and has
different environment requirements.

## Run the Aspire sample

Start the container runtime before launching the AppHost.

For local HTTPS development on Windows, trust the development certificate
if it has not already been trusted:

```powershell
dotnet dev-certs https --trust
```

Launch the sample from the repository root:

```powershell
dotnet run --project SampleApplications/AspireAppSample/AspireAppSample.AppHost/AspireAppSample.AppHost.csproj --launch-profile https
```

Alternatively, select `AspireAppSample.AppHost` as the startup project
in Visual Studio and use its `https` launch profile.

The AppHost starts the frontend, API, and Redis resource. Use the dashboard
address printed during startup, then open the frontend through its resource
endpoint.

Do not use the dashboard's own address as the PingTrace application address.

The launch profile sets the environment to Development. This matters because
the sample's `/health` endpoints are mapped only in that environment, and
the AppHost uses those endpoints for health checks.

### Verify PingTrace

Using the frontend's address, request:

```text
/pingtrace/Ping
```

Expected body:

```text
Pong
```

Next, request:

```text
/pingtrace/Trace
```

Expect one local trace object identifying `AspireAppSample.Web`.

Then request:

```text
/pingtrace/Trace?destination=WeatherApiClient
```

Expect an array containing the frontend result followed by the
`AspireAppSample.ApiService` result.

The API also exposes its own `/pingtrace/Ping` and `/pingtrace/Trace`
endpoints.

The sample's logical API address, `https+http://apiservice`, relies on
the service discovery configured by the sample. Starting the frontend
alone does not automatically reproduce the AppHost-provided configuration.

See [Distributed hosting](../architecture/DISTRIBUTED-HOSTING.md) for the
registration and communication details.

### Exercise the simulated Microsoft Graph destination

Open the **apiservice** resource endpoint from the Aspire dashboard and request:

```text
/pingtrace/Trace?destination=MicrosoftGraph
```

Expect an array containing the API's local result followed by a Graph result:

- `Name`: `MicrosoftGraph`
- `MachineName`: `graph.simulated`
- `Identity`: `SIMULATED; tenant:sample-tenant; app:sample-app`
- `Payload`: a JSON-encoded string containing the simulated tenant ID,
  `Contoso — simulated organization`, and `contoso.example`.

The request uses the real Graph adapter and SDK. The API's
`Services/Simulator/SimulatedGraphHttpMessageHandler.cs` supplies the response
without opening a network connection, and anonymous authentication avoids token
acquisition. The hostname is a diagnostic label, not an endpoint to open in a
browser. The simulation is registered unconditionally in the current sample;
it is not a fallback after a real connection fails.

In `AspireAppSample.ApiService/appsettings.Development.json`, edit:

```json
"GraphSimulation": {
  "Scenario": "Success",
  "DelayMilliseconds": 100
}
```

This is a section of the existing JSON object; preserve the other settings.
The handler reads configuration on each request. Restart the API if a change
has not been reloaded or inspect higher-priority configuration overrides.

| Scenario | Simulated behavior |
| --- | --- |
| `Success` | One organization with ID, display name, and verified domain |
| `Unauthorized` | Graph-shaped HTTP 401 error |
| `Forbidden` | Graph-shaped HTTP 403 error |
| `Unavailable` | Graph-shaped HTTP 503 error |
| `Empty` | Successful HTTP response with no organizations; adapter validation fails |
| `Malformed` | Successful HTTP response with invalid JSON; deserialization fails |

Scenario names are case-insensitive. Unknown names and negative delays fail
explicitly. Delay is applied per handler invocation and observes cancellation.
The named HTTP client inherits the sample's standard resilience settings, so
retryable failures can invoke the handler repeatedly; measured duration need
not equal the configured delay. Resilience timeouts can also end long delays.

The current endpoint propagates dependency exceptions to host error handling.
A simulated Graph 401 or 403 normally results in a PingTrace HTTP 500, not a
matching HTTP status. Inspect API logs for the underlying Graph error.

Ping and local Trace do not invoke Graph. `/pingtrace/TraceAll` invokes both
`ApiServiceDb` and `MicrosoftGraph`, and stops on a dependency failure. Use the
destination-specific request above to exercise Graph without querying SQL
Server; the API still requires its connection-string setting at startup.

The simulation validates registration, SDK response handling, adapter mapping,
and the HTTP endpoint path. It does not validate real authentication,
permissions, DNS, TLS, or Microsoft service behavior, and it does not create a
separate Aspire resource. Consumers replace the sample's client registration
with their authenticated Graph client while retaining the same adapter.

## Preview the documentation

Documentation source is under `docs`. Edit Markdown and the relevant
`toc.yml` files, not generated HTML.

With DocFX installed and available on PATH:

```powershell
docfx build docs/docfx.json --serve
```

Open the local address printed by DocFX. Rebuild after changing source
files to refresh the generated site.

The configured output directory is `docs/_site`.

### Current API-reference limitation

The current `docs/docfx.json` metadata configuration has an empty input
file list. It does not yet identify the libraries from which to generate
API reference pages.

The preview command builds site content; it does not establish that API
metadata is correctly configured or up to date.

API-reference links can remain unresolved until the metadata inputs are
configured and generation succeeds.

The current configuration also does not establish the compiled-assembly
and XML-documentation workflow described in earlier documentation.

DocFX is not registered in the repository's current tool manifest.
Restoring repository tools alone will not install it.

## Create local packages

Version defaults come from the repository's `VERSION` file through
`Directory.Build.props`.

After successful builds and tests, pack the library projects explicitly:

```powershell
$libraryProjects = @(
    Get-ChildItem ./src -Recurse -Filter *.csproj |
        Sort-Object FullName
)

if ($libraryProjects.Count -eq 0) {
    throw "No library projects were found."
}

foreach ($project in $libraryProjects) {
    dotnet pack $project.FullName --configuration Release --output artifacts/packages

    if ($LASTEXITCODE -ne 0) {
        throw "Packing failed: $($project.FullName)"
    }
}
```

This creates local package artifacts. It does not publish them.

Inspect package contents and dependencies before treating them as release
artifacts. A successful pack operation does not prove that the packages
can be consumed by another application.

Per-library README files and their package metadata are still being
established. Do not assume a README is bundled until the project configuration
and resulting package confirm it.

### Current script limitations

`Build.ps1` still contains an outdated library list. It names a `PingTrace`
project that does not exist and omits current libraries.

Use the explicit commands above until that list is corrected.

`eng/Test-Packages.ps1` currently defines helper functions but does not
perform package verification. A successful invocation is not evidence
that package contents or consumption have been tested.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| The SDK cannot be selected | Installed SDKs and the repository's `global.json` |
| Restore fails | Package-source access and credentials, where applicable |
| The sample cannot start Redis | Container runtime availability |
| The sample waits for healthy resources | Resource logs, application startup, and the Development environment |
| A browser rejects local HTTPS | Development certificate trust and the address being used |
| Local Trace works but the API destination fails | API resource status, service discovery, client configuration, and remote endpoint |
| Documentation API links are missing | DocFX metadata inputs and generation output |

When using a case-sensitive filesystem, solution project paths must match
directory casing exactly. Correct any `Tests` versus `tests` mismatch
before relying on a solution build on that platform.

## Related documentation

- [Testing](TESTING.md)
- [Development conventions](CODING-STANDARDS.md)
- [Architecture](../architecture/ARCHITECTURE.md)
- [Execution model](../architecture/PINGTRACE.md)
- [Distributed hosting](../architecture/DISTRIBUTED-HOSTING.md)

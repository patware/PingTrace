# Tests

The library suites use MSTest.Sdk and Microsoft.Testing.Platform.
The repository's `global.json` selects the test runner.

## Projects

| Project | Coverage |
| --- | --- |
| `PingTrace.Runtime.Tests` | Local tracing, connector selection, cancellation, failures, and service registration |
| `PingTrace.Adapter.HttpClient.Tests` | Named clients, URI construction, response parsing, HTTP failures, and cancellation |
| `PingTrace.AspNetCore.Tests` | Routing, response formats, validation, host error handling, and adapter-to-endpoint compatibility |
| `PingTrace.Adapter.EntityFramework.Tests` | Database result mapping, query failures, cancellation, and deferred context resolution |

The adapter-to-endpoint round-trip test currently belongs to
`PingTrace.AspNetCore.Tests`. There is no separate
`PingTrace.IntegrationTests` project.

The Core library currently has no dedicated test project.

## Run one suite

From the repository root:

```powershell
dotnet test --project tests/PingTrace.Runtime.Tests/PingTrace.Runtime.Tests.csproj --configuration Release -- --minimum-expected-tests 1
```

Replace the project path to select another suite.

## Run all library suites

```powershell
$testProjects = Get-ChildItem ./tests -Recurse -Filter *.csproj

foreach ($project in $testProjects) {
    dotnet test --project $project.FullName --configuration Release -- --minimum-expected-tests 1

    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed: $($project.FullName)"
    }
}
```

This selects projects under `tests`. It does not run the Aspire sample's
separate test project under `SampleApplications`.

## Isolation

Library tests use substitutes, in-memory HTTP handlers, and ASP.NET Core
TestServer.

They do not require an external API, database server, Redis instance, or
Aspire host.

The endpoint tests are integration tests at the ASP.NET Core boundary.
Their in-memory hosting does not validate a deployed IIS configuration,
real service discovery, TLS, or production authentication.

EF adapter tests use an in-memory SQLite provider for relational infrastructure.
A command interceptor substitutes readers or failures before the SQL
Server-specific command executes. The substituted reader must match EF's
generated projection order. These tests do not validate SQL Server syntax,
connectivity, authentication, or permissions.

## Conventions

- Include Arrange, Act, and Assert sections.
- Document public test classes and members.
- Put relevant design justification in XML remarks.
- Pass `TestContext.CancellationToken` to asynchronous operations.
- Keep state local to each test to support parallel execution.
- Use explicit completion signals instead of sleeps.
- Avoid assertions that depend on machine speed or network latency.

The current response contract is intentionally tested: local Trace returns
an object, while destination Trace returns an array. Update the producer,
consumer, and assertions together if that contract changes.

## Validation claims

A passing test run establishes the behavior covered by that run.
Do not infer deployment readiness, package validity, or coverage percentages
without performing the corresponding checks.

# Tests

TODO: Testing instructions

## Run library tests

The library test projects enable the MSTest executable runner.
global.json selects Microsoft.Testing.Platform for dotnet test.

Run one suite:

```powershell
dotnet test --configuration Release -- --minimum-expected-tests 1
```

Build the libraries, run every library suite, and pack:

```powershell
$packages = ./Build.ps1
```

Build.ps1 discovers test projects independently of library names, so it
includes Pipeline.Transport.Tests. Any failed suite stops packaging.
Each suite must discover at least one test.

## Coverage and conventions

The existing coverage.runsettings file and XPlat Code Coverage collector apply to VSTest. For a built library assembly:

```powershell
dotnet vstest --Settings:tests/coverage.runsettings --Collect:"XPlat Code Coverage" --ResultsDirectory:TestResults
```

Tests use Arrange, Act, and Assert sections and pass TestContext.CancellationToken through asynchronous operations.

## What the suites establish

## Limits

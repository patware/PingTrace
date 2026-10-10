# Build and run locally

Use a stable .NET 10 SDK compatible with [global.json](https://github.com/patware/Patware.PingTrace/blob/main/global.json): configured minimum feature band 10.0.200, `latestFeature` roll-forward, no previews. Run these commands from the repository root:

```powershell
dotnet restore PingTrace.slnx
dotnet build PingTrace.slnx --configuration Release --no-restore
# See TESTING.md for separate library runner commands.
```

## Documentation and packages

With DocFX available on PATH:

```powershell
dotnet build PingTrace.slnx --configuration Release
docfx docs/docfx.json
```

This generates API metadata from the compiled Release assemblies and adjacent XML documentation, then builds Markdown/TOCs. Assembly input avoids incompatibilities between DocFX's bundled compiler and the SDK Razor generator. Edit source Markdown/XML comments and rebuild; `docs/api`, `docs/_site`, and `docs/.cache` are ignored generated output. Add new pages to section `toc.yml` files.

The prepared release version is 0.0.1, sourced from [VERSION](https://github.com/patware/Patware.PingTrace/blob/main/VERSION) through [Directory.Build.props](https://github.com/patware/Patware.PingTrace/blob/main/Directory.Build.props). Rebuild after changing VERSION before packing. Pack the library projects explicitly; the solution also contains Sample applications (under SampleApplications) hosts and tests:

```powershell
Get-ChildItem src -Directory |
    ForEach-Object {
        $project = Join-Path $_.FullName ($_.Name + '.csproj')
        dotnet pack $project --configuration Release --output artifacts/packages
        if ($LASTEXITCODE -ne 0) { throw "Packing failed: $project" }
    }
```

Inspect package versions, dependencies, bundled README files, and release notes before publishing. See [CHANGELOG.md](https://github.com/patware/Patware.PingTrace/blob/main/CHANGELOG.md). Packing creates local artifacts and does not publish packages.

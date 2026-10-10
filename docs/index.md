# PingTrace

These guides describe the 0.0.1 implementation in `src/`, reviewed on 2026-10-10. They distinguish current behavior and implementation limits responsibilities. Publication is pending; VERSION identifies the prepared build.

Browse the [Core API reference](xref:PingTrace.Core) or start with the library matching your task:

| Library | Purpose |
| --- | --- |

## Documentation guide

Start with the architecture overview to understand the library boundaries, or the development setup to build and run the demo. Design documents explain source-derived choices rather than claiming historical approval of formal decisions.

| Area | Documents |
| --- | --- |
| Architecture | [Library architecture](architecture/ARCHITECTURE.md), [principles](architecture/ARCHITECTURAL-PRINCIPLES.md), and [execution model](architecture/PINGTRACE.md) |
| Definitions and execution | [Definitions](architecture/PINGTRACE-DEFINITIONS.md) |
| Persistence and integrations | [extension contracts](architecture/PLUGIN-ARCHITECTURE.md) |
| Security and observability | [Security](architecture/SECURITY.md) |
| Development | [Getting started](development/GETTING-STARTED.md), [coding guidance](development/CODING-STANDARDS.md) and [testing](development/TESTING.md) |
| Design | [Implemented decisions](design/ADOPTION-DECISIONS.md) and [capability boundaries](design/CAPABILITY-BOUNDARIES.md). |

## Generate this documentation

With the .NET 10 SDK and DocFX installed, run these commands from the repository root:

```powershell
dotnet restore PingTrace.slnx
dotnet build PingTrace.slnx --configuration Release --no-restore
docfx docs/docfx.json
```

DocFX reads the compiled Release assemblies and their adjacent XML documentation files. This includes generated Razor components without requiring DocFX's bundled compiler to run the SDK's Razor generator. Rebuild after source/XML documentation changes. DocFX writes metadata to `docs/api` and HTML to `docs/_site`; both are ignored generated output. See [DocFX assembly input](https://dotnet.github.io/docfx/docs/dotnet-api-docs.html).

To preview the site locally after building the assemblies, run:

```powershell
docfx docs/docfx.json --serve
```

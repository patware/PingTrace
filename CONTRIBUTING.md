# Contributing

Bug reports, documentation improvements, and focused pull requests are welcome.

Participation is covered by the [Code of Conduct](CODE_OF_CONDUCT.md).

## Discuss a change

Use [GitHub issues](https://github.com/patware/Patware.PingTrace/issues) for
reproducible bugs and feature proposals.

For a bug report, include:

- The affected package and version.
- The .NET SDK and hosting environment.
- Expected and actual behavior.
- A minimal reproduction.
- Relevant error messages, with sensitive information removed.

For a feature proposal, explain the application scenario and the problem it
would solve. Discuss substantial public API or architectural changes before
implementing them so the intended behavior and scope are clear.

Report vulnerabilities using [SECURITY.md](SECURITY.md).

## Set up the repository

Install a stable .NET 10 SDK compatible with [global.json](global.json).
Use an editor or IDE that supports the selected SDK and, when opening the
solution, the `.slnx` format.

Follow [Getting started](docs/development/GETTING-STARTED.md) for:

- Building the libraries or full solution.
- Running the Aspire sample.
- Previewing documentation.
- Creating local packages.

Running the Aspire sample has additional requirements, including a container
runtime for Redis. Isolated library development and tests do not require
running the sample.

The getting-started guide also documents current limitations in the build
scripts and DocFX configuration. Use its explicit build and packaging commands
until those limitations are resolved.

## Make a focused change

Follow the [branch guide](branch-guide.md). Start a focused branch from an
up-to-date `main` and submit the pull request back to `main`.

Keep the change limited to the problem being addressed. Separate unrelated
cleanup or refactoring so each change is easier to understand and review.

Follow [.editorconfig](.editorconfig) and the
[Coding standards](docs/development/CODING-STANDARDS.md).

Keep responsibilities in the appropriate library:

| Library | Responsibility |
| --- | --- |
| `PingTrace.Core` | Transport-independent contracts and trace results. |
| `PingTrace.Runtime` | Default behavior, registration, and trace orchestration. |
| `PingTrace.AspNetCore` | HTTP endpoint mapping and responses. |
| `PingTrace.Adapter.HttpClient` | Communication with HTTP destinations. |

Applications own destination configuration, credentials, and authorization
policies.

Document publicly visible types and members with XML documentation. Use
`<remarks>` where design justification or limitations help consumers understand
the API.

## Verify the behavior

Build the affected projects and run the relevant tests. For changes shared
across libraries, run all library test suites.

See [Testing](docs/development/TESTING.md) for commands and conventions.

Great unit tests document themselves: **test method names should document what
each test verifies.**

Use `// Arrange`, `// Act`, and `// Assert` sections. Add or update tests for
changed behavior without maintaining a separate prose inventory of test cases.

Run the Aspire sample or its tests when a change affects sample integration.
Distinguish isolated library tests from checks that exercise running
application resources.

For documentation-only changes, check examples, relative links, and navigation.
Preview the DocFX site when changing site content or navigation.

Report what was actually verified. If an applicable check could not be run,
state that limitation and its reason.

## Update documentation

Keep documentation consistent with implemented behavior.

- Use the root README to introduce PingTrace and its primary usage.
- Use library README files for package-specific guidance as they are added.
- Use `docs` for architecture, design decisions, and development guidance.
- Update the relevant `toc.yml` when adding or moving documentation pages.
- Update XML documentation when a public contract changes.

Clearly distinguish available features from proposals. Examples should use
APIs that exist in the version being documented.

Record user-visible changes under **Unreleased** in
[CHANGELOG.md](CHANGELOG.md). Describe the effect on consumers, including
compatibility changes where applicable.

## Submit a pull request

Lead the description with the problem and resulting behavior.

Include the following where relevant:

- Why the change is needed.
- Public API or compatibility implications.
- Projects and configurations built or tested.
- Sample, documentation, or package checks performed.
- Remaining limitations or checks that could not be completed.

Link the related issue when one exists. Keep the description focused on the
final change so a reviewer can understand it without reading the development
conversation.

Keep credentials, connection strings containing secrets, build output, and
generated reports out of commits.

## Versions and packages

[Directory.Build.props](Directory.Build.props) reads the default shared version
from [VERSION](VERSION). Do not introduce separate library version values for
an ordinary contribution.

Version changes and publication belong to release preparation, as described in
the [branch guide](branch-guide.md).

For packaging changes, follow the local packaging instructions in
[Getting started](docs/development/GETTING-STARTED.md). Inspect the resulting
package contents and verify consumption when relevant.

A successful pack operation establishes that a package was created; package
contents and consumer behavior require their own verification.

## Related documentation

- [Getting started](docs/development/GETTING-STARTED.md)
- [Coding standards](docs/development/CODING-STANDARDS.md)
- [Testing](docs/development/TESTING.md)
- [Architecture](docs/architecture/ARCHITECTURE.md)
- [Architectural principles](docs/architecture/ARCHITECTURAL-PRINCIPLES.md)

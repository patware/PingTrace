# Coding standards

PingTrace favors small, explicit implementations with clear responsibilities.
Code should be easy to understand, inexpensive to execute, and straightforward
for application developers to adopt.

## Formatting and naming

Follow the repository’s [.editorconfig](../../.editorconfig) and the surrounding
code. Treat `.editorconfig` as the source of truth for formatting rather than
maintaining a second set of detailed formatting rules here.

Current C# preferences include:

- Four-space indentation.
- File-scoped namespaces.
- Using directives outside the namespace.
- Explicit types for built-in types.
- Braces for control-flow statements.
- Readonly fields when their values do not change after construction.

Use names that describe responsibilities and behavior. Name asynchronous methods
with the `Async` suffix.

Respect the settings of the project being changed. Nullable reference types and
implicit usings are enabled in several projects, but are not enabled uniformly
across the repository.

## Library responsibilities

Keep each concern in the library that owns it.

| Library | Responsibility |
| --- | --- |
| `PingTrace.Core` | Transport-independent contracts and trace results. |
| `PingTrace.Runtime` | Default behavior, dependency registration, and trace orchestration. |
| `PingTrace.AspNetCore` | HTTP endpoint mapping and translation of results into HTTP responses. |
| `PingTrace.Adapter.HttpClient` | Communication with HTTP destinations. |

Applications own their topology, destination addresses, credentials, and
authorization policies.

Keep HTTP-specific concepts out of the core contracts. A connector owns the
communication needed to reach a dependency; the runtime owns the local trace
result and combines it with the connector’s results.

This separation allows applications to use different dependency technologies
without changing the core tracing model.

## Public API documentation

Document publicly visible types and members with XML documentation, including
public members in test projects.

Use:

- `<summary>` to describe the purpose or behavior.
- `<param>` for each parameter.
- `<returns>` for a return value.
- `<exception>` for exceptions that form part of the documented contract.
- `<remarks>` for useful design justification, limitations, or usage details.

Use `<see cref="..."/>` and `<paramref name="..."/>` when referring to symbols and
parameters. Use `<inheritdoc/>` when inherited documentation accurately describes
the implementation.

Documentation should explain the contract without narrating the implementation.
When a design choice needs justification, record that justification in
`<remarks>` near the relevant API.

For example:

```csharp
/// <summary>
/// Traces the destination represented by this connector.
/// </summary>
/// <param name="cancellationToken">
/// A token that cancels the operation.
/// </param>
/// <returns>
/// The trace results returned by the destination.
/// </returns>
/// <remarks>
/// The caller adds its own local trace result. Keeping that responsibility
/// outside the connector allows different transports to share the same
/// orchestration behavior.
/// </remarks>
Task<IReadOnlyList<TraceResult>> TraceAsync(
    CancellationToken cancellationToken = default);
```

Complete public XML documentation avoids missing-comment warnings such as
CS1591 when XML documentation generation is enabled.

Documentation generation and warning enforcement depend on project settings.
Do not assume every project currently generates XML documentation or treats
documentation warnings as errors.

## Validation and failures

Validate public arguments at the boundary that owns them. Use standard guard
clauses where practical, and provide exception messages that help identify the
invalid value or configuration.

Keep validation focused on the current operation. Avoid introducing additional
configuration or abstractions for hypothetical future requirements.

Preserve useful failure information. Do not catch an exception solely to return
a successful-looking trace result.

Translate failures into transport-specific responses in the transport layer.
For example, HTTP status codes belong in `PingTrace.AspNetCore`, not in
`PingTrace.Core`.

## Asynchronous operations and cancellation

Use asynchronous APIs for network and other asynchronous I/O.

Accept a `CancellationToken` where an operation supports cancellation, and pass
it through to downstream operations. ASP.NET Core endpoints should propagate the
request cancellation token.

Avoid blocking asynchronous operations with `.Wait()` or `.Result`. Do not wrap
asynchronous I/O in `Task.Run`.

Cancellation should remain observable to the caller. Do not turn it into a
successful trace response.

## Dependency injection and resource ownership

Use dependency injection for runtime dependencies. Keep registration code
concerned with service configuration and construction.

Keep constructors inexpensive. Do not contact destinations during registration
or service construction; perform communication when the trace operation is
invoked.

Follow the existing service lifetimes and registration behavior unless a change
explicitly requires revisiting them.

Make resource ownership clear. Dispose resources owned by the implementation,
and let the dependency injection container manage services that it owns.

## Diagnostic behavior

Keep Ping inexpensive and predictable. It should not contact dependencies or
perform business operations.

Keep local tracing distinct from destination tracing. Changes to traversal,
result ordering, payloads, or failure behavior affect consumers and should be
treated as contract changes.

Trace information can include machine names, execution identities, and
application-provided payloads. Avoid including credentials, tokens, or other
secrets.

Document implemented behavior accurately. Proposed capabilities belong in
design documentation until they are available in the public API.

## Unit tests

Great unit tests document themselves: **test method names should document what
each test verifies.**

Use descriptive names that identify the scenario and expected behavior. Avoid
maintaining a separate prose inventory of individual test cases.

Organize test bodies with:

```csharp
// Arrange

// Act

// Assert
```

Keep tests focused and independent. Use controlled substitutes for external
dependencies when testing library behavior, and avoid timing-dependent tests
that rely on arbitrary delays.

Pass `TestContext.CancellationToken` to asynchronous operations when applicable.
Retain concise XML documentation for publicly visible test types and members.

See [Testing](TESTING.md) for the test project organization and execution
instructions.

## Reviewing a change

Keep changes focused on the intended behavior. Update public XML documentation
and relevant Markdown when a contract or usage pattern changes.

Run the checks appropriate to the affected projects. Test names should make the
verified behavior clear, while documentation should explain how and why a
consumer uses the feature.

See [Getting started](GETTING-STARTED.md) for the development workflow.

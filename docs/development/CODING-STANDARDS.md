# Development conventions

Follow [.editorconfig](https://github.com/patware/Patware.PingTrace/blob/main/.editorconfig) and surrounding code. Projects enable nullable references and implicit usings. Preserve asynchronous APIs, cancellation propagation, and guard clauses. Public XML comments feed DocFX and must reflect behavior.

TODO: qualify the nullable/implicit-usings claim, since not all projects enable them.

## Layer ownership

Include XML documentation for public/protected members and design justification in remarks where useful. Tests use Arrange, Act, and Assert comments and propagate TestContext.CancellationToken to asynchronous operations.

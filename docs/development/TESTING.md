# Testing strategy

## Runners

```powershell
dotnet test
```

See [Microsoft runner guidance](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test). A unified solution command requires migrating every suite to a compatible runner; documentation changes do not perform that migration.

## Regression targets

| Suite | Targets |
| --- | --- |

Tests use Arrange, Act, and Assert sections and propagate TestContext.CancellationToken.

Coverage figures in older reports are historical, not fresh results. Merge reports rather than adding percentages when referenced libraries appear in several suites.

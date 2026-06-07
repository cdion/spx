---
description: 'Use when writing or editing tests, deciding unit vs integration, choosing the correct test project, or selecting the narrowest validation command for this repo.'
name: 'Spx Testing And Validation'
applyTo: 'tests/**'
---

# Spx Testing And Validation

## Test Placement

- Default to unit tests for behavior owned by application code.
- Use integration tests only when the risk is owned by EF Core, ASP.NET Identity, endpoint binding, redirects, or Orleans runtime behavior.
- Keep integration tests narrow and seam-focused.

### Test Project Map

- `tests/Spx.Account.Application.Tests`: pure account handlers.
- `tests/Spx.Game.Domain.Tests`: pure game rule helpers.
- `tests/Spx.Game.Application.Tests`: pure game application handlers and helpers.
- `tests/Spx.Game.Application.IntegrationTests`: EF-backed game persistence and query behavior.
- `tests/Spx.Web.Tests`: endpoint mapping, redirect/query behavior, Identity-backed adapters, and other web-owned seams.
- `tests/Spx.Grains.Tests`: focused Orleans grain behavior.

### Decision Procedure

1. If the behavior is decided in application code without a real framework dependency, add a unit test.
2. If the risk is owned by EF Core, ASP.NET Identity, endpoint binding, redirects, or Orleans runtime semantics, add a focused integration test.
3. Keep integration tests narrow. Do not move pure handler logic into integration tests just because the caller is a web endpoint.
4. After the first substantive edit, run the narrowest executable validation that can falsify the change.

## Validation Rules

- After a substantive edit, run the narrowest executable validation that can falsify the change.
- Prefer the touched test project over a repo-wide test run.
- Use `dotnet tool restore` before coverage or EF tool workflows.

## Component Testing (bUnit & Playwright)

### Never select by CSS class or element type

Class names change with Tailwind refactors and carry no semantic contract. Use `data-testid` exclusively.

### Boundary: UI tests vs domain logic tests

Component tests in `Spx.Web.Tests` verify UI behavior: rendering, click routing, tab switching, and state transitions through the component layer. They must **not** re-test domain rules that are already covered by domain test projects (`Spx.Game.Domain.Tests`, `Spx.Game.Application.Tests`).

- **UI layer owns**: component rendering, event callback invocation, state machine transitions (`NexusGameplayPanelState.*`), layout logic, CSS class selection, data-testid presence.
- **Domain layer owns**: cost calculations, combat formulas, movement rules, victory conditions, gate progress mechanics.

When writing a UI test that depends on a domain value (e.g., a unit's energy cost), use soft assertions (`Assert.True(count > 0)`) rather than hard-coding the expected domain value (`Assert.Equal(6, count)`). If a test's primary assertion is about a domain calculation result, the test belongs in a domain test project, not in `Spx.Web.Tests`.

### Pattern: TestIds class + local selector helper

Every testable component in `Spx.Web.Components` has a companion `{Name}TestIds.cs` static class (same folder, same namespace) owning all test ID strings:

```csharp
// NexusGameplayPanelTestIds.cs
public static class NexusGameplayPanelTestIds
{
    public const string SubmitOrdersButton = "nexus-submit-orders";
    public static string ResolveEventRow(int index) => $"nexus-resolve-event-{index}";
}
```

Razor file applies `data-testid` via the class:

```razor
<button data-testid="@NexusGameplayPanelTestIds.SubmitOrdersButton" ...>
```

Tests declare a local helper:

```csharp
private static string TestIdSelector(string testId) => $"[data-testid='{testId}']";

// usage
cut.Find(TestIdSelector(NexusGameplayPanelTestIds.SubmitOrdersButton)).Click();
```

### Rules

- Add `data-testid` and the TestIds class as part of the same PR as the test — don't leave a component without test IDs if a test targets it.
- `const string` for fixed IDs. `static string` methods for IDs varying by index/type/coordinates.
- TestIds class lives in `Spx.Web.Components` namespace/assembly (same folder as the component), not in the test project.
- Never hard-code raw ID strings in test files — always go through the TestIds class.
- Kebab-case with component prefix: `nexus-submit-orders`, not `submit-orders`.

### Playwright browser tools

```python
# Preferred
page.locator('[data-testid="nexus-submit-orders"]')

# Avoid
page.locator('.ui-button-primary')
page.get_by_text('Submit Orders')  # acceptable only when no testid exists
```

If the element lacks a `data-testid`, add one (and the TestIds class) before writing the browser test.

## Test Method Naming

Follow `<Method>_<Condition>_<Expected>` in snake_case:

```csharp
HandleAsync_returns_validation_failure_for_short_game_name()
ShouldResetUiState_WhenGameIdChangesWithSameRound_ReturnsTrue()
ApplySelectionRequest_WhenClearSelection_ResetsSelectedSystem()
```

- Use `[Fact]` for single-case tests.
- Use `[Theory]` + `[InlineData]` for parameterized cases.

## Test Doubles

- Prefer `NSubstitute` for lightweight mocking in unit-style tests when it is clearer than a hand-rolled fake or stub.
- Match the repo-standard package version when adding it to a test project: `NSubstitute` `5.3.0`.
- If a test uses `Substitute` or `Arg`, add `using NSubstitute;` in the file or the test project's global usings.
- Keep direct stub classes for simple coordinator/state tests when they are easier to read than a mock setup.

## Build Quality Gate

- `Directory.Build.props` sets `AnalysisMode=Recommended` and `EnforceCodeStyleInBuild=true` for every project. CA and IDE rule violations are **build errors**, not warnings.
- The pre-commit hook runs `dotnet build -warnaserror` across the solution. A warning that passes locally may still block a commit.
- CA1848 (calling `ILogger.Log*` directly) is one of the most commonly triggered rules — use `[LoggerMessage]` static partial methods in a `partial class` to satisfy it.
- CSharpier is the formatter. Run `dotnet csharpier format <path>` for touched files or `dotnet csharpier format .` for the repo. The installed pre-commit hook runs `dotnet csharpier format "$REPO_ROOT"` before build and test.

## Preferred Validation Order

1. Targeted unit or integration test for the touched slice.
2. Focused build or compile check for the touched project if no narrow test exists.
3. Broad test runs only after the touched slice is green.

## Common Commands

- `dotnet test tests/Spx.Account.Application.Tests/Spx.Account.Application.Tests.csproj`
- `dotnet test tests/Spx.Game.Domain.Tests/Spx.Game.Domain.Tests.csproj`
- `dotnet test tests/Spx.Game.Application.Tests/Spx.Game.Application.Tests.csproj`
- `dotnet test tests/Spx.Game.Application.IntegrationTests/Spx.Game.Application.IntegrationTests.csproj`
- `dotnet test tests/Spx.Web.Tests/Spx.Web.Tests.csproj`
- `dotnet test tests/Spx.Grains.Tests/Spx.Grains.Tests.csproj`
- `dotnet tool restore`

## References

- `TESTING.md`
- `README.md`

---
description: 'Use when writing or editing tests, deciding unit vs integration, choosing the correct test project, or selecting the narrowest validation command for this repo.'
name: 'Spx Testing And Validation'
applyTo: 'tests/**'
---

# Spx Testing And Validation

- Default to unit tests for behavior owned by application code.
- Use integration tests only when the risk is owned by EF Core, ASP.NET Identity, endpoint binding, redirects, or Orleans runtime behavior.
- Keep integration tests narrow and seam-focused.

## Test Project Map

- `tests/Spx.Account.Application.Tests`: pure account handlers.
- `tests/Spx.Game.Domain.Tests`: pure game rule helpers.
- `tests/Spx.Game.Application.Tests`: pure game application handlers and helpers.
- `tests/Spx.Game.Application.IntegrationTests`: EF-backed game persistence and query behavior.
- `tests/Spx.Web.Tests`: endpoint mapping, redirect/query behavior, Identity-backed adapters, and other web-owned seams.
- `tests/Spx.Grains.Tests`: focused grain behavior.

## Validation Rules

- After a substantive edit, run the narrowest executable validation that can falsify the change.
- Prefer the touched test project over a repo-wide test run.
- Use `dotnet tool restore` before coverage or EF tool workflows.

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

## Blazor Component Tests (bUnit — `Spx.Web.Tests`)

### Strongly prefer `data-testid` selectors

**Never select elements by CSS class name or element type.** Class names change with Tailwind refactors and carry no semantic contract. Use `data-testid` attribute selectors exclusively when you need to locate an element.

#### Pattern: TestIds class + local selector helper

Every testable component in `Spx.Web.Components` has a companion `*TestIds.cs` static class (same folder, same namespace) that owns all test ID strings:

```csharp
// NexusGameplayPanelTestIds.cs
public static class NexusGameplayPanelTestIds
{
    public const string SubmitOrdersButton = "nexus-submit-orders";       // static ID
    public static string ResolveEventRow(int index) => $"nexus-resolve-event-{index}"; // parameterized
}
```

The Razor file applies `data-testid` to each testable element:

```razor
<button data-testid="@NexusGameplayPanelTestIds.SubmitOrdersButton" ...>
```

Tests declare a local helper and call `cut.Find(...)` with it:

```csharp
private static string TestIdSelector(string testId) => $"[data-testid='{testId}']";

// usage
cut.Find(TestIdSelector(NexusGameplayPanelTestIds.SubmitOrdersButton)).Click();
cut.Find(TestIdSelector(NexusGameplayPanelTestIds.ResolveEventRow(0))).TextContent;
```

### Rules

- **Add `data-testid` attributes to a component before writing a test that targets it.** If a component you need to test is missing IDs, add them (and the TestIds class) as part of the same PR.
- Use `const string` for fixed IDs. Use `static string` methods for IDs that vary by index, type, or coordinates.
- Keep TestIds class names in the component's namespace and assembly (`Spx.Web.Components`), not in the test project — the component and the test share the same string via the class.
- Never hard-code raw ID strings in test files; always go through the TestIds class.

## Integrated Browser Tests (Playwright tools)

When using browser tools (`run_playwright_code`, `read_page`, etc.) to validate Blazor interactive pages, prefer `data-testid` selectors over CSS class selectors or element text:

```python
# Preferred
page.locator('[data-testid="nexus-submit-orders"]')

# Avoid — brittle to CSS refactors
page.locator('.ui-button-primary')
page.get_by_text('Submit Orders')   # acceptable only when no testid exists
```

If the element you need lacks a `data-testid`, add one to the component and its TestIds class before writing the browser test.

## Common Commands

- `dotnet test tests/Spx.Account.Application.Tests/Spx.Account.Application.Tests.csproj`
- `dotnet test tests/Spx.Game.Domain.Tests/Spx.Game.Domain.Tests.csproj`
- `dotnet test tests/Spx.Game.Application.Tests/Spx.Game.Application.Tests.csproj`
- `dotnet test tests/Spx.Game.Application.IntegrationTests/Spx.Game.Application.IntegrationTests.csproj`
- `dotnet test tests/Spx.Web.Tests/Spx.Web.Tests.csproj`
- `dotnet test tests/Spx.Grains.Tests/Spx.Grains.Tests.csproj`

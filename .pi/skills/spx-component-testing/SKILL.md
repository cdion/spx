---
name: spx-component-testing
description: 'Write bUnit tests for Blazor components following Spx conventions. Use when adding or updating component tests in Spx.Web.Tests, or adding data-testid attributes to components. Trigger words: bUnit, component test, data-testid, TestIds class, cut.Find, Blazor test, Playwright browser test.'
argument-hint: 'Describe the component being tested and the interaction or assertion needed.'
---

# Spx Component Testing (bUnit & Playwright)

## Never select by CSS class or element type

Class names change with Tailwind refactors and carry no semantic contract. Use `data-testid` exclusively.

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

## Playwright browser tools

```python
# Preferred
page.locator('[data-testid="nexus-submit-orders"]')

# Avoid
page.locator('.ui-button-primary')
page.get_by_text('Submit Orders')  # acceptable only when no testid exists
```

If the element lacks a `data-testid`, add one (and the TestIds class) before writing the browser test.

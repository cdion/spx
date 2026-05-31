---
description: 'Use when writing or editing Blazor pages or components, deciding whether to extract state or a coordinator, choosing a parameter shape, or reviewing @code block complexity.'
name: 'Spx Web Component State Patterns'
applyTo: '{src/Spx.Web/**,src/Spx.Web.Components/**}'
---

# Blazor Component State Patterns

There are three distinct layers. Choose the right pattern based on the layer, not the file size.

---

## Layer 1 — `Spx.Web.Components` (no DI, no I/O)

Components here are pure UI. They have no access to application handlers and no DI container.

### Simple component — inline `@code`
Default. Use when the component has at most one state concern and no complex state transitions.

```razor
@code {
    [Parameter] public string Title { get; set; } = string.Empty;

    private bool _expanded;
    private void Toggle() => _expanded = !_expanded;
}
```

### Complex component with multiple orthogonal state machines — static reducer
When a component's `@code` block contains two or more independent state machines (e.g. selection + event focus + order draft), extract the logic to a companion `ComponentNameState.cs` file containing a `static` class with pure functions.

The component owns the mutable fields. The static class provides pure transition functions.

```csharp
// NexusGameplayPanelState.cs
public static class NexusGameplayPanelState
{
    public static SelectionState ApplySelectionRequest(SelectionState state, SelectionRequest request) => ...
    public static EventFocusState ApplyEventFocusRequest(EventFocusState state, EventFocusRequest request) => ...
}
```

**Do not use a static reducer for a single state concern** — inline is clearer.

### Parameter bags
When a component takes many related parameters that always travel together, group them into an `init`-only class and accept a single parameter.

```csharp
// LobbyMessagesState.cs
public sealed class LobbyMessagesState
{
    public required string CurrentUserName { get; init; }
    public IReadOnlyList<TimelineEntryState> Items { get; init; } = [];
    // ...
}
```

Parameter bags are not stateful objects. All properties are `init`-only.

---

## Layer 2 — `Spx.Web` pages (DI available, async I/O)

Pages here constructor-inject application handlers and have async lifecycles.

### Simple page — inline `@code`
Default. Use when the page calls one handler and the entire code block fits comfortably.

```razor
@code {
    [Inject] private IGetGameListHandler Handler { get; set; } = null!;

    private UserGamesView? _games;

    protected override async Task OnInitializedAsync() =>
        _games = await Handler.HandleAsync();
}
```

### Complex page with multiple async lifecycles — coordinator + state objects
When a page has three or more orthogonal async concerns (data load, user actions, background polling), split into:

- **State objects** — mutable bags with `Begin*/Apply*/Fail*` mutation methods. One per concern.
- **Coordinators** — classes that call handlers, catch exceptions, log, and mutate state. Constructed inline in `@code` (not DI-registered).

```csharp
// NexusPageDataCoordinator.cs
internal sealed partial class NexusPageDataCoordinator(
    IGetNexusPageHandler handler,
    ILogger<NexusPageDataCoordinator> logger,
    NexusPageDataState state
)
{
    public async Task LoadPageAsync(Guid gameId, string userId, CancellationToken ct = default)
    {
        state.BeginPageLoad();
        try { state.ApplyPage(await handler.HandleAsync(gameId, userId, ct)); }
        catch (Exception ex) { LogLoadFailed(logger, ex, gameId); state.FailPageLoad("..."); }
    }
}
```

```razor
@code {
    private readonly NexusPageDataState _data = new();
    private NexusPageDataCoordinator DataCoordinator => new(Handler, Logger, _data);
}
```

**Only use this pattern for genuinely complex pages.** `NexusPage` is currently the only page that needs it.

---

## Decision table

| Situation | Pattern |
|---|---|
| Simple component, 1 state concern | Inline `@code` |
| Component with 2+ orthogonal state machines | Static reducer in companion `.cs` |
| Component takes 6+ related parameters | Parameter bag (`init`-only class) |
| Simple page, 1 handler | Inline `@code` |
| Page with 3+ orthogonal async lifecycles | Coordinator + state objects |

---

## Testability: `data-testid` attributes

Any element that a bUnit test (or browser-level test) needs to locate **must** have a `data-testid` attribute. Do not rely on CSS class names or element types — they change with Tailwind refactors and carry no semantic contract.

### Companion TestIds class

For every component in `Spx.Web.Components` that has testable elements, create a companion static class in the same folder and namespace:

```csharp
// NexusGameplayPanelTestIds.cs  (same folder as NexusGameplayPanel.razor)
namespace Spx.Web.Components.Nexus;

public static class NexusGameplayPanelTestIds
{
    // Fixed IDs: use const string
    public const string SubmitOrdersButton = "nexus-submit-orders";
    public const string MapBackground = "nexus-map-background";

    // Parameterized IDs: use static string methods
    public static string ResolveEventRow(int index) => $"nexus-resolve-event-{index}";
    public static string System(HexCoord coord) => $"nexus-map-system-q{coord.Q}-r{coord.R}";
}
```

Apply the ID in the Razor file via the class:

```razor
<button data-testid="@NexusGameplayPanelTestIds.SubmitOrdersButton" ...>
<div data-testid="@NexusGameplayPanelTestIds.ResolveEventRow(index)" ...>
```

### Rules

- Create the `*TestIds.cs` file when you first add `data-testid` attributes to a component — not inside the test project.
- `const string` for stable, non-parameterized IDs. `static string` methods for IDs that depend on index, type, or coordinates.
- Name IDs using kebab-case with a component prefix to avoid collisions (e.g. `nexus-submit-orders`, not just `submit-orders`).
- Never hard-code raw ID strings in test files; always reference the TestIds class.
- If a test targets an element that lacks a `data-testid`, add the attribute (and update the TestIds class) before writing the test.

---

## What does NOT warrant extraction

- A large component with a single concern (e.g. complex JS interop): keep inline.
- A coordinator for a page that just calls one or two handlers: keep inline `@code`.
- A state reducer for a component that only has computed properties: keep inline.

The test: if you removed the extracted file and inlined it, would the result be confusing? If no, don't extract.

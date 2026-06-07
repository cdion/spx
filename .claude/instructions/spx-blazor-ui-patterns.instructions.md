---
name: spx-blazor-ui-patterns
description: 'Choose the right Blazor component or page state pattern.'
applyTo: '{src/Spx.Web/**/*.razor,src/Spx.Web.Components/**/*.razor}'
---

# Spx Blazor UI Patterns

## Layer 1 — Spx.Web.Components (no DI, no I/O)

| Situation | Pattern |
|---|---|
| ≤1 state concern | Inline `@code` |
| 2+ orthogonal state machines | Static reducer in companion `{Name}State.cs` — pure functions, component owns mutable fields |
| 6+ related parameters travelling together | `init`-only parameter bag class |

### Static reducer example

```csharp
// NexusGameplayPanelState.cs
public static class NexusGameplayPanelState
{
    public static SelectionState ApplySelectionRequest(SelectionState state, SelectionRequest request) => ...
    public static EventFocusState ApplyEventFocusRequest(EventFocusState state, EventFocusRequest request) => ...
}
```

### Parameter bag example

```csharp
// LobbyMessagesState.cs
public sealed class LobbyMessagesState
{
    public required string CurrentUserName { get; init; }
    public IReadOnlyList<TimelineEntryState> Items { get; init; } = [];
}
```

## Layer 2 — Spx.Web pages (DI, async I/O)

| Situation | Pattern |
|---|---|
| 1–2 handlers, fits comfortably | Inline `@code` |
| 3+ orthogonal async concerns (data load, user actions, background polling) | Coordinator + state objects |

### Coordinator pattern

State objects: mutable bags with `Begin*/Apply*/Fail*` mutation methods. One per concern.
Coordinators: classes that call handlers, catch exceptions, log, and mutate state. Constructed inline in `@code` (not DI-registered).

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

## Parameter and callback naming

- `EventCallback<T> On<Action>` — for actions the parent handles (e.g., `OnSelectionRequested`, `OnSubmitOrders`, `OnEventFocusRequested`).
- `EventCallback On<State>Changed` or `On<State>Cleared` — for state-change signals (e.g., `SelectedTabChanged`, `GameplayErrorCleared`).
- Use typed `EventCallback<T>` when the payload is a typed request or model; use untyped `EventCallback` only for pure signals with no data.

## UI intent: Request records

UI intent (user action before it becomes a domain command) is modeled as a `*Request` record with a `*Kind` enum and static factory methods:

```csharp
public enum SelectionRequestKind { SelectSystem, ClearSelection }

public sealed record SelectionRequest(SelectionRequestKind Kind, HexCoord? System)
{
    public static SelectionRequest SystemSelected(HexCoord coord) => new(SelectionRequestKind.SelectSystem, coord);
    public static SelectionRequest SelectionCleared() => new(SelectionRequestKind.ClearSelection, null);
}
```

- Components raise a `*Request`; the page/coordinator translates it to a domain `*Command` before calling a handler.
- `*Request` lives in `Spx.Web.Components` (no DI, no I/O). `*Command` lives in `Spx.Nexus.Domain` (or `Spx.Game.Application`).
- Never pass a domain command directly as an `EventCallback` parameter — that couples the component to the application layer.

## What does NOT warrant extraction

- Large component with a single concern (complex JS interop): keep inline.
- Page calling 1–2 handlers: keep inline `@code`.
- Component with only computed properties: keep inline.

Test: if you removed the extracted file and inlined it, would the result be confusing? If no, don't extract.

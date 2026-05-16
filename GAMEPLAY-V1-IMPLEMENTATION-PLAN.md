# Gameplay V1 Implementation Plan

## Summary

Replace the current RPS-shaped session model with the agreed phase-based crafting game by changing the session contracts and Orleans grain state machine first, then adapting application handlers and the Blazor gameplay panel around the new phase/view model.

Reuse the existing lobby, presence, invalidation, and timeline infrastructure where possible, and validate primarily with focused grain and game-application tests before touching broader web behavior.

## Steps

1. Phase 1: Replace the shared gameplay model in contracts and pure domain types.

   Update the current session-facing models so they represent phases, market state, hands, card instances, pending batches, resolution summaries, initiative state, and `Victory` status instead of RPS moves and round outcomes. Keep the grain interface shape reusable, but replace command/query payloads and view models. This phase blocks all later work.

2. Phase 1: Define card and phase primitives in the shared model.

   Introduce the agreed concepts explicitly: action/resource/effect card categories, card instance identity, base and refined resources, market state, pending batch zone, acquire initiative scoring, and `Victory` production requirements. This depends on step 1 and should be kept in the shared contracts or adjacent pure domain model where serialization boundaries require it.

3. Phase 2: Replace the pure session engine in the grain layer.

   Rework `GameSessionEngine` and `GameSessionGrainState` to drive the loop `Acquire -> Play -> Resolve -> WinCheck`, including initial empty hands, initial market reveal, two acquire rounds per acquire phase with fixed pick order, effect-first batch resolution, action-card recycle-to-market behavior, effect destruction, and the pass-twice stalemate rule. This depends on steps 1-2.

4. Phase 2: Add the new command paths on the grain boundary.

   Replace move submission with explicit acquire-pick and play-batch submission commands and keep query access for current session state. Preserve `InitializeAsync` and `AbandonAsync`, but update their semantics for the new session state. This depends on step 3.

5. Phase 2: Implement pure helpers for batch validation and initiative scoring.

   Pull as much rule-heavy logic as possible into pure helpers invoked by the grain/engine so the hard parts remain unit-testable without Orleans runtime setup. This can proceed in parallel with step 4 once the shared model is stable.

6. Phase 3: Update the application layer to match the new runtime contract.

   Replace `SubmitGameMove` outcomes and handlers with phase-specific handlers and outcomes for acquire picks and play-batch locks, update `IGameSessionService`, and keep invalidation publishing behavior so opponent clients still refresh promptly. This depends on steps 3-5.

7. Phase 3: Update page assembly and session-fetch handlers.

   Adapt `GetGamePageHandler`, `GetGameSessionHandler`, and any neighboring view-model assembly so they surface the new phase-aware `GameSessionView` while preserving lobby/presence composition and existing session initialization on second-player join. This depends on step 6.

8. Phase 3: Extend gameplay timeline events.

   Add new event/message kinds and mapping so acquire actions, batch locks or resolutions, and victory/draw outcomes can appear in the existing timeline without replacing the current chat/message infrastructure. This depends on steps 3 and 6 but can run in parallel with step 7.

9. Phase 4: Replace the RPS gameplay panel in the web layer.

   Rebuild the gameplay UI around the new session model: market view, hand view, initiative/pick-order display, acquire action, batch selection and locking, pending batch state, and resolution summary. Keep `GamePage.razor` as the orchestration shell and reuse invalidation subscription, presence, roster, and chat/timeline components. This depends on steps 6-8.

10. Phase 4: Update the Orleans web adapter and page orchestration.

    Extend `OrleansGameRuntimeClient` and the page handlers so the UI can submit acquire picks and play batches, reload the current phase state on invalidation, and preserve the current refresh model instead of introducing a separate polling mechanism. This depends on step 9.

11. Phase 5: Add focused tests by seam.

    Put pure gameplay rule tests in the game-application test project where possible, focused Orleans session-state tests in the grains test project, and only seam-specific web tests for the updated gameplay panel/adapter behavior. Favor testing the pure engine and helper logic first, then grain behavior, then web integration. This depends on steps 3-10.

12. Phase 5: Run narrow validation in dependency order.

    Start with `tests/Spx.Game.Application.Tests` for pure helpers/handlers, then `tests/Spx.Grains.Tests` for state-machine behavior, then `tests/Spx.Web.Tests` only after the web layer is updated. Use broader validation only after those slices are green.

## Relevant Files

- `/run/host/var/home/chris/dev/spx/GAMEPLAY-V1-SPEC.md` — source-of-truth rules to map directly into models, commands, handlers, and UI states.
- `/run/host/var/home/chris/dev/spx/src/Spx.Contracts/GameSessionModels.cs` — current RPS-shaped session contracts to replace with phase/card/batch models.
- `/run/host/var/home/chris/dev/spx/src/Spx.Contracts/IGameSessionGrain.cs` — grain interface shape to evolve for acquire-pick and play-batch commands.
- `/run/host/var/home/chris/dev/spx/src/Spx.Grains/GameSessionGrain.cs` — current persistent state plus `GameSessionEngine`; primary runtime rewrite site.
- `/run/host/var/home/chris/dev/spx/src/Spx.Game.Domain/GameRoundResolver.cs` — current RPS resolver to delete or replace with batch-resolution and initiative helper logic.
- `/run/host/var/home/chris/dev/spx/src/Spx.Game.Application/IGameSessionService.cs` — application/runtime seam to update for new phase operations.
- `/run/host/var/home/chris/dev/spx/src/Spx.Game.Application/GamePageModels.cs` — current move outcome models to replace with acquire/play outcomes and updated session page shape.
- `/run/host/var/home/chris/dev/spx/src/Spx.Game.Application/Features/SubmitGameMove/SubmitGameMoveHandler.cs` — current move-submission path to replace or supersede with new handlers.
- `/run/host/var/home/chris/dev/spx/src/Spx.Game.Application/Features/GetGameSession/GetGameSessionHandler.cs` — query handler that should keep working with the new session view.
- `/run/host/var/home/chris/dev/spx/src/Spx.Game.Application/Features/GetGamePage/GetGamePageHandler.cs` — page assembly path that currently ensures session initialization and composes lobby/session/presence.
- `/run/host/var/home/chris/dev/spx/src/Spx.Game.Application/Features/JoinGame/JoinGameHandler.cs` — keep the current `ensure session on second player join` behavior aligned with the new session model.
- `/run/host/var/home/chris/dev/spx/src/Spx.Game.Application/GameEnums.cs` — extend with new timeline/event kinds for gameplay system events.
- `/run/host/var/home/chris/dev/spx/src/Spx.Data/GameMessageFactory.cs` — likely extension point for persisted gameplay timeline events.
- `/run/host/var/home/chris/dev/spx/src/Spx.Web/Adapters/Games/OrleansGameRuntimeClient.cs` — update web/runtime adapter methods for acquire and play commands.
- `/run/host/var/home/chris/dev/spx/src/Spx.Web/Components/Pages/GamePage.razor` — orchestration shell to keep while swapping the gameplay interaction model.
- `/run/host/var/home/chris/dev/spx/src/Spx.Web/Components/Pages/GameSessionPanel.razor` — current RPS-specific UI that should be replaced by phase-aware gameplay rendering.
- `/run/host/var/home/chris/dev/spx/src/Spx.Web/Components/Pages/GameTimelinePanel.razor` — reusable timeline surface that needs new gameplay event kinds only.
- `/run/host/var/home/chris/dev/spx/src/Spx.Web/Components/Pages/GamePageSubscription.cs` — invalidation subscription infrastructure that should be reused unchanged if possible.
- `/run/host/var/home/chris/dev/spx/tests/Spx.Grains.Tests/GameSessionGrainTests.cs` — current focused session-state tests to replace with new phase-loop grain tests.
- `/run/host/var/home/chris/dev/spx/tests/Spx.Game.Application.Tests/SubmitGameMoveHandlerTests.cs` — move-handler coverage to replace with new acquire/play handler tests.
- `/run/host/var/home/chris/dev/spx/tests/Spx.Game.Application.Tests/GetGameSessionHandlerTests.cs` — query seam tests to keep and adapt.
- `/run/host/var/home/chris/dev/spx/tests/Spx.Game.Application.Tests/GameRoundResolverTests.cs` — current pure rule tests to replace with new pure gameplay helper/engine tests.
- `/run/host/var/home/chris/dev/spx/tests/Spx.Web.Tests` — seam-specific web tests for updated gameplay UI/adapter behavior.

## Verification

1. Run focused pure-rule and handler validation first: `dotnet test tests/Spx.Game.Application.Tests/Spx.Game.Application.Tests.csproj`.
2. Run focused Orleans session validation next: `dotnet test tests/Spx.Grains.Tests/Spx.Grains.Tests.csproj`.
3. Run web seam validation once the panel and adapter changes land: `dotnet test tests/Spx.Web.Tests/Spx.Web.Tests.csproj`.
4. If timeline/event persistence changes touch EF-backed seams, run `dotnet test tests/Spx.Game.Application.IntegrationTests/Spx.Game.Application.IntegrationTests.csproj`.
5. Use a final targeted manual dev-loop check through the AppHost to verify market refill, acquire initiative, batch locking, invalidation refresh, and timeline event rendering in the actual UI.

## Decisions

- Scope includes only the gameplay/session rewrite implied by `GAMEPLAY-V1-SPEC.md`, plus the minimum timeline/event updates needed to reflect gameplay state changes.
- Scope excludes unrelated lobby/account/presence redesign and excludes introducing a deck/discard/resource-meter system beyond what the spec already defines.
- Keep Orleans runtime concerns in contracts/grains and keep business validation/outcome shaping in the application layer.
- Reuse the current invalidation/presence/timeline infrastructure where possible instead of inventing a new refresh transport.
- Treat the current persisted Orleans session state as disposable for this rewrite unless migration becomes a hard requirement during implementation.

## Further Considerations

1. Decide whether gameplay event persistence should use new `GameMessageKind` values in the existing timeline table or a parallel event representation before implementation reaches the data layer.
2. Keep the first UI iteration intentionally plain; phase correctness and invalidation behavior are higher risk than visual polish for this rewrite.

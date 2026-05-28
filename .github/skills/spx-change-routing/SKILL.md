---
name: spx-change-routing
description: 'Route a change to the correct project before editing. Use when deciding whether behavior belongs in Spx.Account.Application, Spx.Game.Application, Spx.Web, Spx.Data, Spx.Grains, Spx.Silo, or Spx.AppHost. Trigger words: which project owns this, where should this change go, account vs web, game application vs data, grain vs adapter, routing a feature, ownership boundary.'
argument-hint: 'Describe the behavior, bug, or feature and the current entry point.'
---

# Spx Change Routing

Use this skill before changing code when the entry point is visible but the owning behavior is unclear.

## Ownership Map

- `src/Spx.Account.Application`: account use cases, account outcomes, validation and branching for login, register, confirm email, resend confirmation, forgot password, reset password.
- `src/Spx.Game.Application`: game use cases, validation, outcome shaping, request models, and pure helper logic.
- `src/Spx.Web`: Blazor UI, endpoint mapping, adapters, redirect/query behavior, and integration with account or game services.
- `src/Spx.Data`: EF Core model, migrations, and persistence adapters.
- `src/Spx.Grains`: Orleans grain implementations and Orleans-specific behavior.
- `src/Spx.Silo`: Orleans silo host and silo-side bootstrap.
- `src/Spx.AppHost`: Aspire orchestration for local development.

## Routing Procedure

1. Start from the named file, failing behavior, or visible endpoint.
2. Ask which layer decides the branch, validation rule, or state mutation.
3. If the current file mostly forwards a call, step one hop deeper to the service, handler, adapter, or persistence boundary that actually decides behavior.
4. Prefer changing the owning layer instead of patching the caller.
5. Choose validation based on the owning layer:
   - application layer: narrow unit test first
   - data layer: focused integration test
   - web adapter: adapter or endpoint integration test
   - AppHost or silo wiring: focused build or startup validation

## Common Routing Rules

- If the change is business validation or a user-facing outcome, it usually belongs in `Spx.Account.Application` or `Spx.Game.Application`.
- If the change is HTTP binding, redirects, route/query handling, or Blazor wiring, it belongs in `Spx.Web`.
- If the change is query shape, persistence state, migrations, or EF configuration, it belongs in `Spx.Data`.
- If the change depends on Orleans lifecycle or observer behavior, it belongs in `Spx.Grains` or `Spx.Silo`.
- If the change is only about local orchestration, dependency wiring, health checks, or startup ordering, it belongs in `Spx.AppHost`.

## Validation Shortcuts

- Account behavior: `dotnet test tests/Spx.Account.Application.Tests/Spx.Account.Application.Tests.csproj`
- Game application behavior: `dotnet test tests/Spx.Game.Application.Tests/Spx.Game.Application.Tests.csproj`
- EF-backed game persistence: `dotnet test tests/Spx.Game.Application.IntegrationTests/Spx.Game.Application.IntegrationTests.csproj`
- Web adapters and endpoints: `dotnet test tests/Spx.Web.Tests/Spx.Web.Tests.csproj`
- Grains: `dotnet test tests/Spx.Grains.Tests/Spx.Grains.Tests.csproj`

## Type Placement Rules

`Spx.Game.Domain` types flow through all layers. Never mirror a Domain type 1:1 in Application or Contracts — that is always a smell.

**Use a Domain type directly when:**
- Passing a command or query from Application through to a grain (Web adapter passes it straight through; no wrapper needed)
- Returning a view model from a grain that Application or Web displays unchanged

**Define an Application type when:**
- The input carries raw user input that needs validation or mapping before it becomes a Domain command (`CreateGameRequest`, `JoinGameRequest`) — it is not yet the same thing
- The read model aggregates data from multiple persistence sources rather than being derived directly from one Domain type (`GameLobbyView`, `GameSummaryView`)
- The outcome hierarchy must live in Application because Application cannot reference Contracts without pulling in the Orleans SDK (`GameSessionCommandOutcome` vs `GameSessionGrainCommandResult`)

**Define a Contracts surrogate when:**
- A Domain type must cross an Orleans grain boundary: add a `[GenerateSerializer]` surrogate struct + `[RegisterConverter]` converter in `GameSessionDomainSurrogates.cs`
- The type is a grain-specific result carrying grain-infrastructure fields (`PendingGameplayEventBatchId`) that Application does not know about

**Define a Grain state type when:**
- Persisting game session state inside a grain: grain state must be mutable classes with `[GenerateSerializer]`+`[Id]` attributes for Orleans storage compatibility and stable schema evolution — Domain records cannot substitute here

## References

- `README.md`
- `TESTING.md`
- `src/Spx.AppHost/Program.cs`
- `src/Spx.Account.Application/AccountOutcomes.cs`
- `src/Spx.Game.Application/GameModels.cs`
- `src/Spx.Game.Application/GameTimelineModels.cs`

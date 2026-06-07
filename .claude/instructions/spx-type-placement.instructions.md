---
name: spx-type-placement
description: 'Decide whether to define a type in Domain, Application, or Contracts.'
applyTo: '{src/Spx.*.Domain/**,src/Spx.*.Application/**,src/Spx.Contracts/**,src/Spx.Grains/**}'
---

# Spx Type Placement (Domain / Application / Contracts)

`Spx.Game.Domain` types flow through all layers. **Never mirror a Domain type 1:1 in Application or Contracts** — that is always a smell.

## Use a Domain type directly when

- Passing a command or query from Application through to a grain (Web adapter passes it straight through; no wrapper needed).
- Returning a view model from a grain that Application or Web displays unchanged.

## Define an Application type when

- The input carries raw user input that needs validation or mapping before it becomes a Domain command (`CreateGameRequest`, `JoinGameRequest`) — it is not yet the same thing.
- The read model aggregates data from multiple persistence sources rather than being derived directly from one Domain type (`GameLobbyView`, `GameSummaryView`).
- The outcome hierarchy must live in Application because Application cannot reference Contracts without pulling in the Orleans SDK (`GameSessionCommandOutcome` vs `GameSessionGrainCommandResult`).

## Define a Contracts surrogate when

- A Domain type must cross an Orleans grain boundary: add a `[GenerateSerializer]` surrogate struct + `[RegisterConverter]` converter in `GameSessionDomainSurrogates.cs`.
- The type is a grain-specific result carrying grain-infrastructure fields (`PendingGameplayEventBatchId`) that Application does not know about.

## Domain view types vs view query helpers

- **View records** (`NexusGameView`, `NexusSystemView`, etc.) in `NexusViews.cs`: immutable `[GenerateSerializer]` + `[Immutable]` records that carry data across grain boundaries. Add instance helpers (simple projections of the type's own fields) directly on the record.
- **View query helpers** in `NexusViewQueries.cs`: static class with pure functions that require cross-type reasoning (e.g., `GetValidMoveDestinations(NexusGameView, Guid, HexCoord)`).
- Never add cross-type view logic to a view record's instance methods — put it in `NexusViewQueries`.

## Define a Grain state type when

- Persisting game session state inside a grain: grain state must be mutable classes with `[GenerateSerializer]` + `[Id]` attributes for Orleans storage compatibility and stable schema evolution — Domain records cannot substitute here.

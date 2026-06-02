---
name: spx-type-placement
description: 'Decide whether to define a type in Domain, Application, or Contracts. Use when deciding where a type belongs, whether to create a surrogate, or how to flow a domain type through layers. Trigger words: where does this type go, Domain vs Application, should I define a surrogate, type placement, Contracts type, cross-layer type, mirrored type.'
argument-hint: 'Describe the type, its purpose, and which boundary it crosses.'
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

## Define a Grain state type when

- Persisting game session state inside a grain: grain state must be mutable classes with `[GenerateSerializer]` + `[Id]` attributes for Orleans storage compatibility and stable schema evolution — Domain records cannot substitute here.

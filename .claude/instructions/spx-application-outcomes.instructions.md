---
description: 'Use when editing account or game application handlers, defining business-facing results, choosing outcome shapes, or deciding between explicit outcomes and exceptions.'
name: 'Spx Application Outcomes'
applyTo: '{src/Spx.Account.Application/**,src/Spx.Game.Application/**}'
---

# Spx Application Outcomes

## Core Policy

- Represent expected business and validation results with outcome types.
- Prefer named outcome records at handler and adapter boundaries.
- Use exceptions only for configuration failures, infrastructure faults, and violated invariants.

## Existing Patterns

- Account flows use status-based outcome records such as `RegisterOutcome`, `LoginOutcome`, `ResetPasswordOutcome`, and `ConfirmEmailOutcome`.
- Game application uses split success and failure hierarchies such as `GameCommandOutcome` with `GameCommandSucceeded` and `GameCommandFailed`.
- Game messaging uses `GameMessageCommandOutcome` with `GameMessageCommandSucceeded` and `GameMessageCommandFailed`.

## Design Procedure

1. Identify whether the caller needs a business-facing success or failure result.
2. If the outcome carries different data on success and failure, prefer separate success and failure records.
3. If a small status enum with optional payload is simpler and stable, use a status-based record.
4. Keep validation and business failures in the outcome model.
5. Reserve exceptions for non-business failure modes.

## Naming Rules

- Use the `Outcome` suffix for business-facing root types.
- Keep names explicit: `RegisterOutcome`, `GameCommandOutcome`, `GameMessageCommandOutcome`, `SubmitGameMoveOutcome`.
- Avoid bool-plus-nullable payload return shapes when an outcome model is clearer.

## Handler Structure

- Handlers are `internal sealed class` with a primary constructor for dependency injection.
- Any handler that logs **must** be `internal sealed partial class` to enable the `[LoggerMessage]` source generator.
- Use `[LoggerMessage]` static partial methods for all structured logging. Direct `ILogger.Log*` calls violate CA1848, which is a build error under `AnalysisMode=Recommended`. There is no `// warning suppress` escape hatch — use the pattern.
- Adapters in `Spx.Data`, `Spx.Web`, and `Spx.Grains` follow the same `internal sealed partial class` convention when they log.

## References

- `src/Spx.Account.Application/AccountOutcomes.cs`
- `src/Spx.Game.Application/GameModels.cs`
- `src/Spx.Game.Application/GameTimelineModels.cs`

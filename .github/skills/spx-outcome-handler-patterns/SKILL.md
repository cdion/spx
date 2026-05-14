---
name: spx-outcome-handler-patterns
description: 'Apply the repo outcome-based handler pattern at application and adapter boundaries. Use when designing or changing account outcomes, game command outcomes, message command outcomes, handler return types, or deciding exception vs outcome. Trigger words: outcome type, handler result, account outcome, game command outcome, game message outcome, exception vs outcome, validation result.'
argument-hint: 'Describe the handler or boundary you are changing.'
---

# Spx Outcome And Handler Patterns

Use this skill when changing a handler, service, or adapter boundary that returns business-facing results.

## Repo Policy

- Expected business and validation outcomes should use outcome types.
- Prefer named outcome records at handler and adapter boundaries.
- Use exceptions for configuration failures, infrastructure faults, and violated invariants only.

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

## References

- `src/Spx.Account/AccountOutcomes.cs`
- `src/Spx.Game.Application/GameModels.cs`
- `src/Spx.Game.Application/GameTimelineModels.cs`

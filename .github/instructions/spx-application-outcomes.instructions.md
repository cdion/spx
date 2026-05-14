---
description: 'Use when editing account or game application handlers, defining business-facing results, choosing outcome shapes, or deciding between explicit outcomes and exceptions.'
name: 'Spx Application Outcomes'
applyTo:
  - 'src/Spx.Account/**'
  - 'src/Spx.Game.Application/**'
---

# Spx Application Outcomes

- Represent expected business and validation results with outcome types.
- Prefer named outcome records at handler and adapter boundaries.
- Use exceptions only for configuration failures, infrastructure faults, and violated invariants.

## Current Repo Patterns

- Account features use status-based outcomes such as `RegisterOutcome`, `LoginOutcome`, `ResetPasswordOutcome`, and `ConfirmEmailOutcome`.
- Game flows use explicit success and failure records such as `GameCommandSucceeded` and `GameCommandFailed`.
- Message flows use `GameMessageCommandOutcome` with success and failure records.

## Guidance

- Use the `Outcome` suffix for root business-facing types.
- Prefer clear outcome models over bool-plus-nullable payload returns.
- Keep validation and domain failures inside the outcome model.

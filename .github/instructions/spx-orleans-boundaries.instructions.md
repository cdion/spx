---
description: 'Use when editing Orleans grains, contracts, silo code, grain observers, or runtime-specific boundaries in this repo.'
name: 'Spx Orleans Boundaries'
applyTo:
  - 'src/Spx.Grains/**'
  - 'src/Spx.Contracts/**'
  - 'src/Spx.Silo/**'
---

# Spx Orleans Boundaries

- Keep Orleans runtime concerns in grains, contracts, and silo code.
- Keep business validation and outcome shaping in the application layer when Orleans is only the transport or runtime mechanism.
- Use focused grain tests for Orleans-specific behavior instead of broad host-level tests.

## Repo Notes

- Contracts should define grain-facing interfaces and shared models.
- Grain implementations should stay small and centered on Orleans behavior or state transitions.
- Do not rely on PostgreSQL grain storage through the Aspire Orleans hosting integration in this repo.

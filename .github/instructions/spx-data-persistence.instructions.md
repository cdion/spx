---
description: 'Use when editing EF Core entities, DbContext, migrations, persistence adapters, or EF-backed integration tests in this repo.'
name: 'Spx Data And Persistence'
applyTo:
  - 'src/Spx.Data/**'
  - 'tests/Spx.Game.Application.IntegrationTests/**'
---

# Spx Data And Persistence

- Keep EF-specific behavior, schema changes, and persistence adapters in `src/Spx.Data`.
- Validate EF query shape and persistence behavior with focused integration tests instead of broad host-level tests.
- Keep application validation and branching in `Spx.Account` or `Spx.Game.Application`, not in data adapters.

## EF Workflow

- Use the local tool manifest, not a global `dotnet-ef` install.
- Typical commands:
  - `dotnet tool restore`
  - `dotnet tool run dotnet-ef -- migrations add NameOfMigration --project src/Spx.Data/Spx.Data.csproj --startup-project src/Spx.Web/Spx.Web.csproj`
  - `dotnet tool run dotnet-ef -- database update --project src/Spx.Data/Spx.Data.csproj --startup-project src/Spx.Web/Spx.Web.csproj`

## Validation

- Prefer `tests/Spx.Game.Application.IntegrationTests` for EF-backed game scenarios.
- Do not move pure handler coverage into EF integration tests.

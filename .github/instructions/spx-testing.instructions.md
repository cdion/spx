---
description: 'Use when writing or editing tests, deciding unit vs integration, choosing the correct test project, or selecting the narrowest validation command for this repo.'
name: 'Spx Testing And Validation'
applyTo:
  - 'tests/**/*.cs'
  - 'tests/**/*.csproj'
---

# Spx Testing And Validation

- Default to unit tests for behavior owned by application code.
- Use integration tests only when the risk is owned by EF Core, ASP.NET Identity, endpoint binding, redirects, or Orleans runtime behavior.
- Keep integration tests narrow and seam-focused.

## Test Project Map

- `tests/Spx.Account.Tests`: pure account handlers.
- `tests/Spx.Game.Application.Tests`: pure game application handlers and helpers.
- `tests/Spx.Game.Application.IntegrationTests`: EF-backed game persistence and query behavior.
- `tests/Spx.Web.Tests`: endpoint mapping, redirect/query behavior, Identity-backed adapters, and other web-owned seams.
- `tests/Spx.Grains.Tests`: focused grain behavior.

## Validation Rules

- After a substantive edit, run the narrowest executable validation that can falsify the change.
- Prefer the touched test project over a repo-wide test run.
- Use `dotnet tool restore` before coverage or EF tool workflows.

## Common Commands

- `dotnet test tests/Spx.Account.Tests/Spx.Account.Tests.csproj`
- `dotnet test tests/Spx.Game.Application.Tests/Spx.Game.Application.Tests.csproj`
- `dotnet test tests/Spx.Game.Application.IntegrationTests/Spx.Game.Application.IntegrationTests.csproj`
- `dotnet test tests/Spx.Web.Tests/Spx.Web.Tests.csproj`
- `dotnet test tests/Spx.Grains.Tests/Spx.Grains.Tests.csproj`

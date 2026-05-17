---
name: spx-test-placement-validation
description: 'Place tests in the correct project and choose the narrowest validation step. Use when deciding unit vs integration, which test project owns behavior, what validation command to run, or how to cover a new branch. Trigger words: where should this test go, unit or integration, which test project, narrow validation, coverage gap, what command should I run.'
argument-hint: 'Describe the behavior under test and the files being changed.'
---

# Spx Test Placement And Validation

Use this skill when adding or updating tests, or when deciding how to validate a code change.

## Test Placement Rules

- `tests/Spx.Account.Tests`: pure account handler behavior.
- `tests/Spx.Game.Domain.Tests`: pure reusable game rule helpers.
- `tests/Spx.Game.Application.Tests`: pure game application handlers and helpers.
- `tests/Spx.Game.Application.IntegrationTests`: EF-backed persistence and query behavior.
- `tests/Spx.Web.Tests`: endpoint mapping, redirect/query behavior, Identity-backed adapters, and other web-owned integration seams.
- `tests/Spx.Grains.Tests`: focused Orleans grain behavior.

## Decision Procedure

1. If the behavior is decided in application code without a real framework dependency, add a unit test.
2. If the risk is owned by EF Core, ASP.NET Identity, endpoint binding, redirects, or Orleans runtime semantics, add a focused integration test.
3. Keep integration tests narrow. Do not move pure handler logic into integration tests just because the caller is a web endpoint.
4. After the first substantive edit, run the narrowest executable validation that can falsify the change.

## Preferred Validation Order

1. Targeted unit or integration test for the touched slice.
2. Focused build or compile check for the touched project if no narrow test exists.
3. Broad test runs only after the touched slice is green.

## Useful Commands

- `dotnet test tests/Spx.Account.Tests/Spx.Account.Tests.csproj`
- `dotnet test tests/Spx.Game.Domain.Tests/Spx.Game.Domain.Tests.csproj`
- `dotnet test tests/Spx.Game.Application.Tests/Spx.Game.Application.Tests.csproj`
- `dotnet test tests/Spx.Game.Application.IntegrationTests/Spx.Game.Application.IntegrationTests.csproj`
- `dotnet test tests/Spx.Web.Tests/Spx.Web.Tests.csproj`
- `dotnet test tests/Spx.Grains.Tests/Spx.Grains.Tests.csproj`
- `dotnet tool restore`

## Coverage Direction

Favor new unit tests in account and game application code before expanding integration coverage. Use integration tests as a thin proving layer for EF, Identity, endpoint binding, and Orleans-specific seams.

## References

- `TESTING.md`
- `README.md`

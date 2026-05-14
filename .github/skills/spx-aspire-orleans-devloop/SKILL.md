---
name: spx-aspire-orleans-devloop
description: 'Work on the local distributed development loop for Aspire, AppHost, Orleans, PostgreSQL, Redis, and Tailwind. Use when starting the app locally, changing AppHost wiring, debugging startup order, validating health checks, or checking dashboard-visible resources. Trigger words: AppHost, Aspire, local startup, silo startup, health check, dashboard, Redis, Postgres, orchestration, tailwind watch.'
argument-hint: 'Describe the local workflow, startup issue, or orchestration change.'
---

# Spx Aspire And Orleans Dev Loop

Use this skill when the task involves local orchestration, startup order, or validating distributed wiring.

## Local Entry Point

The normal local entry point is the AppHost:

- `dotnet run --project src/Spx.AppHost/Spx.AppHost.csproj`
- VS Code task: `dev: apphost + tailwind`

This should start the web app, silo, Redis, PostgreSQL, and the Aspire dashboard.

## Workflow

1. Prefer AppHost as the starting point for local validation.
2. When changing local orchestration, inspect `src/Spx.AppHost/Program.cs` first.
3. Keep dependency ordering explicit with `WaitFor(...)` when startup ordering matters.
4. Validate the narrowest affected resource or startup path before broader checks.
5. If a clean restart fails unexpectedly, check for stale AppHost or DCP processes still holding ports.

## Repo-Specific Notes

- Route validation for Blazor pages should use GET requests, not HEAD. A HEAD request may return 404 even when GET succeeds.
- Orleans hosting in this repo should not rely on PostgreSQL grain storage through the Aspire Orleans hosting integration.
- Tailwind source lives in `src/Spx.Web/Styles/app.css` and the generated output is `src/Spx.Web/wwwroot/app.css`.

## Useful Commands

- `dotnet run --project src/Spx.AppHost/Spx.AppHost.csproj`
- `dotnet build src/Spx.AppHost/Spx.AppHost.csproj`
- `./tools/tailwind/bin/tailwindcss-linux-x64 -i src/Spx.Web/Styles/app.css -o src/Spx.Web/wwwroot/app.css --minify`

## References

- `README.md`
- `src/Spx.AppHost/Program.cs`

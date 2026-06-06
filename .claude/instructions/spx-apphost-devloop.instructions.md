---
description: 'Use when editing AppHost or shared local orchestration code, wiring Aspire resources, adjusting startup dependencies, or validating the distributed dev loop.'
name: 'Spx AppHost Dev Loop'
applyTo: '{src/Spx.AppHost/**,src/Spx.ServiceDefaults/**}'
---

# Spx AppHost Dev Loop

## Local Entry Point

The normal local entry point is the AppHost:

- `dotnet run --project src/Spx.AppHost/Spx.AppHost.csproj`
- VS Code task: `dev: watch apphost + tailwind`

This starts the web app, silo, Redis, PostgreSQL, and the Aspire dashboard.

## Workflow

1. Prefer AppHost as the starting point for local validation.
2. When changing local orchestration, inspect `src/Spx.AppHost/Program.cs` first.
3. Keep dependency ordering explicit with `WaitFor(...)` when startup ordering matters.
4. Validate the narrowest affected resource or startup path before broader checks.
5. If a clean restart fails unexpectedly, check for stale AppHost or DCP processes still holding ports.

## Repo-Specific Notes

- Route validation for Blazor pages should use GET requests, not HEAD. A HEAD request may return 404 even when GET succeeds.
- Orleans hosting should not rely on PostgreSQL grain storage through the Aspire Orleans hosting integration.
- Tailwind source: `src/Spx.Web.Components/Styles/app.css` → output: `src/Spx.Web.Components/wwwroot/app.css`

## Common Commands

- `dotnet run --project src/Spx.AppHost/Spx.AppHost.csproj`
- `dotnet build src/Spx.AppHost/Spx.AppHost.csproj`
- `./tools/tailwind/bin/tailwindcss-linux-x64 -i src/Spx.Web.Components/Styles/app.css -o src/Spx.Web.Components/wwwroot/app.css --minify`

## References

- `README.md`
- `src/Spx.AppHost/Program.cs`

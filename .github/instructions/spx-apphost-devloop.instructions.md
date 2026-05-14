---
description: 'Use when editing AppHost or shared local orchestration code, wiring Aspire resources, adjusting startup dependencies, or validating the distributed dev loop.'
name: 'Spx AppHost Dev Loop'
applyTo:
  - 'src/Spx.AppHost/**'
  - 'src/Spx.ServiceDefaults/**'
---

# Spx AppHost Dev Loop

- The normal local entry point is `src/Spx.AppHost`.
- Prefer validating orchestration changes through AppHost rather than running services in isolation first.
- Keep startup ordering explicit with `WaitFor(...)` when a dependency must be ready.

## Repo Notes

- The main VS Code development task is `dev: apphost + tailwind`.
- Blazor route validation should use GET requests rather than HEAD requests.
- Clean restarts may require stopping stale AppHost or DCP processes that still hold ports.

## Common Commands

- `dotnet build src/Spx.AppHost/Spx.AppHost.csproj`
- `dotnet run --project src/Spx.AppHost/Spx.AppHost.csproj`
- `./tools/tailwind/bin/tailwindcss-linux-x64 -i src/Spx.Web/Styles/app.css -o src/Spx.Web/wwwroot/app.css --minify`

# Spx Orleans Starter

This workspace contains a minimal Microsoft Orleans starter built with:

- .NET 10 SDK
- ASP.NET Core minimal hosting
- .NET Aspire AppHost orchestration for local development
- Aspire ServiceDefaults for health checks, telemetry, and service discovery
- Blazor Web App for a minimal Orleans client UI
- Tailwind CSS via the standalone Linux binary

## Projects

- `src/Spx.Contracts`: grain interfaces shared across the solution
- `src/Spx.Grains`: grain implementations
- `src/Spx.AppHost`: the single Aspire AppHost that orchestrates the local silo and web client
- `src/Spx.Silo`: ASP.NET Core host that runs the Orleans silo and exposes diagnostic HTTP endpoints
- `src/Spx.ServiceDefaults`: shared Aspire defaults for telemetry, health checks, and service discovery
- `src/Spx.Web`: Blazor Web App that connects to Orleans as a client and invokes the hello grain
- `tools/tailwind`: repo-local standalone Tailwind binary for CSS generation

This is the idiomatic Aspire layout for this repo shape:

- one AppHost project
- one ServiceDefaults project
- one hosted ASP.NET Core Orleans silo
- one hosted Blazor Orleans client
- separate contracts and grain implementation projects

## Run it

Preferred local dev run path:

```bash
dotnet run --project src/Spx.AppHost
```

For the Tailwind watcher plus AppHost workflow in VS Code, run the `dev: apphost + tailwind` task.

You can still run the silo or web project directly if you provide the Orleans Redis connection settings yourself, but the Aspire path is the intended local-dev workflow because it starts Redis and wires the silo and client automatically.

Then open the web endpoint shown by Aspire and submit a name from the Blazor page. The silo still exposes these useful paths:

- `/`
- `/hello/orleans`
- `/health`
- `/alive`

If the port changes on your machine, use the URL printed by the AppHost.

The Aspire dashboard URL is printed by the AppHost on startup.

## Tailwind workflow

- Tailwind source CSS lives in `src/Spx.Web/Styles/app.css`
- Generated output is written to `src/Spx.Web/wwwroot/app.css`
- `wwwroot/app.css` is ignored by git and regenerated on build
- `src/Spx.Web/Spx.Web.csproj` runs a one-shot Tailwind build before `Build` and `Publish`
- `.vscode/tasks.json` contains a compound task that runs the Tailwind watcher and AppHost together for development

## What you need next

For a real Orleans app, the next decisions are usually:

1. Define your grain interfaces in `Spx.Contracts`.
2. Implement grain behavior and state in `Spx.Grains`.
3. Move more UI-facing grain interactions behind application services in `Spx.Web`.
4. Replace the local Redis-backed development clustering setup with your production backing store or managed Redis configuration.
5. Add tests for grain behavior and UI or host endpoints.
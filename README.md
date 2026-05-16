# Spx

Spx is a .NET 10 multiplayer game lobby application built on Microsoft Orleans, ASP.NET Core Identity, PostgreSQL, Redis, Blazor, and .NET Aspire.

The current app is no longer a starter template. It includes account flows, invite-code based game creation and joining, lobby views, message persistence, and a split between application, data, web, and Orleans hosting layers.

## Stack

- .NET 10 SDK
- .NET Aspire AppHost for local orchestration
- Microsoft Orleans for distributed runtime and grain hosting
- Redis for Orleans clustering
- PostgreSQL for application data and Orleans storage schema
- ASP.NET Core Identity for authentication and confirmed-account flows
- Blazor Web App with interactive server components
- Tailwind CSS via the repo-local standalone binary
- Local .NET tool manifest for `dotnet-ef` and coverage reporting tools

## Solution Layout

- `src/Spx.Account`: account application handlers for login, registration, email confirmation, password reset, and resend confirmation
- `src/Spx.AppHost`: Aspire entry point that starts the web app, silo, Redis, PostgreSQL, and the Aspire dashboard for local development
- `src/Spx.Contracts`: Orleans grain interfaces and shared contracts
- `src/Spx.Data`: EF Core data model, PostgreSQL persistence, game persistence adapters, Identity entities, and migrations
- `src/Spx.Game.Application`: application-layer game use cases and view models
- `src/Spx.Grains`: Orleans grain implementations
- `src/Spx.Silo`: Orleans silo host plus silo-side storage bootstrap and diagnostics
- `src/Spx.ServiceDefaults`: shared Aspire defaults for telemetry, health checks, and service discovery
- `src/Spx.Web`: Blazor front end, account endpoints, and adapters that connect the web layer to account and game application services
- `tests/Spx.Account.Tests`: account-focused tests
- `tests/Spx.Game.Application.Tests`: games unit tests
- `tests/Spx.Game.Application.IntegrationTests`: EF-backed games integration tests
- `tests/Spx.Grains.IntegrationTests`: Orleans runtime integration tests
- `tests/Spx.Grains.Tests`: Orleans grain behavior tests
- `tests/Spx.Web.Tests`: web and adapter integration tests
- `tools/tailwind`: repo-local Tailwind CLI binary

## Prerequisites

- .NET 10 SDK
- A container runtime compatible with Aspire local development, such as Docker or Podman

Restore repo-local tools before using EF commands or coverage tasks:

```bash
dotnet tool restore
```

## Local Development

The intended local entry point is the Aspire AppHost:

```bash
dotnet run --project src/Spx.AppHost/Spx.AppHost.csproj
```

That starts:

- the `web` app
- the Orleans `silo`
- Redis for Orleans clustering
- PostgreSQL with `appdb` and `orleansdb`
- the Aspire dashboard

Use the URLs printed by the AppHost or shown in the Aspire dashboard. The dashboard is the easiest place to inspect logs, health, and resolved endpoints.

In VS Code, the main development task is:

- `dev: apphost + tailwind`

That runs the Tailwind watcher and the AppHost under `dotnet watch` together.

For normal UI and Razor iteration, prefer that task over the debug launch configuration. Keep the debug launch for cases where you actually need breakpoints or debugger state.

Useful single-purpose tasks are also checked in:

- `build: web`
- `build: silo`
- `build: apphost`
- `tailwind: build css`
- `tailwind: watch css`
- `apphost: watch`
- `apphost: run`

## Application Behavior

The authenticated landing page is the game lobby. From there, users can:

- create a new game
- join an existing game with an invite code
- reopen active games
- view ended games as lightweight history

The current web layer is centered on account management and lobby-first multiplayer flows rather than the earlier hello-grain demo path.

## Authentication And Email

The web app uses ASP.NET Core Identity with confirmed accounts enabled.

In development, confirmation and password reset emails are not sent externally. Instead, the web app logs the full confirmation and reset URLs in the `web` resource logs.

Typical local auth test flow:

1. Start the AppHost.
2. Open the Aspire dashboard.
3. Open the `web` resource logs.
4. Register, resend confirmation, or request a password reset.
5. Copy the logged link into the browser.

Outside development, the app uses a Resend-backed email sender. Production configuration must provide `Resend:ApiKey` and `Resend:FromEmail`.

## Data And Storage

The AppHost provisions two PostgreSQL databases:

- `appdb` for ASP.NET Core Identity and EF-backed game data
- `orleansdb` for Orleans storage schema bootstrap

On web startup, EF Core migrations are applied automatically for `appdb`.

On silo startup, the Orleans PostgreSQL SQL assets are bootstrapped automatically into `orleansdb` if the schema is missing.

Redis remains the clustering backend for Orleans during local development.

## Entity Framework Workflow

Use the checked-in local tool manifest instead of a globally installed `dotnet-ef`:

```bash
dotnet tool restore
dotnet tool run dotnet-ef -- --help
```

Typical commands from the repo root:

```bash
dotnet tool run dotnet-ef -- migrations add NameOfMigration --project src/Spx.Data/Spx.Data.csproj --startup-project src/Spx.Web/Spx.Web.csproj
dotnet tool run dotnet-ef -- database update --project src/Spx.Data/Spx.Data.csproj --startup-project src/Spx.Web/Spx.Web.csproj
```

Current migrations live under `src/Spx.Data/Migrations`.

## Tailwind Workflow

- Source CSS: `src/Spx.Web/Styles/app.css`
- Generated CSS: `src/Spx.Web/wwwroot/app.css`
- The web project runs a one-shot Tailwind build before `Build` and `Publish`
- The checked-in VS Code task can run the watcher alongside the AppHost during development

You can also build the CSS manually:

```bash
./tools/tailwind/bin/tailwindcss-linux-x64 -i src/Spx.Web/Styles/app.css -o src/Spx.Web/wwwroot/app.css --minify
```

## Testing

The repo testing strategy is documented in `TESTING.md`.

Common commands from the repo root:

```bash
dotnet test tests/Spx.Account.Tests/Spx.Account.Tests.csproj
dotnet test tests/Spx.Game.Application.Tests/Spx.Game.Application.Tests.csproj
dotnet test tests/Spx.Game.Application.IntegrationTests/Spx.Game.Application.IntegrationTests.csproj
dotnet test tests/Spx.Grains.Tests/Spx.Grains.Tests.csproj
dotnet test tests/Spx.Grains.IntegrationTests/Spx.Grains.IntegrationTests.csproj
dotnet test tests/Spx.Web.Tests/Spx.Web.Tests.csproj
```

The VS Code task file also includes:

- `tests: run`
- `tests: coverage html`

The coverage task writes HTML output under `coverage/html`.
# Spx

Spx is a .NET 10 multiplayer game lobby application built on Microsoft Orleans, ASP.NET Core Identity, PostgreSQL, Redis, Blazor, .NET Aspire, and Tailwind CSS.

From the authenticated landing page (the game lobby), users can create a new game, join an existing game with an invite code, reopen active games, and view ended games as lightweight history. The app covers account flows, invite-code-based game creation and joining, lobby views, message persistence, and a split between application, data, web, and Orleans hosting layers.

## Prerequisites

- .NET 10 SDK
- A container runtime compatible with Aspire local development, such as Docker or Podman

Restore repo-local tools before using EF commands or coverage tasks:

```bash
dotnet tool restore
```

## Solution Layout

- `src/Spx.Account.Application`: account application handlers for login, registration, email confirmation, password reset, and resend confirmation
- `src/Spx.AppHost`: Aspire entry point that starts the web app, silo, Redis, PostgreSQL, and the Aspire dashboard for local development
- `src/Spx.Contracts`: Orleans grain interfaces and shared contracts
- `src/Spx.Data`: EF Core data model, PostgreSQL persistence, game persistence adapters, Identity entities, and migrations
- `src/Spx.Game.Application`: application-layer game use cases and view models
- `src/Spx.Grains`: Orleans grain implementations
- `src/Spx.Silo`: Orleans silo host plus silo-side storage bootstrap and diagnostics
- `src/Spx.ServiceDefaults`: shared Aspire defaults for telemetry, health checks, and service discovery
- `src/Spx.Web`: Blazor front end, account endpoints, and adapters that connect the web layer to account and game application services
- `tests/Spx.Account.Application.Tests`: account-focused tests
- `tests/Spx.Game.Application.Tests`: games unit tests
- `tests/Spx.Game.Application.IntegrationTests`: EF-backed games integration tests
- `tests/Spx.Grains.IntegrationTests`: Orleans runtime integration tests
- `tests/Spx.Grains.Tests`: Orleans grain behavior tests
- `tests/Spx.Web.Tests`: web and adapter integration tests
- `tools/tailwind`: repo-local Tailwind CLI binary

## Local Development

The intended local entry point:

```bash
just dev
```

This starts the Tailwind watcher and the AppHost under `dotnet watch` together.

Alternatively, run just the AppHost to inspect logs, health, and endpoints via the Aspire dashboard:

```bash
just run
```

That starts:
- the `web` app
- the Orleans `silo`
- Redis for Orleans clustering
- PostgreSQL with `appdb` and `orleansdb`
- the Aspire dashboard

| Command | What it does |
|---|---|
| `just dev` | AppHost + Tailwind in parallel |
| `just dev-run` | Start the AppHost |
| `just dev-watch` | AppHost under `dotnet watch` (background) |
| `just dev-tailwind` | Build Tailwind CSS once |
| `just dev-tailwind-watch` | Watch Tailwind source files |
| `just dev-watch-playground` | Playground under `dotnet watch` (background) |
| `just dev-build-project src/Spx.Web` | Build one project |

### Tailwind

- Source CSS: `src/Spx.Web.Components/Styles/app.css`
- Generated CSS: `src/Spx.Web.Components/wwwroot/app.css`
- The component library runs a one-shot Tailwind build before `Build` and `Publish`
- `just dev-tailwind-watch` runs the watcher alongside the AppHost via `just dev`

### Database Resets

| Command | What it does |
|---|---|
| `just infra-reset-appdb` | Clear all application tables in `appdb` while preserving `__EFMigrationsHistory`. Targets the running Aspire-managed Postgres container directly, so you do not need to chase the current mapped host port. |
| `just infra-reset-orleansdb` | Clear persisted Orleans grain state from `orleansdb` by talking directly to the running Aspire-managed Postgres container. Restart the AppHost afterward to clear any in-memory grain activations. |

## Data And Storage

The AppHost provisions two PostgreSQL databases:
- **`appdb`** — ASP.NET Core Identity and EF-backed game data. Migrations are applied automatically on web startup.
- **`orleansdb`** — Orleans storage schema. The PostgreSQL SQL assets are bootstrapped automatically on silo startup if the schema is missing.

Redis remains the clustering backend for Orleans during local development.

## Entity Framework Workflow

Use the checked-in local tool manifest instead of a globally installed `dotnet-ef`:

```bash
dotnet tool run dotnet-ef -- --help
```

Typical commands from the repo root:

```bash
dotnet tool run dotnet-ef -- migrations add NameOfMigration --project src/Spx.Data/Spx.Data.csproj --startup-project src/Spx.Web/Spx.Web.csproj
dotnet tool run dotnet-ef -- database update --project src/Spx.Data/Spx.Data.csproj --startup-project src/Spx.Web/Spx.Web.csproj
```

Current migrations live under `src/Spx.Data/Migrations`.

## Authentication And Email

The web app uses ASP.NET Core Identity with confirmed accounts enabled.

**In development**, confirmation and password reset emails are not sent externally. Instead, the web app logs the full confirmation and reset URLs in the `web` resource logs. Typical local auth test flow:

1. Start the AppHost.
2. Open the Aspire dashboard.
3. Open the `web` resource logs.
4. Register, resend confirmation, or request a password reset.
5. Copy the logged link into the browser.

**In production**, the app uses a Resend-backed email sender. Configuration must provide `Resend:ApiKey` and `Resend:FromEmail`. The `FromEmail` address must belong to a Resend-verified domain or subdomain such as `mail.mostlyhuman.ca`.

## Testing

The repo testing strategy is documented in `TESTING.md`.

```bash
just dev-test-project tests/Spx.Nexus.Domain.Tests
just dev-test-project tests/Spx.Web.Tests
just dev-test-all
```

Coverage:

```bash
just dev-coverage tests/Spx.Nexus.Domain.Tests
just dev-coverage-full       # clean → collect all → HTML report
```

The coverage report is written to `coverage/html/`.

## CI And Deployment

The repo uses `just` (https://github.com/casey/just) for all task orchestration.
Recipes are organized into topic files that `justfile` imports:

| File | Concern |
|---|---|
| `ci.just` | `ci-*` — restore, build, test, CI pipeline |
| `deploy.just` | `deploy-*` — container images, push, promote, migrate, restart |
| `infra.just` | `infra-*` — VM bootstrap, db reset, diagnostics |
| `dev.just` | `dev-*` — local build, test, coverage, tailwind, run, watch |

GitHub Actions should stay thin and call the same Just recipes you can run locally.

Common entry points from the repo root:

```bash
just ci                    # ci-restore → ci-build → ci-test
just deploy-build-images   # build container images
just deploy-publish        # build + push images
just deploy-promote-prod   # tag current build as prod
just deploy                # deploy-pull-images → deploy-migrate → deploy-restart-app
```

### CI Pipeline

On `main`, the CI workflow:
1. Runs `just ci` (restore → build → test).
2. Publishes immutable commit-derived tags to GHCR.
3. Promotes the same build to the stable `prod` tags.

Deploy is gated on CI success, refreshes the VM's GHCR login explicitly, then runs `just deploy` against whatever `prod` already points to. Deploy does not rebuild images or move tags.

The workflows authenticate to GHCR with repository secrets `GHCR_USERNAME` and `GHCR_TOKEN` rather than relying on the built-in `GITHUB_TOKEN`. That avoids package-linkage and workflow-access issues with pre-existing GHCR packages.

### Build Version

The canonical build version is shared between container image tags and the web UI. By default it is derived from the current Git commit in a SemVer-compatible format, and you can override it when needed:

```bash
SPX_VERSION=1.2.3-rc.1 just ci
IMAGE_TAG=1.2.3-rc.1 just deploy-publish deploy-promote-prod
```

### Required Secrets

| Secret | CI | Deploy | Purpose |
|---|---|---|---|
| `GHCR_USERNAME` | ✓ | ✓ | GHCR account name (e.g. `cdion`) |
| `GHCR_TOKEN` | ✓ | ✓ | Classic PAT with `write:packages` + `read:packages` (CI) or `read:packages` (deploy) |
| `VM` | | ✓ | SSH target `user@host` |
| `VM_SSH_KEY` | | ✓ | Private key for that host |
| `VM_KNOWN_HOSTS` | | ✓ | Pinned known-host entry for the VM |

### VM Bootstrap

`just infra-bootstrap` remains the one-time VM setup path. It handles GHCR login for the VM's root Podman context when `GHCR_USERNAME` and `GHCR_TOKEN` are present in the shell environment, keeping registry bootstrap and Quadlet file installation in the same path.

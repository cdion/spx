# Spx Agent Rules

## Context & File Discipline

- Avoid reading `bin/`, `obj/`, `wwwroot/lib/`, `deploy/`, `.github/`, `coverage/`, `artifacts/`, `.aider.*` unless asked.
- Use `path` to scope `find`/`grep`/`ls` by subdirectory. Use `offset`/`limit` on `read` for large files.
- Skip generated files: `*.g.cs`, `*.g.i.cs`, `GlobalUsings.g.cs`, `AssemblyInfo.cs`.
- After substantive edits, run the **narrowest validation** that can falsify the change (use `/skill:spx-test-placement-validation` for guidance).

## Architecture & Ownership

- `Spx.Game.Domain` — pure game rules. Flows through all layers; never mirror it 1:1 elsewhere.
- `Spx.Nexus.Domain` — nexus/crafting pure domain logic.
- `Spx.Account.Application` — account use cases & outcomes.
- `Spx.Game.Application` — game use cases & outcomes.
- `Spx.Contracts` — grain-facing interfaces, shared DTOs.
- `Spx.Grains` — Orleans grain implementations.
- `Spx.Data` — EF Core model, migrations, persistence adapters.
- `Spx.Silo` — Orleans silo host & bootstrap.
- `Spx.Web` — Blazor UI, endpoints, adapters, redirect/query.
- `Spx.Web.Components` — shared Blazor components (no DI, no I/O).
- `Spx.Web.Playground` — story/component playground.
- `Spx.AppHost` — Aspire orchestration (local dev entry point).
- `Spx.ServiceDefaults` — shared service configuration.

## Handler Patterns

- Use **named outcome records** (`*Outcome` suffix) for expected business results — prefer split success/failure records when different data on each path.
- Use exceptions only for configuration failures, infrastructure faults, and violated invariants.
- Handlers are `internal sealed class` with primary constructor DI. Handlers that log **must** be `internal sealed partial class` with `[LoggerMessage]` static partial methods — direct `ILogger.Log*` calls violate CA1848 (build error). No escape hatch.
- This applies to all adapters (Spx.Data, Spx.Web, Spx.Grains) when they log.

| Area | Pattern | Example |
|------|---------|---------|
| Account | Status-based outcome records | `RegisterOutcome`, `LoginOutcome` |
| Game commands | Split success/failure | `GameCommandOutcome` → `GameCommandSucceeded` / `GameCommandFailed` |
| Game messages | Split success/failure | `GameMessageCommandOutcome` → `GameMessageCommandSucceeded` / `GameMessageCommandFailed` |

## Testing

- Default to **unit tests** for application code. Integration tests for EF Core, ASP.NET Identity, endpoint binding, redirects, Orleans runtime.
- After a substantive edit, run the **narrowest test project** that can falsify the change.

| Project | Scope |
|---------|-------|
| `tests/Spx.Account.Application.Tests` | Pure account handlers |
| `tests/Spx.Nexus.Domain.Tests` | Pure domain, map, engine |
| `tests/Spx.Nexus.Simulation.Tests` | Tactical simulation & balance |
| `tests/Spx.Game.Application.Tests` | Pure game handlers |
| `tests/Spx.Game.Application.IntegrationTests` | EF-backed persistence & queries |
| `tests/Spx.Web.Tests` | Endpoint mapping, Identity adapters, bUnit |
| `tests/Spx.Grains.Tests` | Focused grain behavior |

- `NSubstitute 5.3.0` for lightweight mocking. Add `using NSubstitute;` when using `Substitute` or `Arg`.
- `AnalysisMode=Recommended` + `EnforceCodeStyleInBuild=true` — CA/IDE violations are **build errors**.
- Pre-commit hook runs `dotnet build -warnaserror` across solution.
- CSharpier: `dotnet csharpier format <path>`.

## Common Commands

- Build: `dotnet build src/<Project>/<Project>.csproj`
- Run AppHost: `dotnet run --project src/Spx.AppHost/Spx.AppHost.csproj`
- Tailwind: `./tools/tailwind/bin/tailwindcss-linux-x64 -i src/Spx.Web.Components/Styles/app.css -o src/Spx.Web.Components/wwwroot/app.css --minify`
- EF migrations: `dotnet tool restore && dotnet tool run dotnet-ef -- migrations add <Name> --project src/Spx.Data/Spx.Data.csproj --startup-project src/Spx.Web/Spx.Web.csproj`
- CSharpier: `dotnet csharpier format <path>`
- Test: `dotnet test tests/<TestProject>/<TestProject>.csproj`

## Key Files

- `Directory.Build.props` — shared MSBuild (AnalysisMode, EnforceCodeStyleInBuild)
- `Spx.slnx` — solution file
- `justfile` — automation
- `NEXUS-PROTOCOL-SPEC.md` — protocol spec (read only if asked)
- `TESTING.md` — testing strategy (refer when adding tests)

## Available Skills

Use `/skill:name` for detailed guidance on specific topics:

| Skill | When to use |
|-------|-------------|
| `spx-change-routing` | Unsure which project owns a behavior |
| `spx-outcome-handler-patterns` | Designing a new outcome type or handler boundary |
| `spx-aspire-orleans-devloop` | Working on AppHost, startup, orchestration |
| `spx-orleans-conventions` | Serialization, surrogates, observers, grain state |
| `spx-type-placement` | Whether a type goes in Domain, Application, or Contracts |
| `spx-component-testing` | bUnit data-testid patterns, TestIds classes |
| `spx-blazor-ui-patterns` | Component/page state extraction decision table |
| `spx-css-tailwind` | Color palette, dynamic classes, @apply, arbitrary values |
| `spx-test-placement-validation` | Choosing the right test project and validation step |

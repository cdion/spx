@.claude/SYSTEM.md
@.claude/AGENTS.md

## Instructions

Auto-load based on file paths. No manual invocation needed.

| File | Applies To | When |
|------|-----------|------|
| [spx-apphost-devloop](.claude/instructions/spx-apphost-devloop.instructions.md) | `src/Spx.AppHost/**`, `src/Spx.ServiceDefaults/**` | AppHost/Aspire orchestration, startup ordering |
| [spx-application-outcomes](.claude/instructions/spx-application-outcomes.instructions.md) | `src/Spx.Account.Application/**`, `src/Spx.Game.Application/**` | Handler patterns, outcome shapes, `[LoggerMessage]` |
| [spx-change-routing](.claude/instructions/spx-change-routing.instructions.md) | `src/**`, `tests/**` | Route a change to the correct project |
| [spx-data-persistence](.claude/instructions/spx-data-persistence.instructions.md) | `src/Spx.Data/**`, `tests/Spx.Game.Application.IntegrationTests/**` | EF Core migrations, persistence adapters |
| [spx-orleans-boundaries](.claude/instructions/spx-orleans-boundaries.instructions.md) | `src/Spx.Grains/**`, `src/Spx.Contracts/**`, `src/Spx.Silo/**` | Grain serialization, surrogates, observer lifecycle |
| [spx-testing](.claude/instructions/spx-testing.instructions.md) | `tests/**` | Unit vs integration decisions, test project map, bUnit/Playwright, `data-testid` |
| [spx-type-placement](.claude/instructions/spx-type-placement.instructions.md) | `src/Spx.*.Domain/**`, `src/Spx.*.Application/**`, `src/Spx.Contracts/**`, `src/Spx.Grains/**` | Decide whether to define a type in Domain, Application, or Contracts |
| [spx-web-css](.claude/instructions/spx-web-css.instructions.md) | `src/Spx.Web/**/*.razor`, `src/Spx.Web/**/*.css`, `src/Spx.Web.Components/**` | Blazor components, Tailwind v4, color palette, layout patterns |
| [spx-blazor-ui-patterns](.claude/instructions/spx-blazor-ui-patterns.instructions.md) | `src/Spx.Web/**/*.razor`, `src/Spx.Web.Components/**/*.razor` | Component/page state patterns, coordinator pattern, static reducers |
| [spx-verification](.claude/instructions/spx-verification.instructions.md) | `**` | Post-edit quality gates: build → test → format |

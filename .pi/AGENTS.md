# Spx Agent Rules

## Architecture & Ownership

| Project | Path | Role | Namespace |
|---------|------|------|-----------|
| Spx.Nexus.Domain | `src/Spx.Nexus.Domain` | Pure nexus/crafting domain rules | `Spx.Nexus.Domain` |
| Spx.Nexus.Simulation | `src/Spx.Nexus.Simulation` | Tactical simulation & balance | `Spx.Nexus.Simulation` |
| Spx.Account.Application | `src/Spx.Account.Application` | Account use cases & outcomes | `Spx.Account.Application` |
| Spx.Game.Application | `src/Spx.Game.Application` | Game use cases & outcomes (commands + messages) | `Spx.Game.Application` |
| Spx.Contracts | `src/Spx.Contracts` | Grain interfaces (`I*Grain`), observers (`IGrainObserver`), shared DTOs | `Spx.Contracts` |
| Spx.Grains | `src/Spx.Grains` | Orleans grain implementations | `Spx.Grains` |
| Spx.Data | `src/Spx.Data` | EF Core model, migrations, persistence adapters | `Spx.Data` |
| Spx.Silo | `src/Spx.Silo` | Orleans silo host & bootstrap | `Spx.Silo` |
| Spx.Web | `src/Spx.Web` | Blazor UI pages, endpoints, adapters, circuits | `Spx.Web` |
| Spx.Web.Components | `src/Spx.Web.Components` | Shared Blazor components (no DI, no I/O) | `Spx.Web` (RootNamespace override) |
| Spx.Web.Playground | `src/Spx.Web.Playground` | Story/component playground | `Spx.Web.Playground` |
| Spx.AppHost | `src/Spx.AppHost` | Aspire orchestration (local dev entry point) | `Spx.AppHost` |
| Spx.ServiceDefaults | `src/Spx.ServiceDefaults` | Shared service configuration | `Spx.ServiceDefaults` |

## Dependency Flow

```
Spx.Nexus.Domain  (pure, no deps)
  └── Spx.Nexus.Simulation
  └── Spx.Contracts (references Nexus.Domain for grain signatures)
        └── Spx.Grains (implements grain interfaces)
Spx.Account.Application (account outcomes + handlers)
Spx.Game.Application (game outcomes + handlers, persistence abstractions)
  └── Spx.Data (EF Core adapters implementing IGamePersistence etc.)
  └── Spx.Grains  (game grains like GamePresenceGrain, LobbyInvalidationGrain)
        └── Spx.Silo (hosts grains)
  └── Spx.Web.Components (shared UI, references Game.Application + Nexus.Domain)
        └── Spx.Web (pages, endpoints, adapters references everything)
```

## Namespace Quirks

- `Spx.Web.Components` has `<RootNamespace>Spx.Web</RootNamespace>` — components use `using Spx.Web;`.
- `Spx.Contracts` csproj is `Spx.Game.Contract.csproj` but namespace is `Spx.Contracts`.
- `Spx.Grains` csproj is `Spx.Game.Grain.csproj` but namespace is `Spx.Grains`.
- All other projects use their directory name as the namespace.

## Quick Search Anchors

### Orleans Grains (Contracts → Grains)

| Grain Interface | Location | Grain Impl | Grain State |
|----------------|----------|------------|-------------|
| `IGamePresenceGrain` | Contracts | `GamePresenceGrain` | (stateless) |
| `ILobbyInvalidationGrain` | Contracts | `LobbyInvalidationGrain` | (stateless) |
| `ILobbyInvalidationObserver` (IGrainObserver) | Contracts | N/A — Blazor circuit subscribes | N/A |
| `INexusSessionGrain` | Contracts | `NexusSessionGrain` | `NexusSessionGrainState` holding `NexusState` |

### Application Handlers (Spx.Game.Application)

| Feature | Handler | Outcome | Key Types |
|---------|---------|---------|-----------|
| CreateGame | `CreateGameHandler` | `GameCommandSucceeded/Failed` | `CreateGameRequest` |
| JoinGame | `JoinGameHandler` | `GameCommandSucceeded/Failed` | `JoinGameRequest` |
| LeaveGame | `LeaveGameHandler` | `GameCommandSucceeded/Failed` | — |
| SendPublicMessage | `SendPublicMessageHandler` | `GameMessageCommandSucceeded/Failed` | `SendGameMessageRequest` |
| SendPrivateMessage | `SendPrivateMessageHandler` | `GameMessageCommandSucceeded/Failed` | `SendGameMessageRequest` |
| EditMessage | `EditMessageHandler` | `GameMessageCommandSucceeded/Failed` | `UpdateGameMessageRequest` |
| DeleteMessage | `DeleteMessageHandler` | `GameMessageCommandSucceeded/Failed` | — |
| GetMessages | `GetMessagesHandler` | (returns `GameTimelinePageView`) | — |
| GetMessageUpdates | `GetMessageUpdatesHandler` | (returns `IReadOnlyList<GameTimelineEntryView>`) | — |
| GetUserGames | `GetUserGamesHandler` | (returns `UserGamesView`) | — |
| GetGamePresence | `GetGamePresenceHandler` | (returns `GamePresenceView`) | — |
| EnsureNexusSession | `EnsureNexusSessionHandler` | — | Nexus sub-folder |
| GetNexusPage | `GetNexusPageHandler` | — | Nexus sub-folder |
| SubmitOrders | `SubmitOrdersHandler` | — | Nexus sub-folder |

### Account Handlers (Spx.Account.Application)

| Feature | Handler | Outcome |
|---------|---------|---------|
| Register | `RegisterHandler` | `RegisterOutcome` (status + optional email/errors) |
| Login | `LoginHandler` | `LoginOutcome` (status + returnUrl) |
| ConfirmEmail | `ConfirmEmailHandler` | `ConfirmEmailOutcome` |
| ForgotPassword | `ForgotPasswordHandler` | `ForgotPasswordOutcome` |
| ResetPassword | `ResetPasswordHandler` | `ResetPasswordOutcome` |
| ResendConfirmation | `ResendConfirmationHandler` | `ResendConfirmationOutcome` |
| Logout | `LogoutHandler` | `LogoutOutcome` |

### Persistence Abstractions (Spx.Game.Application → Spx.Data)

| Interface | Location | EF Adapter |
|-----------|----------|------------|
| `IGamePersistence` | Game.Application | `EfGamePersistence` in Spx.Data |
| `IGameMessagePersistence` | Game.Application | `EfGameMessagePersistence` in Spx.Data |
| `IGamePresenceService` | Game.Application | Implemented in Spx.Grains (`GamePresenceGrain`) |
| `ILobbyInvalidationPublisher` | Game.Application | Implemented in Spx.Grains (`LobbyInvalidationGrain`) |
| `IGameMessageInvalidationPublisher` | Game.Application | Implemented in Spx.Grains |
| `INexusSessionService` | Game.Application/Nexus | Spx.Web adapters (`OrleansNexusRuntimeClient`) |
| `INexusSessionInvalidationPublisher` | Game.Application/Nexus | Implemented in Spx.Grains |

### EF Core Entities (Spx.Data)

| Entity | Table | Notes |
|--------|-------|-------|
| `Game` | Games | Aggregate root for lobby |
| `GamePlayer` | GamePlayers | Join table with player info |
| `GameMessage` | GameMessages | Timeline entries |
| `ApplicationUser` | AspNetUsers | ASP.NET Identity |

### Nexus Domain Types (Spx.Nexus.Domain)

| File | Key Type | Purpose |
|------|----------|---------|
| `NexusState.cs` | `NexusState` | Full game state (root aggregate) |
| `NexusEngine.cs` | `NexusEngine` | Turn resolution logic |
| `NexusCommands.cs` | `InitializeNexusGameCommand`, `NexusTurnOrdersCommand` | Command types |
| `NexusViews.cs` | `NexusGameView` | Player-facing projection |
| `NexusViewQueries.cs` | `NexusViewQueries` | Builds views from state |
| `NexusMap.cs` | `NexusMap` | Hex map structure |
| `NexusMapTopology.cs` | — | Map topology/generation |
| `NexusEnums.cs` | Various enums | Domain enums |
| `HexCoord.cs` | `HexCoord` | Hex coordinate system |
| `NexusCombatSpec.cs` | Unit specs | Combat/balance data |
| `NexusUnitType.cs` | Unit types | Unit definitions |
| `NexusResolveEvents.cs` | Resolution events | Event log |

### Blazor Pages (Spx.Web/Components/Pages)

| Route | Page | Coordinators/State |
|-------|------|-------------------|
| `/` | `Home.razor` | — |
| `/lobby` | `Lobby/Home.razor` | — |
| `/lobby/create` | `Lobby/GameCreate.razor` | — |
| `/lobby/join` | `Lobby/GameJoin.razor` | — |
| `/lobby/games/:id` | `Lobby/Games.razor` | — |
| `/nexus/:gameId` | `Nexus/NexusPage.razor` | `NexusPageDataCoordinator` + `NexusPageActionCoordinator` + `NexusPageMessageCoordinator` + `NexusPagePresenceCoordinator` |
| Account pages | `Account/*.razor` | — |

### Shared Components (Spx.Web.Components/Components)

| Category | Components |
|----------|-----------|
| Account | `AccountLoginForm`, `AccountRegisterForm`, `AccountForgotPasswordForm`, `AccountResetPasswordForm`, `AccountResendConfirmationForm`, `AuthRequiredPrompt` |
| Lobby | `LobbyHeader`, `LobbyListPanel`, `LobbyGameSummaryCard`, `LobbySessionPanel`, `LobbyCurrentUserPanel`, `LobbyMessageComposer`, `InviteCopyButton` |
| Nexus | `NexusHexGridComponent`, `NexusHexCellComponent`, `NexusSelectedHexPanel`, `NexusOrdersPanel`, `NexusPendingOrdersListComponent`, `NexusGameplayPanel`, `NexusGameplayTopInfoBar`, `NexusResolveEventsPanel`, `NexusTimelinePanel`, `NexusSidebarPanelShell`, `NexusEmptySelectionState` |
| General | `FormField`, `FormPage`, `AlertBanner`, `PageHero`, `SectionHeader`, `StatusBadge`, `SurfacePanelShell`, `Tabs` |

### Web Adapters (Spx.Web/Adapters)

| Path | Purpose |
|------|---------|
| `Account/IdentityAccountEmailSender.cs` | Wraps ASP.NET Identity email sender |
| `Account/IdentityAccountIdentityAdapter.cs` | Adapts `IAccountIdentity` to ASP.NET Identity |
| `Account/AccountLinkBuilder.cs` | Builds confirmation/link URLs |
| `Account/LoggingAccountEmailSender.cs` | Dev-only email logging |
| `Account/ResendAccountEmailSender.cs` | Resend email via IEmailSender |
| `Nexus/OrleansNexusRuntimeClient.cs` | Adapts `INexusSessionService` → grain calls |
| `Nexus/WebAdaptersServiceCollectionExtensions.cs` | DI registration |

### Web Endpoints (Spx.Web/Endpoints)

| File | Registers |
|------|-----------|
| `AccountEndpointRouteBuilderExtensions.cs` | Account API endpoints (login, register, etc.) |
| `GameEndpointRouteBuilderExtensions.cs` | Game/lobby API endpoints |

## Handler Patterns

| Area | Pattern | Example |
|------|---------|---------|
| Account | Status-based outcome records | `RegisterOutcome`, `LoginOutcome` |
| Game commands | Split success/failure | `GameCommandOutcome` → `GameCommandSucceeded` / `GameCommandFailed` |
| Game messages | Split success/failure | `GameMessageCommandOutcome` → `GameMessageCommandSucceeded` / `GameMessageCommandFailed` |

## Common Values

| Constant | Value | Defined in |
|----------|-------|------------|
| Message mutation window | 2 minutes | `GameMessageSupport.MessageMutationWindow` |
| Default page size | 20 | `GameMessageSupport.DefaultPageSize` |
| Max create attempts | 10 | `CreateGameHandler.MaxCreateAttempts` |
| Max players | 6 | `Game.MaxPlayers` (hardcoded in CreateGame) |

## Enums (Spx.Game.Application.GameEnums)

| Enum | Values |
|------|--------|
| `GameStatus` | `Open`, `Ended` |
| `GameMessageSenderKind` | `Player`, `Game` |
| `GameMessageKind` | `PlayerPublic`, `PlayerPrivate`, `GameCreated`, `PlayerJoined`, `PlayerLeft`, `GameEnded`, `GameplayEvent` |

## Testing

| Project | Scope | Test framework |
|---------|-------|----------------|
| `tests/Spx.Account.Application.Tests` | Pure account handlers | xUnit + NSubstitute |
| `tests/Spx.Nexus.Domain.Tests` | Pure domain, map, engine | xUnit |
| `tests/Spx.Nexus.Simulation.Tests` | Tactical simulation & balance | xUnit |
| `tests/Spx.Game.Application.Tests` | Pure game handlers | xUnit + NSubstitute |
| `tests/Spx.Game.Application.IntegrationTests` | EF-backed persistence & queries | xUnit + Testcontainers |
| `tests/Spx.Web.Tests` | Endpoint mapping, Identity adapters, bUnit | xUnit + bUnit |
| `tests/Spx.Grains.Tests` | Focused grain behavior | xUnit |
| `tests/Spx.Grains.IntegrationTests` | Orleans cluster integration | xUnit + Testcontainers |

## Common Commands

| Task | Command |
|------|---------|
| Build project | `dotnet build src/<Project>/<Project>.csproj` |
| Test project | `dotnet test tests/<TestProject>/<TestProject>.csproj` |
| Run AppHost | `dotnet run --project src/Spx.AppHost/Spx.AppHost.csproj` |
| Run Playground | `dotnet run --project src/Spx.Web.Playground/Spx.Web.Playground.csproj` |
| Tailwind build | `./tools/tailwind/bin/tailwindcss-linux-x64 -i src/Spx.Web.Components/Styles/app.css -o src/Spx.Web.Components/wwwroot/app.css --minify` |
| Tailwind watch | `./tools/tailwind/bin/tailwindcss-linux-x64 -i src/Spx.Web.Components/Styles/app.css -o src/Spx.Web.Components/wwwroot/app.css --watch` |
| EF migration | `dotnet tool restore && dotnet tool run dotnet-ef -- migrations add <Name> --project src/Spx.Data/Spx.Data.csproj --startup-project src/Spx.Web/Spx.Web.csproj` |
| EF update db | `just dev-db-migrate` (requires running AppHost with Postgres) |
| Coverage single | `dotnet test <project> --collect:"XPlat Code Coverage" --results-directory coverage/testresults` |
| Coverage full | `just dev-coverage-full` |
| Solution build | `dotnet build Spx.slnx` (uses `-warnaserror` via pre-commit hook) |

## Key Files

- `Directory.Build.props` — shared MSBuild (AnalysisMode, EnforceCodeStyleInBuild)
- `Spx.slnx` — solution file
- `justfile` — imports ci/deploy/infra/dev recipes
- `dev.just` — development recipes (build, test, run, watch, migrate, coverage)
- `NEXUS-PROTOCOL-SPEC.md` — protocol spec (read only if asked)
- `TESTING.md` — testing strategy (refer when adding tests)
- `.config/dotnet-tools.json` — local tool manifest (csharpier, reportgenerator, dotnet-ef)

## Framework Versions

- .NET 10.0 (`net10.0`)
- Orleans 10.1.0 (`Microsoft.Orleans.Core.Abstractions`)
- Tailwind CSS v4 (standalone CLI in `tools/tailwind/bin/`)
- NSubstitute 5.3.0
- bUnit for component tests
- xUnit for all tests
- Testcontainers for integration tests

## Available Skills

Use `/skill:name` for detailed guidance on specific topics:

| Skill | When to use |
|-------|-------------|
| `spx-orleans-conventions` | Serialization, surrogates, observers, grain state |
| `spx-type-placement` | Whether a type goes in Domain, Application, or Contracts |
| `spx-component-testing` | bUnit data-testid patterns, TestIds classes |
| `spx-blazor-ui-patterns` | Component/page state extraction decision table |
| `spx-css-tailwind` | Color palette, dynamic classes, @apply, arbitrary values |

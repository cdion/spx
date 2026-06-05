# Testing Strategy

Unit tests are the default. Integration tests are reserved for adapter seams where framework behavior owns the risk. Host-level and end-to-end tests are rare.

The architecture guides where tests live:

- **`Spx.Account.Application`** and **`Spx.Game.Application`** — carry most of the test volume via pure handler/helper tests
- **`Spx.Game.Domain`** — carries unit tests for reusable card and crafting logic
- **`Spx.Data`**, **`Spx.Web`**, and **Orleans integration points** — carry a smaller number of focused integration tests for wiring and framework behavior

## Decision Framework

When adding a new test, ask:

1. Is the behavior decided in `Spx.Account.Application` or `Spx.Game.Application` without needing a real framework implementation?
   → Write a **unit test** in the corresponding `.Application.Tests` project.
2. Does the behavior depend on EF Core, ASP.NET Identity, endpoint binding, or Orleans runtime semantics?
   → Write an **integration test** in the relevant integration test project.
3. Would mocking the dependency make the test prove less than a real adapter check?
   → Keep one or two integration tests for that seam, not dozens.

### Unit test by default

- handler validation
- branching and rule enforcement
- outcome/result shaping
- publisher calls
- normalization and formatting helpers
- edge-case handling for missing or invalid inputs

### Integrate when the framework owns risk

- EF Core persistence and query behavior
- ASP.NET Identity token and sign-in behavior
- HTTP endpoint form binding and redirects
- Orleans observer or runtime interactions that are hard to fake meaningfully

## Test Project Reference

### `tests/Spx.Account.Application.Tests`

Fast unit tests for account application handlers:

- `Login`, `Logout`, `Register`, `ConfirmEmail`, `ForgotPassword`, `ResetPassword`, `ResendConfirmation`

Use fakes for `IAccountIdentity` and `IAccountEmailSender`. Assert validation branches, outcome mapping, follow-up action behavior (send / don't send), and error propagation from ports.

Examples:
- `RegisterHandler` returns `PasswordMismatch` when passwords differ
- `RegisterHandler` does not send email when user creation fails
- `ForgotPasswordHandler` does not send reset email for unknown or unconfirmed users
- `ResetPasswordHandler` maps identity failures to the right outcome
- `LoginHandler` maps sign-in statuses to the right redirect/outcome behavior

### `tests/Spx.Game.Application.Tests`

Fast unit tests for games application handlers and pure helpers:

- `CreateGame`, `JoinGame`, `LeaveGame`, `GetLobby`, `GetUserGames`
- `GetMessages`, `GetMessageUpdates`, `SendPublicMessage`, `SendPrivateMessage`, `EditMessage`, `DeleteMessage`
- `InviteCodeGenerator`, `GameInputNormalizer`, `GameMessageFactory`

Use fakes or in-memory test doubles for application ports. Assert validation rules, branching behavior, command/result mapping, publisher invocation, and normalization/formatting rules.

Examples:
- invite code normalization and generation shape
- player name and game name validation branches
- message body validation and ownership checks
- message update filtering logic
- lobby result shaping for allowed and denied access

### `tests/Spx.Game.Domain.Tests`

Fast unit tests for pure game rule helpers:

- `GameCardCatalog`, `GameCraftingRules`
- Other pure rule helpers that depend only on game enums, card definitions, and deterministic rule evaluation

Assert card classification, initiative weights, refine/produce recipe/result rules, and reusable rule helpers consumed by multiple layers.

### `tests/Spx.Game.Application.IntegrationTests`

Narrow integration tests for the EF-backed games persistence path. Cover only cases where a real database path is important to prove:

- EF query shape and filtering
- persistence of game, player, and message state changes
- important multi-entity updates in a single workflow
- high-value end-to-end application-through-EF scenarios

Do not let this project become the default home for every new games behavior test.

### `tests/Spx.Web.Tests`

Focused integration tests for web adapters:

- HTTP endpoint mapping and redirect behavior
- ASP.NET Identity-backed `IAccountIdentity` behavior
- Other web-owned adapters where framework wiring matters

Good candidates: `MapAccountEndpoints()` redirect/query behavior, `IdentityAccountIdentityAdapter`, email adapter link generation.

Do not use this project for pure `Spx.Account.Application` handler tests.

### `tests/Spx.Grains.Tests`

Focused tests for grain behavior and Orleans-specific edges. Prefer direct grain behavior tests over broad host-level tests unless Orleans runtime wiring is the risk being validated.

### `tests/Spx.Grains.IntegrationTests`

Focused Orleans runtime integration tests. Reserve for cases where risk depends on Orleans activation lifetime, timers, observers, or other runtime semantics that are not meaningfully proved by direct grain tests.

## UI Component Testing Policy

For Blazor/bUnit interaction tests:

- Add a **`data-testid`** to every element a test clicks, hovers, types into, or asserts by presence/absence.
- Use semantic, domain-shaped test ids such as `nexus-map-system-q2-r-2` or `nexus-pending-move-order-0-q2-r-2-to-q1-r-2`.
- **Do not** use CSS classes, Tailwind utilities, `title` attributes, text matching, or `FindAll()[n]` ordering to locate interactive targets.
- When a test needs to observe UI state, expose an explicit state attribute such as `data-state`, `data-focus-state`, `aria-selected`, `aria-expanded`, or `aria-pressed` instead of asserting on class strings.
- If a component lacks a stable selector, add one in the component rather than writing a brittle test around its presentation markup.

Semantic selectors (`role`, `aria-*`, element id) are still appropriate when the accessibility contract itself is the thing under test.

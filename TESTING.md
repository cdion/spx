# Testing Strategy

This repo should be mostly unit tested.

The default rule is:

- test application behavior with unit tests
- test only important adapter seams with integration tests
- keep host-level and end-to-end tests rare

That fits the current architecture:

- `Spx.Account` and `Spx.Games` contain application use cases and should carry most of the test volume
- `Spx.Data`, `Spx.Web`, and Orleans integration points should have a smaller number of focused integration tests where wiring and framework behavior matter

## Current Test Project Layout

### `tests/Spx.Account.Tests`

Purpose: fast unit tests for account application handlers.

This project is the main home for tests covering:

- `Login`
- `Logout`
- `Register`
- `ConfirmEmail`
- `ForgotPassword`
- `ResetPassword`
- `ResendConfirmation`

These tests should use fakes for `IAccountIdentity` and `IAccountEmailSender` and should assert:

- validation branches
- outcome mapping
- whether follow-up actions happen or do not happen
- error propagation from ports

Examples:

- `RegisterHandler` returns `PasswordMismatch` when passwords differ
- `RegisterHandler` does not send email when user creation fails
- `ForgotPasswordHandler` does not send reset email for unknown or unconfirmed users
- `ResetPasswordHandler` maps identity failures to the right outcome
- `LoginHandler` maps sign-in statuses to the right redirect/outcome behavior

### `tests/Spx.Games.Tests`

Purpose: fast unit tests for games application handlers and pure helpers.

This project is the main home for tests covering:

- `CreateGame`
- `JoinGame`
- `LeaveGame`
- `GetLobby`
- `GetUserGames`
- `GetMessages`
- `GetMessageUpdates`
- `SendPublicMessage`
- `SendPrivateMessage`
- `EditMessage`
- `DeleteMessage`
- `InviteCodeGenerator`
- `GameInputNormalizer`
- `GameMessageFactory`

These tests should prefer fakes or in-memory test doubles for the application ports and should assert:

- validation rules
- branching behavior
- command/result mapping
- publisher invocation behavior
- normalization and formatting rules

Examples:

- invite code normalization and generation shape
- player name and game name validation branches
- message body validation and ownership checks
- message update filtering logic
- lobby result shaping for allowed and denied access

### `tests/Spx.Games.IntegrationTests`

Purpose: integration tests for the EF-backed games persistence path.

This project should stay, but it should stay narrow. It should cover only the cases where a real database-backed path is important to prove, such as:

- EF query shape and filtering
- persistence of game, player, and message state changes
- important multi-entity updates in a single workflow
- high-value end-to-end application-through-EF scenarios

As unit coverage grows, avoid turning this project into the default place for every new games behavior test.

### `tests/Spx.Web.Tests`

Purpose: integration tests for important web adapters.

This project should stay focused on:

- HTTP endpoint mapping and redirect behavior
- ASP.NET Identity-backed `IAccountIdentity` behavior
- other web-owned adapters where framework wiring matters

Good candidates here:

- `MapAccountEndpoints()` redirect/query behavior
- `IdentityAccountIdentityAdapter`
- email adapter link generation if it becomes more complex

Do not use this project for pure `Spx.Account` handler tests.

### `tests/Spx.Grains.Tests`

Purpose: focused tests for grain behavior and Orleans-specific edges.

Keep this project small and targeted. Prefer direct grain behavior tests over broad host-level tests unless the Orleans runtime wiring itself is the risk being validated.

## What Should Be Unit Tested vs Integrated

### Unit tests by default

Use unit tests when the behavior is owned by your code and can be expressed through application contracts.

That includes:

- handler validation
- branching and rule enforcement
- outcome/result shaping
- publisher calls
- normalization and formatting helpers
- edge-case handling for missing or invalid inputs

### Integration tests when the framework owns risk

Use integration tests when the risk lives in framework behavior or adapter wiring.

That includes:

- EF Core persistence and query behavior
- ASP.NET Identity token and sign-in behavior
- HTTP endpoint form binding and redirects
- Orleans observer or runtime interactions that are hard to fake meaningfully

## Current Repo Direction

The repo now has the intended split between unit-heavy application tests and narrower integration seams.

In particular:

- `tests/Spx.Account.Tests` exists and should remain the default place for pure account handler behavior
- `tests/Spx.Games.Tests` exists and should remain the default place for pure games handler and helper behavior
- `tests/Spx.Games.IntegrationTests` should stay focused on EF-backed scenarios and avoid becoming the default home for new games behavior tests
- `tests/Spx.Web.Tests` is in a good place and should remain small and adapter-focused
- `tests/Spx.Grains.Tests` is already relatively small and focused

## Practical Rules For New Tests

When adding a new test, ask:

1. Is the behavior decided in `Spx.Account` or `Spx.Games` without needing a real framework implementation?
   Put it in a unit test project.
2. Does the behavior depend on EF Core, ASP.NET Identity, endpoint binding, or Orleans runtime semantics?
   Put it in the relevant integration test project.
3. Would mocking the dependency make the test prove less than a real adapter check?
   Keep one or two integration tests for that seam, not dozens.

## Ongoing Direction

1. Add new `Spx.Account` behavior tests to `tests/Spx.Account.Tests` by default.
2. Add new `Spx.Games` handler and helper tests to `tests/Spx.Games.Tests` by default.
3. Keep `tests/Spx.Games.IntegrationTests` limited to EF-backed scenarios that need a real database path.
4. Keep `tests/Spx.Web.Tests` limited to web adapters and endpoint wiring.
5. Treat integration tests as a thin proving layer, not the default place for new behavior coverage.
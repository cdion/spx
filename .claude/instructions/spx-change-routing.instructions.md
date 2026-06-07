---
name: spx-change-routing
description: 'Route a change to the correct project before editing.'
applyTo: '{src/**,tests/**}'
---

# Spx Change Routing

Use this before changing code when the entry point is visible but the owning behavior is unclear.

## Ownership Map

- `src/Spx.Account.Application`: account use cases, account outcomes, validation and branching for login, register, confirm email, resend confirmation, forgot password, reset password.
- `src/Spx.Game.Application`: game use cases, validation, outcome shaping, request models, and pure helper logic.
- `src/Spx.Web`: Blazor UI, endpoint mapping, adapters, redirect/query behavior, and integration with account or game services.
- `src/Spx.Data`: EF Core model, migrations, and persistence adapters.
- `src/Spx.Grains`: Orleans grain implementations and Orleans-specific behavior.
- `src/Spx.Silo`: Orleans silo host and silo-side bootstrap.
- `src/Spx.AppHost`: Aspire orchestration for local development.

## DI registration

Each layer registers its own services via a `*ServiceCollectionExtensions.cs` static class in the `Microsoft.Extensions.DependencyInjection` namespace. The method signature is `Add<LayerName>Services(this IServiceCollection)` returning `IServiceCollection`. New handlers, adapters, and services must be registered in their own layer's extension — never from an outer layer like `Spx.Web`.

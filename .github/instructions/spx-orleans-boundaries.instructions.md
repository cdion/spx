---
description: 'Use when editing Orleans grains, contracts, silo code, grain observers, or runtime-specific boundaries in this repo.'
name: 'Spx Orleans Boundaries'
applyTo:
  - 'src/Spx.Grains/**'
  - 'src/Spx.Contracts/**'
  - 'src/Spx.Silo/**'
---

# Spx Orleans Boundaries

- Keep Orleans runtime concerns in grains, contracts, and silo code.
- Keep business validation and outcome shaping in the application layer when Orleans is only the transport or runtime mechanism.
- Use focused grain tests for Orleans-specific behavior instead of broad host-level tests.

## Repo Notes

- Contracts should define grain-facing interfaces and shared models.
- Grain implementations should stay small and centered on Orleans behavior or state transitions.
- Do not rely on PostgreSQL grain storage through the Aspire Orleans hosting integration in this repo.
- Grains and adapters that log must be `public sealed partial class` (or `internal sealed partial class`) with `[LoggerMessage]` static partial methods to satisfy CA1848, which is a build error in this repo.

## Domain Type Surrogates

Every Domain type used in a grain interface method signature must have a surrogate pair in `src/Spx.Contracts/GameSessionDomainSurrogates.cs`. Without it, Orleans throws a serialization exception at runtime.

A surrogate pair consists of:

1. A `[GenerateSerializer]` struct named `{Type}Surrogate` with an `[Id(N)]` attribute on each property (zero-indexed, never reuse or skip an ID).
2. A `[RegisterConverter]` `sealed class` named `{Type}Converter` implementing `IConverter<TDomain, TSurrogate>` with `ConvertFromSurrogate` and `ConvertToSurrogate` methods.

Types defined in `Spx.Contracts` itself (not in `Spx.Game.Domain`) use `[GenerateSerializer]` directly and do not need a surrogate.

## Grain Observer Subscription Lifecycle

Observer implementations (`IGameInvalidationObserver`) require explicit reference management — the object reference lives in the cluster client, not the local object:

1. Call `clusterClient.CreateObjectReference<T>(this)` to register the local object as an Orleans observer.
2. Subscribe by passing that reference to a grain method (e.g. `grain.Subscribe(observerReference)`).
3. Track subscription state with an `isSubscribed` bool to guard against double-unsubscribe.
4. On dispose: call `grain.Unsubscribe(observerReference)` and then `clusterClient.DeleteObjectReference<T>(observerReference)` — in a `finally` block to guarantee cleanup even on failure.
5. Observer callback methods (`OnXxxInvalidated`) are fire-and-forget from the grain side. Discard the returned `Task` with `_ = callback()` to avoid blocking grain execution.

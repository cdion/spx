---
description: 'Use when editing Orleans grains, contracts, silo code, grain observers, or runtime-specific boundaries in this repo.'
name: 'Spx Orleans Boundaries'
applyTo: '{src/Spx.Grains/**,src/Spx.Contracts/**,src/Spx.Silo/**}'
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

## Domain Type Serialization

**Owned domain types** (types defined in `Spx.Game.Domain` that this repo controls) are annotated directly with `[GenerateSerializer]` and `[Id(N)]` on each property. The domain project references `Microsoft.Orleans.Core.Abstractions` for these attributes — it is a thin, attribute-only package with no runtime. This is the idiomatic C# approach: attribute packages (like `System.Text.Json`, EF Core's `[Key]`) exist so types can opt into serialization without a runtime dependency.

- Apply `[GenerateSerializer]` to every class, record, or struct that crosses the grain boundary (method signatures or grain state).
- Apply `[Id(N)]` to every property/field, zero-indexed, never skip or reuse IDs.
- For **polymorphic hierarchies** (abstract record bases with multiple subtypes), add `[DerivedType(N, typeof(ConcreteSubtype))]` to the abstract base so Orleans can deserialize the correct concrete type. Never skip a `[DerivedType]` index — it corrupts serialized data.

**Surrogate pairs are only for foreign types** — types defined in third-party packages or the BCL that you cannot annotate directly. If you find yourself writing a surrogate for a type you own, stop and annotate the type directly instead.

A surrogate pair (only for foreign types) consists of:

1. A `[GenerateSerializer]` struct named `{Type}Surrogate` with an `[Id(N)]` attribute on each property.
2. A `[RegisterConverter]` `sealed class` named `{Type}Converter` implementing `IConverter<TDomain, TSurrogate>` with `ConvertFromSurrogate` and `ConvertToSurrogate` methods.

## Grain Observer Subscription Lifecycle

Observer implementations (`IGameInvalidationObserver`) require explicit reference management — the object reference lives in the cluster client, not the local object:

1. Call `clusterClient.CreateObjectReference<T>(this)` to register the local object as an Orleans observer.
2. Subscribe by passing that reference to a grain method (e.g. `grain.Subscribe(observerReference)`).
3. Track subscription state with an `isSubscribed` bool to guard against double-unsubscribe.
4. On dispose: call `grain.Unsubscribe(observerReference)` and then `clusterClient.DeleteObjectReference<T>(observerReference)` — in a `finally` block to guarantee cleanup even on failure.
5. Observer callback methods (`OnXxxInvalidated`) are fire-and-forget from the grain side. Discard the returned `Task` with `_ = callback()` to avoid blocking grain execution.

---
name: spx-orleans-conventions
description: 'Apply Spx Orleans serialization, surrogate, observer, and grain state conventions. Use when annotating types with [GenerateSerializer], writing surrogates for foreign types, subscribing grain observers, or defining grain state. Trigger words: GenerateSerializer, surrogate, grain observer, Orleans serializer, grain state, Id(N), DerivedType, polymorphic serialization.'
argument-hint: 'Describe the Orleans types, serialization boundary, or observer pattern you need to implement.'
---

# Spx Orleans Conventions

## Domain Type Serialization

Types defined in `Spx.Game.Domain` that cross grain boundaries use direct annotation:

- `[GenerateSerializer]` on every class, record, or struct crossing the boundary.
- `[Id(N)]` on every property/field, zero-indexed, never skip or reuse IDs.
- For polymorphic hierarchies: `[DerivedType(N, typeof(ConcreteSubtype))]` on the abstract base — never skip indices.

**Surrogate pairs are only for foreign types** (third-party/BCL). If you own the type, annotate it directly instead.

A surrogate pair (foreign types only):
1. `[GenerateSerializer]` struct `{Type}Surrogate` with `[Id(N)]` on each property.
2. `[RegisterConverter]` sealed class `{Type}Converter` implementing `IConverter<TDomain, TSurrogate>`.

## Grain Observer Subscription Lifecycle

Observer implementations (`IGameInvalidationObserver`) require explicit reference management:

1. `clusterClient.CreateObjectReference<T>(this)` to register the local object.
2. Subscribe by passing that reference to `grain.Subscribe(observerReference)`.
3. Track subscription state with `isSubscribed` bool to guard double-unsubscribe.
4. On dispose: `grain.Unsubscribe(observerReference)` then `clusterClient.DeleteObjectReference<T>(observerReference)` — in a `finally` block.
5. Callback methods (`OnXxxInvalidated`) are fire-and-forget: `_ = callback()`.

## Grain State

- Grain state types must be mutable classes with `[GenerateSerializer]` + `[Id]` for Orleans storage compatibility and stable schema evolution.
- Domain records cannot substitute here.

## Other Rules

- Do not rely on PostgreSQL grain storage through Aspire Orleans hosting integration.
- Grains that log: `public sealed partial class` with `[LoggerMessage]` static methods (CA1848).
- Keep grain implementations small — centered on Orleans behavior or state transitions.

using System.Collections.Immutable;
using Orleans;

namespace Spx.Nexus.Domain;

// Kept for lobby infrastructure compatibility (EnsureSessionAsync etc.)
[GenerateSerializer]
[Immutable]
public sealed record NexusSessionPlayer([property: Id(0)] Guid PlayerId);

[GenerateSerializer]
[Immutable]
public sealed record InitializeNexusGameCommand(
    [property: Id(0)] ImmutableArray<NexusSessionPlayer> Players
);

/// <summary>Build order for one unit type with a count.</summary>
[GenerateSerializer]
[Immutable]
public sealed record NexusBuildOrder(
    [property: Id(0)] NexusUnitType UnitType,
    [property: Id(1)] int Count
);

/// <summary>
/// A single fleet move: pick a subset of units from <see cref="From"/> and move them to
/// the adjacent system <see cref="To"/>. Carrier capacity rules are enforced by the engine.
/// </summary>
[GenerateSerializer]
[Immutable]
public sealed record NexusMoveOrder(
    [property: Id(0)] HexCoord From,
    [property: Id(1)] HexCoord To,
    [property: Id(2)] ImmutableArray<NexusUnitStackGroup> Stacks
);

/// <summary>
/// All orders a player submits for one round. Stored in grain state until both players
/// have submitted, then resolved together.
/// </summary>
[GenerateSerializer]
[Immutable]
public sealed record NexusTurnOrdersCommand(
    [property: Id(0)] Guid PlayerId,
    [property: Id(1)] int ExpectedRoundNumber,
    [property: Id(2)] ImmutableArray<NexusMoveOrder> MoveOrders,
    [property: Id(3)] ImmutableArray<NexusBuildOrder> BuildOrders,
    [property: Id(4)] bool BeginNexusGate
);

[GenerateSerializer]
[Alias("NexusTurnOrdersResult")]
[Immutable]
public abstract record NexusTurnOrdersResult;

[GenerateSerializer]
[Immutable]
public sealed record NexusTurnOrdersAccepted : NexusTurnOrdersResult;

[GenerateSerializer]
[Immutable]
public sealed record NexusTurnOrdersRejected([property: Id(0)] string ErrorMessage)
    : NexusTurnOrdersResult;

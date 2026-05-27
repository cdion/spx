using System.Collections.Immutable;
using Orleans;

namespace Spx.Game.Domain;

[GenerateSerializer]
[Immutable]
public sealed record NexusSystemView(
    [property: Id(0)] HexCoord Coord,
    [property: Id(1)] bool IsNexus,
    [property: Id(2)] int IncomeValue,
    [property: Id(3)] Guid? HomePlayerId,
    [property: Id(4)] Guid? ControlOwner,
    [property: Id(5)] ImmutableDictionary<Guid, ImmutableDictionary<NexusUnitType, int>> Units
);

[GenerateSerializer]
[Immutable]
public sealed record NexusPlayerView(
    [property: Id(0)] Guid PlayerId,
    [property: Id(1)] NexusFactionColor Faction,
    [property: Id(2)] int Energy,
    [property: Id(3)] NexusGateProgress GateProgress,
    [property: Id(4)] bool HasSubmittedOrders,
    [property: Id(5)] bool IsActive,
    // Only populated for the viewing player; null for opponents
    [property: Id(6)] ImmutableArray<NexusMoveOrder>? PendingMoveOrders,
    [property: Id(7)] ImmutableArray<NexusBuildOrder>? PendingBuildOrders,
    [property: Id(8)] bool PendingBeginNexusGate,
    [property: Id(9)] int SupplyPool,
    [property: Id(10)] int CapitalCount
);

[GenerateSerializer]
[Immutable]
public sealed record NexusGameView(
    [property: Id(0)] Guid GameId,
    [property: Id(1)] int RoundNumber,
    [property: Id(3)] ImmutableArray<NexusSystemView> Systems,
    [property: Id(4)] NexusPlayerView CurrentPlayer,
    [property: Id(5)] NexusPlayerView Opponent,
    [property: Id(6)] ImmutableArray<NexusResolveEvent> LastResolveEvents,
    [property: Id(7)] NexusGameCompletion? Completion
);

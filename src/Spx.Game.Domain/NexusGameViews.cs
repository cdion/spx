using System.Collections.Immutable;
using Orleans;

namespace Spx.Game.Domain;

[GenerateSerializer]
[Immutable]
public sealed record NexusHexView(
    [property: Id(0)] HexCoord Coord,
    [property: Id(1)] NexusColonyColor Color,
    [property: Id(2)] bool IsNexus,
    [property: Id(3)] bool IsHome,
    [property: Id(4)] Guid? ColonyOwnerId,
    [property: Id(5)] NexusFactionColor? ColonyOwnerFaction,
    [property: Id(6)] int RedFleetCount,
    [property: Id(7)] int BlueFleetCount
);

[GenerateSerializer]
[Immutable]
public sealed record NexusPlayerView(
    [property: Id(0)] Guid PlayerId,
    [property: Id(1)] NexusFactionColor Faction,
    [property: Id(2)] int RedCredits,
    [property: Id(3)] int BlueCredits,
    [property: Id(4)] int GoldCredits,
    [property: Id(5)] NexusGateProgress GateProgress,
    [property: Id(6)] bool HasSubmittedOrders,
    [property: Id(7)] bool IsActive,
    // Only populated for the current player, null for opponent
    [property: Id(8)] ImmutableArray<NexusFleetOrder>? PendingFleetOrders,
    [property: Id(9)] bool PendingBuildFleet,
    [property: Id(10)] bool PendingBeginNexusGate
);

[GenerateSerializer]
[Immutable]
public sealed record NexusTradeRouteView(
    [property: Id(0)] HexCoord Hex1,
    [property: Id(1)] Guid Owner1,
    [property: Id(2)] NexusFactionColor Faction1,
    [property: Id(3)] HexCoord Hex2,
    [property: Id(4)] Guid Owner2,
    [property: Id(5)] NexusFactionColor Faction2
);

[GenerateSerializer]
[Immutable]
public sealed record NexusGameView(
    [property: Id(0)] Guid GameId,
    [property: Id(1)] int RoundNumber,
    [property: Id(2)] NexusGamePhase Phase,
    [property: Id(3)] ImmutableArray<NexusHexView> Hexes,
    [property: Id(4)] ImmutableArray<NexusTradeRouteView> ActiveTradeRoutes,
    [property: Id(5)] NexusPlayerView CurrentPlayer,
    [property: Id(6)] NexusPlayerView OpponentPlayer,
    [property: Id(7)] ImmutableArray<NexusResolveEvent> ResolveEvents,
    [property: Id(8)] NexusGameCompletion? Completion
);

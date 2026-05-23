using Orleans;

namespace Spx.Game.Domain;

[GenerateSerializer]
[Alias("NexusResolveEvent")]
[Immutable]
public abstract record NexusResolveEvent;

// --- Moves ---

[GenerateSerializer]
[Immutable]
public sealed record NexusMoveEvent(
    [property: Id(0)] Guid OwnerId,
    [property: Id(1)] NexusFactionColor Faction,
    [property: Id(2)] HexCoord From,
    [property: Id(3)] HexCoord To
) : NexusResolveEvent;

[GenerateSerializer]
[Immutable]
public sealed record NexusSpeedBonusMoveEvent(
    [property: Id(0)] Guid OwnerId,
    [property: Id(1)] NexusFactionColor Faction,
    [property: Id(2)] HexCoord From,
    [property: Id(3)] HexCoord To,
    [property: Id(4)] HexCoord TradeRouteEndpoint
) : NexusResolveEvent;

[GenerateSerializer]
[Immutable]
public sealed record NexusUndefendedEntryEvent(
    [property: Id(0)] Guid OwnerId,
    [property: Id(1)] NexusFactionColor Faction,
    [property: Id(2)] HexCoord Hex
) : NexusResolveEvent;

// --- Combat ---

[GenerateSerializer]
[Immutable]
public sealed record NexusCombatParticipant(
    [property: Id(0)] Guid PlayerId,
    [property: Id(1)] NexusFactionColor Faction,
    [property: Id(2)] int Count,
    [property: Id(3)] int Losses
);

[GenerateSerializer]
[Immutable]
public sealed record NexusCombatEvent(
    [property: Id(0)] HexCoord Hex,
    [property: Id(1)] List<NexusCombatParticipant> Participants,
    [property: Id(2)] Guid? WinnerId
) : NexusResolveEvent;

// --- Colonization ---

[GenerateSerializer]
[Immutable]
public sealed record NexusColonizeEvent(
    [property: Id(0)] Guid OwnerId,
    [property: Id(1)] NexusFactionColor Faction,
    [property: Id(2)] HexCoord Hex,
    [property: Id(3)] NexusColonyColor HexColor
) : NexusResolveEvent;

[GenerateSerializer]
[Immutable]
public sealed record NexusColonizeFailedEvent(
    [property: Id(0)] Guid OwnerId,
    [property: Id(1)] NexusFactionColor Faction,
    [property: Id(2)] HexCoord Hex
) : NexusResolveEvent;

// --- Trade Routes ---

[GenerateSerializer]
[Immutable]
public sealed record NexusTradeRouteOpenedEvent(
    [property: Id(0)] HexCoord Hex1,
    [property: Id(1)] Guid Owner1,
    [property: Id(2)] NexusFactionColor Faction1,
    [property: Id(3)] HexCoord Hex2,
    [property: Id(4)] Guid Owner2,
    [property: Id(5)] NexusFactionColor Faction2
) : NexusResolveEvent;

[GenerateSerializer]
[Immutable]
public sealed record NexusTradeRouteClosedEvent(
    [property: Id(0)] HexCoord Hex1,
    [property: Id(1)] Guid Owner1,
    [property: Id(2)] NexusFactionColor Faction1,
    [property: Id(3)] HexCoord Hex2,
    [property: Id(4)] Guid Owner2,
    [property: Id(5)] NexusFactionColor Faction2
) : NexusResolveEvent;

// --- Income ---

[GenerateSerializer]
[Immutable]
public sealed record NexusIncomeEvent(
    [property: Id(0)] Guid PlayerId,
    [property: Id(1)] NexusFactionColor Faction,
    [property: Id(2)] Dictionary<NexusColonyColor, int> Amounts
) : NexusResolveEvent;

// --- Fleet / Gate ---

[GenerateSerializer]
[Immutable]
public sealed record NexusFleetDeployedEvent(
    [property: Id(0)] Guid OwnerId,
    [property: Id(1)] NexusFactionColor Faction,
    [property: Id(2)] HexCoord HomeHex
) : NexusResolveEvent;

[GenerateSerializer]
[Immutable]
public sealed record NexusGateBegunEvent(
    [property: Id(0)] Guid OwnerId,
    [property: Id(1)] NexusFactionColor Faction,
    [property: Id(2)] HexCoord Hex,
    [property: Id(3)] Dictionary<NexusColonyColor, int> Cost
) : NexusResolveEvent;

[GenerateSerializer]
[Immutable]
public sealed record NexusGateProgressedEvent(
    [property: Id(0)] Guid OwnerId,
    [property: Id(1)] NexusFactionColor Faction,
    [property: Id(2)] HexCoord Hex,
    [property: Id(3)] Dictionary<NexusColonyColor, int> Cost
) : NexusResolveEvent;

[GenerateSerializer]
[Immutable]
public sealed record NexusGateCancelledEvent(
    [property: Id(0)] Guid OwnerId,
    [property: Id(1)] NexusFactionColor Faction,
    [property: Id(2)] HexCoord Hex
) : NexusResolveEvent;

// --- Win ---

[GenerateSerializer]
[Immutable]
public sealed record NexusVictoryEvent(
    [property: Id(0)] Guid WinnerId,
    [property: Id(1)] NexusFactionColor WinnerFaction
) : NexusResolveEvent;

[GenerateSerializer]
[Immutable]
public sealed record NexusDrawEvent([property: Id(0)] string Reason) : NexusResolveEvent;

[GenerateSerializer]
[Immutable]
public sealed record NexusTiebreakerVictoryEvent(
    [property: Id(0)] Guid WinnerId,
    [property: Id(1)] NexusFactionColor WinnerFaction,
    [property: Id(2)] int WinnerSystems,
    [property: Id(3)] int LoserSystems
) : NexusResolveEvent;

[GenerateSerializer]
[Immutable]
public sealed record NexusTiebreakerDrawEvent([property: Id(0)] int SystemCount)
    : NexusResolveEvent;

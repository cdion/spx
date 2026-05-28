using System.Collections.Immutable;
using Orleans;

namespace Spx.Nexus.Domain;

[GenerateSerializer]
[Alias("NexusResolveEvent")]
[Immutable]
public abstract record NexusResolveEvent;

// ── Supporting records ────────────────────────────────────────────────────────

/// <summary>One unit type destroyed for one player during a combat phase.</summary>
[GenerateSerializer]
[Immutable]
public sealed record NexusCombatLoss(
    [property: Id(0)] Guid PlayerId,
    [property: Id(1)] NexusUnitType UnitType,
    [property: Id(2)] int Count
);

/// <summary>One individual attack roll during a combat phase.</summary>
[GenerateSerializer]
[Immutable]
public sealed record NexusCombatAttackRoll(
    [property: Id(0)] Guid AttackingPlayerId,
    [property: Id(1)] NexusUnitType AttackerType,
    [property: Id(2)] NexusUnitType TargetType,
    [property: Id(3)] int Roll,
    [property: Id(4)] int Threshold,
    [property: Id(5)] bool IsHit
);

// ── Movement ──────────────────────────────────────────────────────────────────

/// <summary>A player's fleet moved from one system to another.</summary>
[GenerateSerializer]
[Immutable]
public sealed record NexusUnitsMovedEvent(
    [property: Id(0)] Guid PlayerId,
    [property: Id(1)] HexCoord From,
    [property: Id(2)] HexCoord To,
    [property: Id(3)] ImmutableDictionary<NexusUnitType, int> Units,
    [property: Id(4)] bool IsRetreat
) : NexusResolveEvent;

// ── System Control ────────────────────────────────────────────────────────────

/// <summary>
/// A player's planetary units are the only planetary units in the system;
/// control is assigned (or retained) to that player.
/// </summary>
[GenerateSerializer]
[Immutable]
public sealed record NexusPlanetaryControlEvent(
    [property: Id(0)] HexCoord System,
    [property: Id(1)] Guid PlayerId
) : NexusResolveEvent;

/// <summary>Both players have planetary units in the system — control is contested.</summary>
[GenerateSerializer]
[Immutable]
public sealed record NexusSystemContestedEvent([property: Id(0)] HexCoord System)
    : NexusResolveEvent;

/// <summary>No planetary units remain in the system — control becomes uncontrolled.</summary>
[GenerateSerializer]
[Immutable]
public sealed record NexusSystemUncontrolledEvent([property: Id(0)] HexCoord System)
    : NexusResolveEvent;

// ── Combat ────────────────────────────────────────────────────────────────────

/// <summary>Combat has begun in a system between two players.</summary>
[GenerateSerializer]
[Immutable]
public sealed record NexusCombatBeganEvent(
    [property: Id(0)] HexCoord System,
    [property: Id(1)] Guid Player1Id,
    [property: Id(2)] Guid Player2Id
) : NexusResolveEvent;

/// <summary>Results of one combat phase — individual rolls and units destroyed.</summary>
[GenerateSerializer]
[Immutable]
public sealed record NexusPhaseResultEvent(
    [property: Id(0)] HexCoord System,
    [property: Id(1)] int Phase,
    [property: Id(2)] ImmutableArray<NexusCombatLoss> Losses,
    [property: Id(3)] ImmutableArray<NexusCombatAttackRoll> AttackRolls
) : NexusResolveEvent;

/// <summary>All of one player's units have been eliminated from the system.</summary>
[GenerateSerializer]
[Immutable]
public sealed record NexusSystemClearedEvent(
    [property: Id(0)] HexCoord System,
    [property: Id(1)] Guid VictorId
) : NexusResolveEvent;

// ── Income ────────────────────────────────────────────────────────────────────

/// <summary>A player collected income this round.</summary>
[GenerateSerializer]
[Immutable]
public sealed record NexusIncomeEvent(
    [property: Id(0)] Guid PlayerId,
    [property: Id(1)] int Amount,
    [property: Id(2)] ImmutableArray<HexCoord> Sources
) : NexusResolveEvent;

// ── Deployment ────────────────────────────────────────────────────────────────

/// <summary>Units were built and placed in the player's home system.</summary>
[GenerateSerializer]
[Immutable]
public sealed record NexusUnitDeployedEvent(
    [property: Id(0)] Guid PlayerId,
    [property: Id(1)] NexusUnitType UnitType,
    [property: Id(2)] HexCoord HomeSystem,
    [property: Id(3)] int Count
) : NexusResolveEvent;

// ── Nexus Gate ────────────────────────────────────────────────────────────────

/// <summary>A player began constructing the Nexus Gate (first of two turns).</summary>
[GenerateSerializer]
[Immutable]
public sealed record NexusGateStartedEvent(
    [property: Id(0)] Guid PlayerId,
    [property: Id(1)] HexCoord System
) : NexusResolveEvent;

/// <summary>A player completed the Nexus Gate — win condition may now trigger.</summary>
[GenerateSerializer]
[Immutable]
public sealed record NexusGateCompletedEvent(
    [property: Id(0)] Guid PlayerId,
    [property: Id(1)] HexCoord System
) : NexusResolveEvent;

/// <summary>A player's Nexus Gate construction was cancelled (planetary units lost, moved, or insufficient energy).</summary>
[GenerateSerializer]
[Immutable]
public sealed record NexusGateCancelledEvent(
    [property: Id(0)] Guid PlayerId,
    [property: Id(1)] HexCoord System
) : NexusResolveEvent;

// ── Supply ────────────────────────────────────────────────────────────────────

/// <summary>A capital ship was disbanded because the player's capital count exceeded their supply pool.</summary>
[GenerateSerializer]
[Immutable]
public sealed record NexusCapitalDisbandedEvent(
    [property: Id(0)] Guid PlayerId,
    [property: Id(1)] NexusUnitType UnitType,
    [property: Id(2)] HexCoord System,
    [property: Id(3)] int Count
) : NexusResolveEvent;

// ── Game End ──────────────────────────────────────────────────────────────────

[GenerateSerializer]
[Immutable]
public sealed record NexusVictoryEvent([property: Id(0)] Guid WinnerId) : NexusResolveEvent;

[GenerateSerializer]
[Immutable]
public sealed record NexusDrawEvent([property: Id(0)] string Reason) : NexusResolveEvent;

using System.Collections.Immutable;
using Spx.Nexus.Primitives;

namespace Spx.Game.Application.Nexus;

// ── Command types ─────────────────────────────────────────────────────────────

public sealed record NexusSubmitTurnCommand(
    Guid PlayerId,
    int ExpectedRoundNumber,
    ImmutableArray<NexusMoveRequest> MoveOrders,
    ImmutableArray<NexusBuildRequest> BuildOrders,
    bool BeginNexusGate
);

public sealed record NexusMoveRequest(
    HexCoord From,
    HexCoord To,
    ImmutableDictionary<NexusUnitType, int> Units
);

public sealed record NexusBuildRequest(NexusUnitType UnitType, int Count);

// ── Session view ──────────────────────────────────────────────────────────────

public sealed record NexusSessionView(
    Guid GameId,
    int RoundNumber,
    ImmutableArray<NexusSystemSnapshot> Systems,
    NexusPlayerSnapshot CurrentPlayer,
    NexusPlayerSnapshot Opponent,
    ImmutableArray<NexusSessionEvent> ResolveEvents,
    NexusSessionCompletion? Completion
);

public sealed record NexusSystemSnapshot(
    HexCoord Coord,
    bool IsNexus,
    int IncomeValue,
    Guid? HomePlayerId,
    Guid? ControlOwner,
    ImmutableDictionary<Guid, ImmutableDictionary<NexusUnitType, int>> Units
);

public sealed record NexusPlayerSnapshot(
    Guid PlayerId,
    NexusFactionColor Faction,
    int Energy,
    NexusGateProgress GateProgress,
    bool HasSubmittedOrders,
    bool IsActive,
    ImmutableArray<NexusMoveRequest>? PendingMoveOrders,
    ImmutableArray<NexusBuildRequest>? PendingBuildOrders,
    bool PendingBeginNexusGate,
    int SupplyPool,
    int CapitalCount
);

public sealed record NexusSessionCompletion(NexusSessionOutcome Outcome, Guid? WinnerId);

public enum NexusSessionOutcome
{
    Victory = 0,
    Draw = 1,
}

// ── Session events ────────────────────────────────────────────────────────────

public abstract record NexusSessionEvent;

public sealed record NexusUnitsMovedSessionEvent(
    Guid PlayerId,
    HexCoord From,
    HexCoord To,
    ImmutableDictionary<NexusUnitType, int> Units,
    bool IsRetreat
) : NexusSessionEvent;

public sealed record NexusPlanetaryControlSessionEvent(HexCoord System, Guid PlayerId)
    : NexusSessionEvent;

public sealed record NexusSystemContestedSessionEvent(HexCoord System) : NexusSessionEvent;

public sealed record NexusSystemUncontrolledSessionEvent(HexCoord System) : NexusSessionEvent;

public sealed record NexusCombatBeganSessionEvent(HexCoord System, Guid Player1Id, Guid Player2Id)
    : NexusSessionEvent;

public sealed record NexusPhaseResultSessionEvent(HexCoord System) : NexusSessionEvent;

public sealed record NexusSystemClearedSessionEvent(HexCoord System, Guid VictorId)
    : NexusSessionEvent;

public sealed record NexusIncomeSessionEvent(Guid PlayerId, ImmutableArray<HexCoord> Sources)
    : NexusSessionEvent;

public sealed record NexusUnitDeployedSessionEvent(
    Guid PlayerId,
    NexusUnitType UnitType,
    HexCoord HomeSystem,
    int Count
) : NexusSessionEvent;

public sealed record NexusGateStartedSessionEvent(Guid PlayerId, HexCoord System)
    : NexusSessionEvent;

public sealed record NexusGateCompletedSessionEvent(Guid PlayerId, HexCoord System)
    : NexusSessionEvent;

public sealed record NexusGateCancelledSessionEvent(Guid PlayerId, HexCoord System)
    : NexusSessionEvent;

public sealed record NexusCapitalDisbandedSessionEvent(
    Guid PlayerId,
    NexusUnitType UnitType,
    HexCoord System,
    int Count
) : NexusSessionEvent;

public sealed record NexusVictorySessionEvent(Guid WinnerId) : NexusSessionEvent;

public sealed record NexusDrawSessionEvent(string Reason) : NexusSessionEvent;

public sealed record NexusUnknownSessionEvent : NexusSessionEvent;

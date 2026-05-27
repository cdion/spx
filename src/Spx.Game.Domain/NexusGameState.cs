using Orleans;

namespace Spx.Game.Domain;

public enum NexusFactionColor
{
    Red = 0,
    Blue = 1,
}

public enum NexusGateProgress
{
    None = 0,
    Started = 1,
    Completed = 2,
}

public enum NexusGameOutcome
{
    Victory = 0,
    Draw = 1,
}

[GenerateSerializer]
[Immutable]
public sealed record NexusGameCompletion(
    [property: Id(0)] NexusGameOutcome Outcome,
    [property: Id(1)] Guid? WinnerId
);

[GenerateSerializer]
public sealed class NexusSystemState
{
    [Id(0)]
    public HexCoord Coord { get; set; }

    [Id(1)]
    public bool IsNexus { get; set; }

    /// <summary>Null for non-home systems; set to a player ID for home systems.</summary>
    [Id(2)]
    public Guid? HomePlayerId { get; set; }

    /// <summary>Energy per turn. 0 for Nexus; 3 for home systems; 2–5 for income systems.</summary>
    [Id(3)]
    public int IncomeValue { get; set; }

    /// <summary>Null = uncontrolled; set to a player ID when that player controls the system.</summary>
    [Id(4)]
    public Guid? ControlOwner { get; set; }

    /// <summary>Units present: player ID → unit type → count.</summary>
    [Id(5)]
    public Dictionary<Guid, Dictionary<NexusUnitType, int>> Units { get; set; } = [];

    public int GetUnitCount(Guid playerId, NexusUnitType unitType)
    {
        if (!Units.TryGetValue(playerId, out var byType))
            return 0;
        return byType.TryGetValue(unitType, out var count) ? count : 0;
    }

    public Dictionary<NexusUnitType, int> GetPlayerUnits(Guid playerId) =>
        Units.TryGetValue(playerId, out var byType) ? byType : [];

    public void AddUnits(Guid playerId, NexusUnitType unitType, int count)
    {
        if (!Units.TryGetValue(playerId, out var byType))
        {
            byType = [];
            Units[playerId] = byType;
        }

        byType[unitType] = GetUnitCount(playerId, unitType) + count;
    }

    public void RemoveUnits(Guid playerId, NexusUnitType unitType, int count)
    {
        var current = GetUnitCount(playerId, unitType);
        var remaining = current - count;

        if (remaining <= 0)
        {
            if (Units.TryGetValue(playerId, out var byType))
            {
                byType.Remove(unitType);
                if (byType.Count == 0)
                    Units.Remove(playerId);
            }
        }
        else
        {
            Units[playerId][unitType] = remaining;
        }
    }

    public bool HasPlanetaryUnits(Guid playerId)
    {
        foreach (var unitType in GetPlayerUnits(playerId).Keys)
        {
            if (unitType.IsPlanetary())
                return true;
        }

        return false;
    }

    public bool HasAnyUnits(Guid playerId) => GetPlayerUnits(playerId).Count > 0;
}

[GenerateSerializer]
public sealed class NexusPlayerState
{
    [Id(0)]
    public Guid PlayerId { get; set; }

    [Id(1)]
    public NexusFactionColor Faction { get; set; }

    [Id(2)]
    public int Energy { get; set; }

    [Id(3)]
    public NexusGateProgress GateProgress { get; set; }

    [Id(4)]
    public bool IsActive { get; set; }

    [Id(5)]
    public bool HasSubmittedOrders { get; set; }

    /// <summary>Stored when this player submits orders, cleared after resolution.</summary>
    [Id(6)]
    public List<NexusMoveOrder> PendingMoveOrders { get; set; } = [];

    /// <summary>Build orders for this turn, stored pending resolution.</summary>
    [Id(7)]
    public List<NexusBuildOrder> PendingBuildOrders { get; set; } = [];

    [Id(8)]
    public bool PendingBeginNexusGate { get; set; }
}

[GenerateSerializer]
public sealed class NexusGameState
{
    [Id(0)]
    public Guid GameId { get; set; }

    [Id(1)]
    public int RoundNumber { get; set; } = 1;

    [Id(3)]
    public List<NexusSystemState> Systems { get; set; } = [];

    [Id(4)]
    public List<NexusPlayerState> Players { get; set; } = [];

    [Id(5)]
    public NexusGameCompletion? Completion { get; set; }

    [Id(6)]
    public List<NexusResolveEvent> LastResolveEvents { get; set; } = [];
}

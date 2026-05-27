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
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1711:IdentifiersShouldNotHaveIncorrectSuffix",
    Justification = "'Stack' is wargame domain terminology for a group of units sharing a hex, not a data structure."
)]
public sealed class NexusUnitStack
{
    [Id(0)]
    public NexusUnitType UnitType { get; set; }

    [Id(1)]
    public int HitsAbsorbed { get; set; }

    [Id(2)]
    public int Count { get; set; }
}

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

    /// <summary>Units present: player ID → list of stacks (unit type + damage level + count).</summary>
    [Id(5)]
    public Dictionary<Guid, List<NexusUnitStack>> Units { get; set; } = [];

    public int GetUnitCount(Guid playerId, NexusUnitType unitType)
    {
        if (!Units.TryGetValue(playerId, out var stacks))
            return 0;
        var total = 0;
        foreach (var stack in stacks)
            if (stack.UnitType == unitType)
                total += stack.Count;
        return total;
    }

    public Dictionary<NexusUnitType, int> GetPlayerUnits(Guid playerId)
    {
        if (!Units.TryGetValue(playerId, out var stacks))
            return [];
        var result = new Dictionary<NexusUnitType, int>();
        foreach (var stack in stacks)
            result[stack.UnitType] = result.GetValueOrDefault(stack.UnitType) + stack.Count;
        return result;
    }

    public IReadOnlyList<NexusUnitStack> GetPlayerStacks(Guid playerId) =>
        Units.TryGetValue(playerId, out var stacks) ? stacks : [];

    public void AddUnits(Guid playerId, NexusUnitType unitType, int count, int hitsAbsorbed = 0)
    {
        if (count <= 0)
            return;
        if (!Units.TryGetValue(playerId, out var stacks))
        {
            stacks = [];
            Units[playerId] = stacks;
        }

        var existing = stacks.Find(s => s.UnitType == unitType && s.HitsAbsorbed == hitsAbsorbed);
        if (existing is not null)
            existing.Count += count;
        else
            stacks.Add(
                new NexusUnitStack
                {
                    UnitType = unitType,
                    HitsAbsorbed = hitsAbsorbed,
                    Count = count,
                }
            );
    }

    public void RemoveUnits(
        Guid playerId,
        NexusUnitType unitType,
        int count,
        bool retreating = false
    ) => TakeUnits(playerId, unitType, count, retreating);

    /// <summary>
    /// Removes <paramref name="count"/> units of <paramref name="unitType"/> and returns
    /// the (HitsAbsorbed, Count) pairs actually taken, ordered by the retreating rule.
    /// </summary>
    public List<(int HitsAbsorbed, int Count)> TakeUnits(
        Guid playerId,
        NexusUnitType unitType,
        int count,
        bool retreating = false
    )
    {
        var taken = new List<(int HitsAbsorbed, int Count)>();
        if (!Units.TryGetValue(playerId, out var stacks))
            return taken;

        var ordered = retreating
            ? stacks
                .Where(s => s.UnitType == unitType)
                .OrderByDescending(s => s.HitsAbsorbed)
                .ToList()
            : stacks.Where(s => s.UnitType == unitType).OrderBy(s => s.HitsAbsorbed).ToList();

        var remaining = count;
        foreach (var stack in ordered)
        {
            if (remaining <= 0)
                break;
            var take = Math.Min(stack.Count, remaining);
            stack.Count -= take;
            remaining -= take;
            taken.Add((stack.HitsAbsorbed, take));
        }

        stacks.RemoveAll(s => s.Count <= 0);
        if (stacks.Count == 0)
            Units.Remove(playerId);

        return taken;
    }

    public bool HasPlanetaryUnits(Guid playerId)
    {
        if (!Units.TryGetValue(playerId, out var stacks))
            return false;
        return stacks.Any(s => s.UnitType.IsPlanetary());
    }

    public bool HasAnyUnits(Guid playerId) =>
        Units.TryGetValue(playerId, out var stacks) && stacks.Count > 0;
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

using Orleans;

namespace Spx.Game.Domain;

public enum NexusFactionColor
{
    Red = 0,
    Blue = 1,
    Green = 2,
    Yellow = 3,
}

public enum NexusGamePhase
{
    Planning = 0,
    Ended = 1,
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
public sealed record NexusTradeRoute(
    [property: Id(0)] HexCoord Hex1,
    [property: Id(1)] Guid Owner1,
    [property: Id(2)] HexCoord Hex2,
    [property: Id(3)] Guid Owner2
);

[GenerateSerializer]
[Immutable]
public sealed record NexusGameCompletion(
    [property: Id(0)] NexusGameOutcome Outcome,
    [property: Id(1)] Guid? WinnerId
);

[GenerateSerializer]
public sealed class NexusPlayerState
{
    [Id(0)]
    public Guid PlayerId { get; set; }

    [Id(1)]
    public NexusFactionColor Faction { get; set; }

    [Id(2)]
    public Dictionary<NexusColonyColor, int> Credits { get; set; } = [];

    [Id(3)]
    public NexusGateProgress GateProgress { get; set; }

    [Id(4)]
    public bool IsActive { get; set; }

    [Id(5)]
    public bool HasSubmittedOrders { get; set; }

    // Stored between first-player submit and resolve
    [Id(6)]
    public List<NexusFleetOrder> PendingFleetOrders { get; set; } = [];

    [Id(7)]
    public bool PendingBuildFleet { get; set; }

    [Id(8)]
    public bool PendingBeginNexusGate { get; set; }

    // Transient during resolve — not persisted beyond the resolve step
    [Id(9)]
    public bool PendingFleetDeployment { get; set; }

    [Id(10)]
    public NexusGateProgress GateProgressBeforeThisTurn { get; set; }

    public int GetCredits(NexusColonyColor color) =>
        Credits.TryGetValue(color, out var value) ? value : 0;

    public void AddCredits(NexusColonyColor color, int amount)
    {
        Credits[color] = GetCredits(color) + amount;
    }

    public void DeductCredits(NexusColonyColor color, int amount)
    {
        Credits[color] = GetCredits(color) - amount;
    }
}

[GenerateSerializer]
public sealed class NexusHexState
{
    [Id(0)]
    public HexCoord Coord { get; set; }

    [Id(1)]
    public Guid? ColonyOwnerId { get; set; }

    [Id(2)]
    public Dictionary<NexusFactionColor, int> Fleets { get; set; } = [];

    public int GetFleets(NexusFactionColor faction) =>
        Fleets.TryGetValue(faction, out var value) ? value : 0;

    public void AddFleets(NexusFactionColor faction, int count)
    {
        Fleets[faction] = GetFleets(faction) + count;
    }

    public void SetFleets(NexusFactionColor faction, int count)
    {
        if (count == 0)
            Fleets.Remove(faction);
        else
            Fleets[faction] = count;
    }

    public void RemoveFleets(NexusFactionColor faction, int count)
    {
        SetFleets(faction, GetFleets(faction) - count);
    }
}

[GenerateSerializer]
public sealed class NexusGameState
{
    [Id(0)]
    public List<NexusPlayerState> Players { get; set; } = [];

    [Id(1)]
    public List<NexusHexState> Hexes { get; set; } = [];

    [Id(2)]
    public int RoundNumber { get; set; } = 1;

    [Id(3)]
    public NexusGamePhase Phase { get; set; } = NexusGamePhase.Planning;

    [Id(4)]
    public List<NexusResolveEvent> ResolveEvents { get; set; } = [];

    [Id(5)]
    public List<NexusTradeRoute> ActiveTradeRoutes { get; set; } = [];

    [Id(6)]
    public NexusGameCompletion? Completion { get; set; }
}

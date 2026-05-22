using Orleans;

namespace Spx.Game.Domain;

public enum NexusFactionColor
{
    Red = 0,
    Blue = 1,
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
    public int RedCredits { get; set; }

    [Id(3)]
    public int BlueCredits { get; set; }

    [Id(4)]
    public int GoldCredits { get; set; }

    [Id(5)]
    public NexusGateProgress GateProgress { get; set; }

    [Id(6)]
    public bool IsActive { get; set; }

    [Id(7)]
    public bool HasSubmittedOrders { get; set; }

    // Stored between first-player submit and resolve
    [Id(8)]
    public List<NexusFleetOrder> PendingFleetOrders { get; set; } = [];

    [Id(9)]
    public bool PendingBuildFleet { get; set; }

    [Id(10)]
    public bool PendingBeginNexusGate { get; set; }

    // Transient during resolve — not persisted beyond the resolve step
    [Id(11)]
    public bool PendingFleetDeployment { get; set; }

    [Id(12)]
    public NexusGateProgress GateProgressBeforeThisTurn { get; set; }
}

[GenerateSerializer]
public sealed class NexusHexState
{
    [Id(0)]
    public HexCoord Coord { get; set; }

    [Id(1)]
    public Guid? ColonyOwnerId { get; set; }

    [Id(2)]
    public int RedFleets { get; set; }

    [Id(3)]
    public int BlueFleets { get; set; }
}

[GenerateSerializer]
public sealed class NexusGameState
{
    [Id(0)]
    public NexusPlayerState? RedPlayer { get; set; }

    [Id(1)]
    public NexusPlayerState? BluePlayer { get; set; }

    [Id(2)]
    public List<NexusHexState> Hexes { get; set; } = [];

    [Id(3)]
    public int RoundNumber { get; set; } = 1;

    [Id(4)]
    public NexusGamePhase Phase { get; set; } = NexusGamePhase.Planning;

    [Id(5)]
    public List<NexusResolveEvent> ResolveEvents { get; set; } = [];

    [Id(6)]
    public List<NexusTradeRoute> ActiveTradeRoutes { get; set; } = [];

    [Id(7)]
    public NexusGameCompletion? Completion { get; set; }
}

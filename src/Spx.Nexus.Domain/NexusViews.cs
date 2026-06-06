using System.Collections.Immutable;
using Orleans;

namespace Spx.Nexus.Domain;

[GenerateSerializer]
[Immutable]
public sealed record NexusUnitStackGroup(
    [property: Id(0)] Guid DesignId,
    [property: Id(1)] NexusUnitCategory Category,
    [property: Id(2)] int RemainingHits,
    [property: Id(3)] int Count,
    [property: Id(4)] string DesignName = ""
);

[GenerateSerializer]
[Immutable]
public sealed record NexusSystemView(
    [property: Id(0)] HexCoord Coord,
    [property: Id(1)] bool IsNexus,
    [property: Id(2)] int IncomeValue,
    [property: Id(3)] Guid? HomePlayerId,
    [property: Id(4)] Guid? ControlOwner,
    [property: Id(5)] ImmutableDictionary<Guid, ImmutableArray<NexusUnitStackGroup>> UnitStacks,
    [property: Id(6)]
        ImmutableDictionary<Guid, ImmutableArray<NexusUnitStackGroup>>? MovableUnitStacks = null
)
{
    public ImmutableArray<NexusUnitStackGroup> GetPlayerStacks(Guid playerId) =>
        UnitStacks.TryGetValue(playerId, out var stacks)
            ? stacks
            : ImmutableArray<NexusUnitStackGroup>.Empty;

    public ImmutableArray<NexusUnitStackGroup> GetPlayerMovableStacks(Guid playerId)
    {
        if (MovableUnitStacks is null)
            return GetPlayerStacks(playerId);

        return MovableUnitStacks.TryGetValue(playerId, out var stacks)
            ? stacks
            : ImmutableArray<NexusUnitStackGroup>.Empty;
    }

    public ImmutableDictionary<Guid, int> GetPlayerUnitCounts(Guid playerId)
    {
        var builder = ImmutableDictionary.CreateBuilder<Guid, int>();
        foreach (var stack in GetPlayerStacks(playerId))
        {
            builder.TryGetValue(stack.DesignId, out var current);
            builder[stack.DesignId] = current + stack.Count;
        }

        return builder.ToImmutable();
    }
}

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
    [property: Id(10)] int CapitalCount,
    // Own player only
    [property: Id(11)] ImmutableArray<NexusUnitDesign>? Designs = null
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

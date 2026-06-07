using System.Collections.Immutable;
using Spx.Nexus.Domain;

namespace Spx.Nexus.Domain.Tests;

/// <summary>Tests for multi-hop (Drive module) move orders.</summary>
public class NexusMultiHopMoveTests
{
    private static readonly Guid P1Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid P2Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    // Map layout used by these tests (all valid coords):
    //   P1Home=(2,-2) → Hop1=(1,-2) → Hop2=(0,-2) → Hop3=(0,-1)
    private static readonly HexCoord P1Home = NexusMap.Player1HomeCoord; // (2,-2)
    private static readonly HexCoord Hop1 = new(1, -2);
    private static readonly HexCoord Hop2 = new(0, -2);
    private static readonly HexCoord Hop3 = new(0, -1);

    // Capital hull + Drive(1) → Move=2
    private static readonly NexusUnitDesign FastCarrier = new()
    {
        DesignId = Guid.NewGuid(),
        Name = "FastCarrier",
        Hull = NexusUnitCategory.Capital,
        Modules = [new Drive(1), new Hangar(4)],
    };

    // Capital hull + Drive(2) → Move=3
    private static readonly NexusUnitDesign LongRangeCarrier = new()
    {
        DesignId = Guid.NewGuid(),
        Name = "LongRangeCarrier",
        Hull = NexusUnitCategory.Capital,
        Modules = [new Drive(2), new Hangar(4)],
    };

    // Slow capital (Move=1, no Drive)
    private static readonly NexusUnitDesign SlowCarrier = new()
    {
        DesignId = Guid.NewGuid(),
        Name = "SlowCarrier",
        Hull = NexusUnitCategory.Capital,
        Modules = [new Hangar(4)],
    };

    private static NexusState MakeState()
    {
        var state = new NexusState();
        NexusEngine.Initialize(
            state,
            new InitializeNexusGameCommand(
                ImmutableArray.Create(new NexusSessionPlayer(P1Id), new NexusSessionPlayer(P2Id))
            ),
            new Random(42)
        );
        TestState.RegisterDesigns(state);
        foreach (var player in state.Players)
        {
            foreach (var design in new[] { FastCarrier, LongRangeCarrier, SlowCarrier })
                player.Designs.Add(design);
        }
        return state;
    }

    private static NexusTurnOrdersResult Submit(
        NexusState state,
        Guid playerId,
        ImmutableArray<NexusMoveOrder> moves
    ) =>
        NexusEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(
                playerId,
                state.RoundNumber,
                moves,
                ImmutableArray<NexusBuildOrder>.Empty,
                false
            ),
            new Random(42)
        );

    private static NexusMoveOrder MultiHopMove(
        HexCoord from,
        ImmutableArray<HexCoord> waypoints,
        params (NexusUnitDesign Design, int Count)[] units
    ) =>
        new(
            from,
            waypoints,
            units
                .Select(u => new NexusUnitStackGroup(
                    u.Design.DesignId,
                    u.Design.Hull,
                    NexusHullBaselines.GetProfile(u.Design).Hits,
                    u.Count
                ))
                .ToImmutableArray()
        );

    [Fact]
    public void TwoHopMove_WithDrive1Capital_IsAccepted()
    {
        var s = MakeState();
        var p1Home = s.Systems.First(sys => sys.HomePlayerId == P1Id);
        p1Home.AddUnits(
            P1Id,
            FastCarrier.DesignId,
            NexusUnitCategory.Capital,
            1,
            NexusHullBaselines.GetProfile(FastCarrier).Hits
        );

        var waypoints = ImmutableArray.Create(Hop1, Hop2);
        var result = Submit(s, P1Id, [MultiHopMove(P1Home, waypoints, (FastCarrier, 1))]);

        Assert.IsAssignableFrom<NexusTurnOrdersAccepted>(result);
    }

    [Fact]
    public void PathLongerThanFleetMove_IsRejected()
    {
        var s = MakeState();
        var p1Home = s.Systems.First(sys => sys.HomePlayerId == P1Id);
        p1Home.AddUnits(
            P1Id,
            FastCarrier.DesignId,
            NexusUnitCategory.Capital,
            1,
            NexusHullBaselines.GetProfile(FastCarrier).Hits
        );

        // FastCarrier has Move=2; 3-hop path exceeds that
        var waypoints = ImmutableArray.Create(Hop1, Hop2, Hop3);
        var result = Submit(s, P1Id, [MultiHopMove(P1Home, waypoints, (FastCarrier, 1))]);

        var rejected = Assert.IsAssignableFrom<NexusTurnOrdersRejected>(result);
        Assert.Contains("Fleet can only move 2", rejected.ErrorMessage);
    }

    [Fact]
    public void PathWithCycle_IsRejected()
    {
        var s = MakeState();
        var p1Home = s.Systems.First(sys => sys.HomePlayerId == P1Id);
        p1Home.AddUnits(
            P1Id,
            LongRangeCarrier.DesignId,
            NexusUnitCategory.Capital,
            1,
            NexusHullBaselines.GetProfile(LongRangeCarrier).Hits
        );

        // (2,-2) → (1,-2) → (2,-2) revisits From
        var waypoints = ImmutableArray.Create(Hop1, P1Home);
        var result = Submit(s, P1Id, [MultiHopMove(P1Home, waypoints, (LongRangeCarrier, 1))]);

        var rejected = Assert.IsAssignableFrom<NexusTurnOrdersRejected>(result);
        Assert.Contains("cannot revisit", rejected.ErrorMessage);
    }

    [Fact]
    public void EnemyFleetAtWaypoint_BlocksPath()
    {
        var s = MakeState();
        var p1Home = s.Systems.First(sys => sys.HomePlayerId == P1Id);
        p1Home.AddUnits(
            P1Id,
            FastCarrier.DesignId,
            NexusUnitCategory.Capital,
            1,
            NexusHullBaselines.GetProfile(FastCarrier).Hits
        );

        // Place an enemy Strike unit at Hop1 (the intermediate waypoint)
        var hop1System = s.Systems.First(sys => sys.Coord == Hop1);
        hop1System.AddUnits(
            P2Id,
            TestDesigns.Fighter.DesignId,
            NexusUnitCategory.Strike,
            1,
            NexusHullBaselines.GetProfile(TestDesigns.Fighter).Hits
        );
        foreach (var player in s.Players)
            player.Designs.Add(TestDesigns.Fighter);

        var waypoints = ImmutableArray.Create(Hop1, Hop2);
        var result = Submit(s, P1Id, [MultiHopMove(P1Home, waypoints, (FastCarrier, 1))]);

        var rejected = Assert.IsAssignableFrom<NexusTurnOrdersRejected>(result);
        Assert.Contains("enemy fleet present", rejected.ErrorMessage);
    }

    [Fact]
    public void EnemyFleetAtDestination_IsAllowed()
    {
        var s = MakeState();
        var p1Home = s.Systems.First(sys => sys.HomePlayerId == P1Id);
        p1Home.AddUnits(
            P1Id,
            FastCarrier.DesignId,
            NexusUnitCategory.Capital,
            1,
            NexusHullBaselines.GetProfile(FastCarrier).Hits
        );

        // Enemy at Hop2 (the destination), but Hop1 is clear
        var hop2System = s.Systems.First(sys => sys.Coord == Hop2);
        hop2System.AddUnits(
            P2Id,
            TestDesigns.Fighter.DesignId,
            NexusUnitCategory.Strike,
            1,
            NexusHullBaselines.GetProfile(TestDesigns.Fighter).Hits
        );
        foreach (var player in s.Players)
            player.Designs.Add(TestDesigns.Fighter);

        var waypoints = ImmutableArray.Create(Hop1, Hop2);
        var result = Submit(s, P1Id, [MultiHopMove(P1Home, waypoints, (FastCarrier, 1))]);

        Assert.IsAssignableFrom<NexusTurnOrdersAccepted>(result);
    }

    [Fact]
    public void FleetMove_UsesMinAcrossMovableStacks()
    {
        var s = MakeState();
        var p1Home = s.Systems.First(sys => sys.HomePlayerId == P1Id);
        // FastCarrier (Move=2) + SlowCarrier (Move=1) → fleet move = min(2,1) = 1
        p1Home.AddUnits(
            P1Id,
            FastCarrier.DesignId,
            NexusUnitCategory.Capital,
            1,
            NexusHullBaselines.GetProfile(FastCarrier).Hits
        );
        p1Home.AddUnits(
            P1Id,
            SlowCarrier.DesignId,
            NexusUnitCategory.Capital,
            1,
            NexusHullBaselines.GetProfile(SlowCarrier).Hits
        );

        // 2-hop path exceeds the mixed fleet's effective move of 1
        var waypoints = ImmutableArray.Create(Hop1, Hop2);
        var result = Submit(
            s,
            P1Id,
            [MultiHopMove(P1Home, waypoints, (FastCarrier, 1), (SlowCarrier, 1))]
        );

        var rejected = Assert.IsAssignableFrom<NexusTurnOrdersRejected>(result);
        Assert.Contains("Fleet can only move 1", rejected.ErrorMessage);
    }
}

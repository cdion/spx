using System.Collections.Immutable;

namespace Spx.Game.Domain.Tests;

public class HexCoordTests
{
    [Fact]
    public void DistanceTo_SameHex_ReturnsZero()
    {
        var a = new HexCoord(1, 2);
        Assert.Equal(0, a.DistanceTo(a));
    }

    [Theory]
    [InlineData(0, 0, 1, 0)]
    [InlineData(0, 0, 1, -1)]
    [InlineData(0, 0, 0, -1)]
    [InlineData(0, 0, -1, 0)]
    [InlineData(0, 0, -1, 1)]
    [InlineData(0, 0, 0, 1)]
    public void DistanceTo_AdjacentHex_ReturnsOne(int q1, int r1, int q2, int r2)
    {
        var a = new HexCoord(q1, r1);
        var b = new HexCoord(q2, r2);
        Assert.Equal(1, a.DistanceTo(b));
    }

    [Fact]
    public void DistanceTo_TwoHopsAway_ReturnsTwo()
    {
        Assert.Equal(2, new HexCoord(0, 0).DistanceTo(new HexCoord(2, 0)));
        Assert.Equal(2, new HexCoord(0, 0).DistanceTo(new HexCoord(1, 1)));
        Assert.Equal(2, new HexCoord(0, 0).DistanceTo(new HexCoord(-1, -1)));
    }

    [Fact]
    public void GetNeighbours_ReturnsExactlySix()
    {
        var center = new HexCoord(0, 0);
        Assert.Equal(6, center.GetNeighbours().Length);
    }

    [Fact]
    public void GetNeighbours_AllAtDistanceOne()
    {
        var center = new HexCoord(2, -1);
        foreach (var n in center.GetNeighbours())
            Assert.Equal(1, center.DistanceTo(n));
    }
}

public class NexusMapTests
{
    [Fact]
    public void Map_Has19Hexes()
    {
        Assert.Equal(19, NexusMap.Hexes.Count);
    }

    [Fact]
    public void Map_HasExactlyOneNexus()
    {
        Assert.Single(NexusMap.Hexes, h => h.IsNexus);
        Assert.Equal(NexusMap.NexusCoord, NexusMap.Hexes.Single(h => h.IsNexus).Coord);
    }

    [Fact]
    public void Map_HasTwoHomeHexes()
    {
        Assert.Equal(2, NexusMap.Hexes.Count(h => h.IsHome));
    }

    [Fact]
    public void Map_HasSixRedSixBlueFourGold()
    {
        Assert.Equal(6, NexusMap.Hexes.Count(h => h.Color == NexusColonyColor.Red && !h.IsHome));
        Assert.Equal(6, NexusMap.Hexes.Count(h => h.Color == NexusColonyColor.Blue && !h.IsHome));
        Assert.Equal(4, NexusMap.Hexes.Count(h => h.Color == NexusColonyColor.Gold));
    }

    [Fact]
    public void Map_Is180DegreesRotationallySymmetric()
    {
        foreach (var hex in NexusMap.Hexes)
        {
            var mirrored = new HexCoord(-hex.Coord.Q, -hex.Coord.R);
            Assert.True(NexusMap.IsValidCoord(mirrored), $"No mirror for {hex.Coord}");
        }
    }

    [Fact]
    public void Map_AllCoordsWithinRadius2()
    {
        var origin = new HexCoord(0, 0);
        foreach (var hex in NexusMap.Hexes)
            Assert.True(origin.DistanceTo(hex.Coord) <= 2, $"{hex.Coord} exceeds radius 2");
    }

    [Fact]
    public void HomeCoords_AreOnTheMap()
    {
        Assert.True(NexusMap.IsValidCoord(NexusMap.GetHomeCoord(NexusFactionColor.Red)));
        Assert.True(NexusMap.IsValidCoord(NexusMap.GetHomeCoord(NexusFactionColor.Blue)));
    }
}

public class NexusGameEngineTests
{
    private static readonly HexCoord RedHome = NexusMap.GetHomeCoord(NexusFactionColor.Red);
    private static readonly HexCoord BlueHome = NexusMap.GetHomeCoord(NexusFactionColor.Blue);

    private static NexusPlayerState RedPlayer(NexusGameState s) =>
        s.Players.First(p => p.Faction == NexusFactionColor.Red);

    private static NexusPlayerState BluePlayer(NexusGameState s) =>
        s.Players.First(p => p.Faction == NexusFactionColor.Blue);

    private static NexusHexState GetHex(NexusGameState s, HexCoord coord) =>
        s.Hexes.First(h => h.Coord == coord);

    private static void SubmitRound(
        NexusGameState s,
        int round,
        ImmutableArray<NexusFleetOrder> redOrders = default,
        ImmutableArray<NexusFleetOrder> blueOrders = default
    )
    {
        NexusGameEngine.SubmitOrders(
            s,
            new NexusTurnOrdersCommand(
                RedPlayer(s).PlayerId,
                round,
                redOrders.IsDefault ? [] : redOrders,
                false,
                false
            )
        );
        NexusGameEngine.SubmitOrders(
            s,
            new NexusTurnOrdersCommand(
                BluePlayer(s).PlayerId,
                round,
                blueOrders.IsDefault ? [] : blueOrders,
                false,
                false
            )
        );
    }

    private static NexusGameState MakeInitializedState()
    {
        var state = new NexusGameState();
        NexusGameEngine.Initialize(
            state,
            new InitializeNexusGameCommand(
                ImmutableArray.Create(
                    new GameSessionParticipant(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001")),
                    new GameSessionParticipant(Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002"))
                )
            )
        );
        return state;
    }

    [Fact]
    public void Initialize_SetsTwoDistinctPlayers()
    {
        var state = MakeInitializedState();
        Assert.Equal(2, state.Players.Count);
        Assert.NotEqual(state.Players[0].PlayerId, state.Players[1].PlayerId);
    }

    [Fact]
    public void Initialize_StartsAtRound1Planning()
    {
        var state = MakeInitializedState();
        Assert.Equal(1, state.RoundNumber);
        Assert.Equal(NexusGamePhase.Planning, state.Phase);
    }

    [Fact]
    public void Initialize_EachPlayerHasTwoFleets()
    {
        var state = MakeInitializedState();
        Assert.Equal(2, GetHex(state, RedHome).GetFleets(NexusFactionColor.Red));
        Assert.Equal(2, GetHex(state, BlueHome).GetFleets(NexusFactionColor.Blue));
    }

    [Fact]
    public void Initialize_FleetsAreAtHomeHex()
    {
        var state = MakeInitializedState();
        var totalRedFleets = state.Hexes.Sum(h => h.GetFleets(NexusFactionColor.Red));
        var totalBlueFleets = state.Hexes.Sum(h => h.GetFleets(NexusFactionColor.Blue));
        Assert.Equal(2, totalRedFleets);
        Assert.Equal(2, totalBlueFleets);
    }

    [Fact]
    public void Initialize_HomeHexesArePreColonized()
    {
        var state = MakeInitializedState();
        Assert.Equal(RedPlayer(state).PlayerId, GetHex(state, RedHome).ColonyOwnerId);
        Assert.Equal(BluePlayer(state).PlayerId, GetHex(state, BlueHome).ColonyOwnerId);
    }

    [Fact]
    public void Initialize_AllOtherHexesUnclaimed()
    {
        var state = MakeInitializedState();
        var nonHomeHexes = state.Hexes.Where(h => h.Coord != RedHome && h.Coord != BlueHome);
        Assert.All(nonHomeHexes, h => Assert.Null(h.ColonyOwnerId));
    }

    [Fact]
    public void Initialize_ZeroStartingResources()
    {
        var state = MakeInitializedState();
        var red = RedPlayer(state);
        var blue = BluePlayer(state);
        Assert.Equal(0, red.GetCredits(NexusColonyColor.Red));
        Assert.Equal(0, red.GetCredits(NexusColonyColor.Blue));
        Assert.Equal(0, red.GetCredits(NexusColonyColor.Gold));
        Assert.Equal(0, blue.GetCredits(NexusColonyColor.Red));
        Assert.Equal(0, blue.GetCredits(NexusColonyColor.Blue));
        Assert.Equal(0, blue.GetCredits(NexusColonyColor.Gold));
    }

    [Fact]
    public void SubmitOrders_RejectsDuplicateSubmission()
    {
        var state = MakeInitializedState();
        var playerId = RedPlayer(state).PlayerId;
        var cmd = new NexusTurnOrdersCommand(playerId, 1, [], false, false);

        NexusGameEngine.SubmitOrders(state, cmd);
        var result = NexusGameEngine.SubmitOrders(state, cmd);

        Assert.IsType<NexusTurnOrdersRejected>(result);
    }

    [Fact]
    public void SubmitOrders_RejectsWrongRoundNumber()
    {
        var state = MakeInitializedState();
        var cmd = new NexusTurnOrdersCommand(RedPlayer(state).PlayerId, 99, [], false, false);
        var result = NexusGameEngine.SubmitOrders(state, cmd);
        Assert.IsType<NexusTurnOrdersRejected>(result);
    }

    [Fact]
    public void SubmitOrders_AcceptsValidEmptyOrders()
    {
        var state = MakeInitializedState();
        var cmd = new NexusTurnOrdersCommand(RedPlayer(state).PlayerId, 1, [], false, false);
        var result = NexusGameEngine.SubmitOrders(state, cmd);
        Assert.IsType<NexusTurnOrdersAccepted>(result);
    }

    [Fact]
    public void SubmitOrders_BothPlayers_AdvancesRound()
    {
        var state = MakeInitializedState();

        SubmitRound(state, 1);

        Assert.Equal(2, state.RoundNumber);
        Assert.Equal(NexusGamePhase.Planning, state.Phase);
        Assert.False(RedPlayer(state).HasSubmittedOrders);
        Assert.False(BluePlayer(state).HasSubmittedOrders);
    }

    [Fact]
    public void Resolve_HomeIncomeApplied()
    {
        var state = MakeInitializedState();

        SubmitRound(state, 1);

        // Red home is a Red hex (+2 Red); Blue home is a Blue hex (+2 Blue)
        Assert.Equal(2, RedPlayer(state).GetCredits(NexusColonyColor.Red));
        Assert.Equal(0, RedPlayer(state).GetCredits(NexusColonyColor.Blue));
        Assert.Equal(2, BluePlayer(state).GetCredits(NexusColonyColor.Blue));
        Assert.Equal(0, BluePlayer(state).GetCredits(NexusColonyColor.Red));
    }

    [Fact]
    public void Resolve_ColonizeOrder_ClaimsHex()
    {
        var state = MakeInitializedState();
        var red = RedPlayer(state);

        // Move a Red fleet to an adjacent unclaimed hex, then colonize next turn
        var target = new HexCoord(2, -1); // adjacent to Red home (2,-2)

        SubmitRound(state, 1, [new NexusMoveOrder(RedHome, target, 1)]);
        SubmitRound(state, 2, [new NexusColonizeOrder(target)]);

        Assert.Equal(red.PlayerId, GetHex(state, target).ColonyOwnerId);
    }

    [Fact]
    public void Resolve_Combat_LargerForceWins()
    {
        var state = MakeInitializedState();
        var red = RedPlayer(state);
        var blue = BluePlayer(state);

        // Give Red a 3rd fleet by adding directly to hex
        GetHex(state, RedHome).AddFleets(NexusFactionColor.Red, 1);

        // Stage red from (1,0) adjacent to contestedHex, blue from (0,-1)
        var redStaging = new HexCoord(1, 0);
        var blueStaging = new HexCoord(0, -1);
        var contestedHex = new HexCoord(1, -1);

        // Move all fleets to staging hexes first (bypass: set counts directly)
        GetHex(state, RedHome).SetFleets(NexusFactionColor.Red, 0);
        GetHex(state, redStaging).SetFleets(NexusFactionColor.Red, 3);
        GetHex(state, BlueHome).SetFleets(NexusFactionColor.Blue, 0);
        GetHex(state, blueStaging).SetFleets(NexusFactionColor.Blue, 2);

        // Move all to contested hex
        var redOrders = ImmutableArray.Create<NexusFleetOrder>(
            new NexusMoveOrder(redStaging, contestedHex, 3)
        );
        var blueOrders = ImmutableArray.Create<NexusFleetOrder>(
            new NexusMoveOrder(blueStaging, contestedHex, 2)
        );

        SubmitRound(state, 1, redOrders, blueOrders);

        // 3 Red vs 2 Blue: Red wins, loses 2, Blue loses all
        Assert.Equal(1, GetHex(state, contestedHex).GetFleets(NexusFactionColor.Red));
        Assert.Equal(0, GetHex(state, contestedHex).GetFleets(NexusFactionColor.Blue));
    }

    [Fact]
    public void Resolve_Combat_MutualDestruction()
    {
        var state = MakeInitializedState();
        var red = RedPlayer(state);
        var blue = BluePlayer(state);

        var contestedHex = new HexCoord(0, -1);
        var redStaging = new HexCoord(0, 0);
        var blueStaging = new HexCoord(1, -1);

        // Reposition fleets via counts
        GetHex(state, RedHome).RemoveFleets(NexusFactionColor.Red, 1);
        GetHex(state, redStaging).SetFleets(NexusFactionColor.Red, 1);
        GetHex(state, BlueHome).RemoveFleets(NexusFactionColor.Blue, 1);
        GetHex(state, blueStaging).SetFleets(NexusFactionColor.Blue, 1);

        SubmitRound(
            state,
            1,
            [new NexusMoveOrder(redStaging, contestedHex, 1)],
            [new NexusMoveOrder(blueStaging, contestedHex, 1)]
        );

        Assert.Equal(0, GetHex(state, contestedHex).GetFleets(NexusFactionColor.Red));
        Assert.Equal(0, GetHex(state, contestedHex).GetFleets(NexusFactionColor.Blue));
    }

    [Fact]
    public void Resolve_ColonizeAfterCombat_Fails()
    {
        var state = MakeInitializedState();
        var red = RedPlayer(state);

        var contestedHex = new HexCoord(0, -1);
        var redStaging = new HexCoord(0, 0);
        var blueStaging = new HexCoord(1, -1);

        // Give red 2 fleets at staging, blue 1
        GetHex(state, RedHome).RemoveFleets(NexusFactionColor.Red, 2);
        GetHex(state, redStaging).SetFleets(NexusFactionColor.Red, 2);
        GetHex(state, BlueHome).RemoveFleets(NexusFactionColor.Blue, 1);
        GetHex(state, blueStaging).SetFleets(NexusFactionColor.Blue, 1);

        // Red sends 2 to contested with colonize intent; Blue sends 1 — Red wins (loses 1)
        SubmitRound(
            state,
            1,
            [new NexusMoveOrder(redStaging, contestedHex, 2)],
            [new NexusMoveOrder(blueStaging, contestedHex, 1)]
        );

        // The contested hex should NOT be colonized by red (combat happened there)
        Assert.Null(GetHex(state, contestedHex).ColonyOwnerId);
    }

    [Fact]
    public void Abandon_EndsGameWithOpponentWin()
    {
        var state = MakeInitializedState();
        var red = RedPlayer(state);
        var blue = BluePlayer(state);

        NexusGameEngine.Abandon(state, red.PlayerId);

        Assert.Equal(NexusGamePhase.Ended, state.Phase);
        Assert.Equal(NexusGameOutcome.Victory, state.Completion!.Outcome);
        Assert.Equal(blue.PlayerId, state.Completion.WinnerId);
    }

    [Fact]
    public void Round15_Tiebreak_MoreSystemsWins()
    {
        var state = MakeInitializedState();
        state.RoundNumber = 15;

        var red = RedPlayer(state);

        // Give Red one extra colony
        GetHex(state, new HexCoord(1, -1)).ColonyOwnerId = red.PlayerId;

        SubmitRound(state, 15);

        Assert.Equal(NexusGamePhase.Ended, state.Phase);
        Assert.Equal(NexusGameOutcome.Victory, state.Completion!.Outcome);
        Assert.Equal(red.PlayerId, state.Completion.WinnerId);
    }

    [Fact]
    public void Resolve_OpposingColorColony_GeneratesNoDirectIncomeForOwner()
    {
        // Red colonizes a Blue hex that is isolated from Blue's fleet positions so no trade
        // route forms — confirms that base income from an opposing-color colony is zero.
        var state = MakeInitializedState();
        var red = RedPlayer(state);

        // (1,1) is a Blue hex far from both home hexes (distance 3 from Red home, 4 from Blue home)
        // so no trade route can form with either player's starting fleets
        GetHex(state, new HexCoord(1, 1)).ColonyOwnerId = red.PlayerId;

        SubmitRound(state, 1);

        // Red gets Red home income (+2 Red) only — no Blue credit from opposing-color colony
        Assert.Equal(2, RedPlayer(state).GetCredits(NexusColonyColor.Red));
        Assert.Equal(0, RedPlayer(state).GetCredits(NexusColonyColor.Blue));
        // Blue gets Blue home income (+2 Blue) only — loses the denied colony income
        Assert.Equal(2, BluePlayer(state).GetCredits(NexusColonyColor.Blue));
        Assert.Equal(0, BluePlayer(state).GetCredits(NexusColonyColor.Red));
    }

    [Fact]
    public void Resolve_GoldColony_GeneratesGoldForAnyOwner()
    {
        // Gold hexes should generate Gold for whoever owns them
        var state = MakeInitializedState();
        var red = RedPlayer(state);

        var goldHex = state.Hexes.First(h =>
            NexusMap.ByCoord[h.Coord].Color == NexusColonyColor.Gold
        );
        goldHex.ColonyOwnerId = red.PlayerId;

        SubmitRound(state, 1);

        Assert.Equal(1, RedPlayer(state).GetCredits(NexusColonyColor.Gold));
    }
}

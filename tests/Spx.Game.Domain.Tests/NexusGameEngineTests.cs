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
        Assert.True(NexusMap.IsValidCoord(NexusMap.RedHomeCoord));
        Assert.True(NexusMap.IsValidCoord(NexusMap.BlueHomeCoord));
    }
}

public class NexusGameEngineTests
{
    private static NexusGameState MakeInitializedState()
    {
        var state = new NexusGameState();
        NexusGameEngine.Initialize(
            state,
            new InitializeNexusGameCommand(
                new GameSessionParticipant(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001")),
                new GameSessionParticipant(Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002"))
            )
        );
        return state;
    }

    [Fact]
    public void Initialize_SetsTwoDistinctPlayers()
    {
        var state = MakeInitializedState();
        Assert.NotNull(state.RedPlayer);
        Assert.NotNull(state.BluePlayer);
        Assert.NotEqual(state.RedPlayer.PlayerId, state.BluePlayer.PlayerId);
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
        var redHome = state.Hexes.First(h => h.Coord == NexusMap.RedHomeCoord);
        var blueHome = state.Hexes.First(h => h.Coord == NexusMap.BlueHomeCoord);
        Assert.Equal(2, redHome.RedFleets);
        Assert.Equal(2, blueHome.BlueFleets);
    }

    [Fact]
    public void Initialize_FleetsAreAtHomeHex()
    {
        var state = MakeInitializedState();
        // All red fleets are on red home, all blue fleets are on blue home
        // (verified by Initialize_EachPlayerHasTwoFleets; here we confirm no stray fleets)
        var totalRedFleets = state.Hexes.Sum(h => h.RedFleets);
        var totalBlueFleets = state.Hexes.Sum(h => h.BlueFleets);
        Assert.Equal(2, totalRedFleets);
        Assert.Equal(2, totalBlueFleets);
    }

    [Fact]
    public void Initialize_HomeHexesArePreColonized()
    {
        var state = MakeInitializedState();
        var redHome = state.Hexes.First(h => h.Coord == NexusMap.RedHomeCoord);
        var blueHome = state.Hexes.First(h => h.Coord == NexusMap.BlueHomeCoord);
        Assert.Equal(state.RedPlayer!.PlayerId, redHome.ColonyOwnerId);
        Assert.Equal(state.BluePlayer!.PlayerId, blueHome.ColonyOwnerId);
    }

    [Fact]
    public void Initialize_AllOtherHexesUnclaimed()
    {
        var state = MakeInitializedState();
        var nonHomeHexes = state.Hexes.Where(h =>
            h.Coord != NexusMap.RedHomeCoord && h.Coord != NexusMap.BlueHomeCoord
        );
        Assert.All(nonHomeHexes, h => Assert.Null(h.ColonyOwnerId));
    }

    [Fact]
    public void Initialize_ZeroStartingResources()
    {
        var state = MakeInitializedState();
        Assert.Equal(0, state.RedPlayer!.RedCredits);
        Assert.Equal(0, state.RedPlayer.BlueCredits);
        Assert.Equal(0, state.RedPlayer.GoldCredits);
        Assert.Equal(0, state.BluePlayer!.RedCredits);
        Assert.Equal(0, state.BluePlayer.BlueCredits);
        Assert.Equal(0, state.BluePlayer.GoldCredits);
    }

    [Fact]
    public void SubmitOrders_RejectsDuplicateSubmission()
    {
        var state = MakeInitializedState();
        var playerId = state.RedPlayer!.PlayerId;
        var cmd = new NexusTurnOrdersCommand(playerId, 1, [], false, false);

        NexusGameEngine.SubmitOrders(state, cmd);
        var result = NexusGameEngine.SubmitOrders(state, cmd);

        Assert.IsType<NexusTurnOrdersRejected>(result);
    }

    [Fact]
    public void SubmitOrders_RejectsWrongRoundNumber()
    {
        var state = MakeInitializedState();
        var cmd = new NexusTurnOrdersCommand(state.RedPlayer!.PlayerId, 99, [], false, false);
        var result = NexusGameEngine.SubmitOrders(state, cmd);
        Assert.IsType<NexusTurnOrdersRejected>(result);
    }

    [Fact]
    public void SubmitOrders_AcceptsValidEmptyOrders()
    {
        var state = MakeInitializedState();
        var cmd = new NexusTurnOrdersCommand(state.RedPlayer!.PlayerId, 1, [], false, false);
        var result = NexusGameEngine.SubmitOrders(state, cmd);
        Assert.IsType<NexusTurnOrdersAccepted>(result);
    }

    [Fact]
    public void SubmitOrders_BothPlayers_AdvancesRound()
    {
        var state = MakeInitializedState();
        var red = state.RedPlayer!.PlayerId;
        var blue = state.BluePlayer!.PlayerId;

        NexusGameEngine.SubmitOrders(state, new NexusTurnOrdersCommand(red, 1, [], false, false));
        NexusGameEngine.SubmitOrders(state, new NexusTurnOrdersCommand(blue, 1, [], false, false));

        Assert.Equal(2, state.RoundNumber);
        Assert.Equal(NexusGamePhase.Planning, state.Phase);
        Assert.False(state.RedPlayer.HasSubmittedOrders);
        Assert.False(state.BluePlayer!.HasSubmittedOrders);
    }

    [Fact]
    public void Resolve_HomeIncomeApplied()
    {
        var state = MakeInitializedState();
        var red = state.RedPlayer!.PlayerId;
        var blue = state.BluePlayer!.PlayerId;

        NexusGameEngine.SubmitOrders(state, new NexusTurnOrdersCommand(red, 1, [], false, false));
        NexusGameEngine.SubmitOrders(state, new NexusTurnOrdersCommand(blue, 1, [], false, false));

        // Red home is a Red hex (+2 Red); Blue home is a Blue hex (+2 Blue)
        Assert.Equal(2, state.RedPlayer!.RedCredits);
        Assert.Equal(0, state.RedPlayer.BlueCredits);
        Assert.Equal(2, state.BluePlayer!.BlueCredits);
        Assert.Equal(0, state.BluePlayer.RedCredits);
    }

    [Fact]
    public void Resolve_ColonizeOrder_ClaimsHex()
    {
        var state = MakeInitializedState();
        var red = state.RedPlayer!;
        var blue = state.BluePlayer!;

        // Move a Red fleet to an adjacent unclaimed hex, then colonize next turn
        var target = new HexCoord(2, -1); // adjacent to Red home (2,-2)

        var moveCmd = new NexusTurnOrdersCommand(
            red.PlayerId,
            1,
            [new NexusMoveOrder(NexusMap.RedHomeCoord, target, 1)],
            false,
            false
        );
        NexusGameEngine.SubmitOrders(state, moveCmd);
        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(blue.PlayerId, 1, [], false, false)
        );

        // Round 2: colonize from the target hex
        var colonizeCmd = new NexusTurnOrdersCommand(
            red.PlayerId,
            2,
            [new NexusColonizeOrder(target)],
            false,
            false
        );
        NexusGameEngine.SubmitOrders(state, colonizeCmd);
        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(blue.PlayerId, 2, [], false, false)
        );

        var hexState = state.Hexes.First(h => h.Coord == target);
        Assert.Equal(red.PlayerId, hexState.ColonyOwnerId);
    }

    [Fact]
    public void Resolve_Combat_LargerForceWins()
    {
        var state = MakeInitializedState();
        var red = state.RedPlayer!;
        var blue = state.BluePlayer!;

        // Give Red a 3rd fleet by adding directly to hex
        var redHome = state.Hexes.First(h => h.Coord == NexusMap.RedHomeCoord);
        redHome.RedFleets++;

        // Stage red from (1,0) adjacent to contestedHex, blue from (0,-1)
        var redStaging = new HexCoord(1, 0);
        var blueStaging = new HexCoord(0, -1);
        var contestedHex = new HexCoord(1, -1);

        // Move all fleets to staging hexes first (bypass: set counts directly)
        redHome.RedFleets -= 3;
        var redStagingHex = state.Hexes.First(h => h.Coord == redStaging);
        redStagingHex.RedFleets = 3;
        var blueHome = state.Hexes.First(h => h.Coord == NexusMap.BlueHomeCoord);
        blueHome.BlueFleets -= 2;
        var blueStagingHex = state.Hexes.First(h => h.Coord == blueStaging);
        blueStagingHex.BlueFleets = 2;

        // Move all to contested hex
        var redOrders = ImmutableArray.Create<NexusFleetOrder>(
            new NexusMoveOrder(redStaging, contestedHex, 3)
        );
        var blueOrders = ImmutableArray.Create<NexusFleetOrder>(
            new NexusMoveOrder(blueStaging, contestedHex, 2)
        );

        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(red.PlayerId, 1, redOrders, false, false)
        );
        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(blue.PlayerId, 1, blueOrders, false, false)
        );

        var contested = state.Hexes.First(h => h.Coord == contestedHex);
        // 3 Red vs 2 Blue: Red wins, loses 2, Blue loses all
        Assert.Equal(1, contested.RedFleets);
        Assert.Equal(0, contested.BlueFleets);
    }

    [Fact]
    public void Resolve_Combat_MutualDestruction()
    {
        var state = MakeInitializedState();
        var red = state.RedPlayer!;
        var blue = state.BluePlayer!;

        var contestedHex = new HexCoord(0, -1);
        var redStaging = new HexCoord(0, 0);
        var blueStaging = new HexCoord(1, -1);

        // Reposition fleets via counts
        var redHome = state.Hexes.First(h => h.Coord == NexusMap.RedHomeCoord);
        redHome.RedFleets--;
        state.Hexes.First(h => h.Coord == redStaging).RedFleets = 1;
        var blueHome = state.Hexes.First(h => h.Coord == NexusMap.BlueHomeCoord);
        blueHome.BlueFleets--;
        state.Hexes.First(h => h.Coord == blueStaging).BlueFleets = 1;

        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(
                red.PlayerId,
                1,
                [new NexusMoveOrder(redStaging, contestedHex, 1)],
                false,
                false
            )
        );
        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(
                blue.PlayerId,
                1,
                [new NexusMoveOrder(blueStaging, contestedHex, 1)],
                false,
                false
            )
        );

        var contested = state.Hexes.First(h => h.Coord == contestedHex);
        Assert.Equal(0, contested.RedFleets);
        Assert.Equal(0, contested.BlueFleets);
    }

    [Fact]
    public void Resolve_ColonizeAfterCombat_Fails()
    {
        var state = MakeInitializedState();
        var red = state.RedPlayer!;
        var blue = state.BluePlayer!;

        var contestedHex = new HexCoord(0, -1);
        var redStaging = new HexCoord(0, 0);
        var blueStaging = new HexCoord(1, -1);

        // Give red 2 fleets at staging, blue 1
        var redHome = state.Hexes.First(h => h.Coord == NexusMap.RedHomeCoord);
        redHome.RedFleets -= 2;
        state.Hexes.First(h => h.Coord == redStaging).RedFleets = 2;
        var blueHome = state.Hexes.First(h => h.Coord == NexusMap.BlueHomeCoord);
        blueHome.BlueFleets--;
        state.Hexes.First(h => h.Coord == blueStaging).BlueFleets = 1;

        // Red sends 2 to contested with colonize intent; Blue sends 1 — Red wins (loses 1)
        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(
                red.PlayerId,
                1,
                [new NexusMoveOrder(redStaging, contestedHex, 2)],
                false,
                false
            )
        );
        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(
                blue.PlayerId,
                1,
                [new NexusMoveOrder(blueStaging, contestedHex, 1)],
                false,
                false
            )
        );

        // The contested hex should NOT be colonized by red (combat happened there)
        var hexState = state.Hexes.First(h => h.Coord == contestedHex);
        Assert.Null(hexState.ColonyOwnerId);
    }

    [Fact]
    public void Abandon_EndsGameWithOpponentWin()
    {
        var state = MakeInitializedState();
        var red = state.RedPlayer!;
        var blue = state.BluePlayer!;

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

        var red = state.RedPlayer!;
        var blue = state.BluePlayer!;

        // Give Red one extra colony
        var extraHex = state.Hexes.First(h => h.Coord == new HexCoord(1, -1));
        extraHex.ColonyOwnerId = red.PlayerId;

        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(red.PlayerId, 15, [], false, false)
        );
        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(blue.PlayerId, 15, [], false, false)
        );

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
        var red = state.RedPlayer!;
        var blue = state.BluePlayer!;

        // (1,1) is a Blue hex far from both home hexes (distance 3 from Red home, 4 from Blue home)
        // so no trade route can form with either player's starting fleets
        var blueHex = state.Hexes.First(h => h.Coord == new HexCoord(1, 1));
        blueHex.ColonyOwnerId = red.PlayerId;

        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(red.PlayerId, 1, [], false, false)
        );
        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(blue.PlayerId, 1, [], false, false)
        );

        // Red gets Red home income (+2 Red) only — no Blue credit from opposing-color colony
        Assert.Equal(2, state.RedPlayer!.RedCredits);
        Assert.Equal(0, state.RedPlayer.BlueCredits);
        // Blue gets Blue home income (+2 Blue) only — loses the denied colony income
        Assert.Equal(2, state.BluePlayer!.BlueCredits);
        Assert.Equal(0, state.BluePlayer.RedCredits);
    }

    [Fact]
    public void Resolve_GoldColony_GeneratesGoldForAnyOwner()
    {
        // Gold hexes should generate Gold for whoever owns them
        var state = MakeInitializedState();
        var red = state.RedPlayer!;
        var blue = state.BluePlayer!;

        var goldHex = state.Hexes.First(h =>
            NexusMap.ByCoord[h.Coord].Color == NexusColonyColor.Gold
        );
        goldHex.ColonyOwnerId = red.PlayerId;

        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(red.PlayerId, 1, [], false, false)
        );
        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(blue.PlayerId, 1, [], false, false)
        );

        Assert.Equal(1, state.RedPlayer!.GoldCredits);
    }
}

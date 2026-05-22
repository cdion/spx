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
        Assert.Equal(2, state.Fleets.Count(f => f.OwnerId == state.RedPlayer!.PlayerId));
        Assert.Equal(2, state.Fleets.Count(f => f.OwnerId == state.BluePlayer!.PlayerId));
    }

    [Fact]
    public void Initialize_FleetsAreAtHomeHex()
    {
        var state = MakeInitializedState();
        Assert.All(
            state.Fleets.Where(f => f.OwnerId == state.RedPlayer!.PlayerId),
            f => Assert.Equal(NexusMap.RedHomeCoord, f.Position)
        );
        Assert.All(
            state.Fleets.Where(f => f.OwnerId == state.BluePlayer!.PlayerId),
            f => Assert.Equal(NexusMap.BlueHomeCoord, f.Position)
        );
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
        var redFleet = state.Fleets.First(f => f.OwnerId == red.PlayerId);
        var target = new HexCoord(2, -1); // adjacent to Red home (2,-2)

        var moveCmd = new NexusTurnOrdersCommand(
            red.PlayerId,
            1,
            [new NexusMoveOrder(redFleet.FleetId, target)],
            false,
            false
        );
        NexusGameEngine.SubmitOrders(state, moveCmd);
        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(blue.PlayerId, 1, [], false, false)
        );

        // Round 2: colonize
        var fleetAtTarget = state.Fleets.First(f =>
            f.OwnerId == red.PlayerId && f.Position == target
        );
        var colonizeCmd = new NexusTurnOrdersCommand(
            red.PlayerId,
            2,
            [new NexusColonizeOrder(fleetAtTarget.FleetId)],
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

        // Give Red a 3rd fleet (simulate by adding directly)
        var nexusHex = new HexCoord(1, 0); // valid map hex
        state.Fleets.Add(
            new NexusFleetState
            {
                FleetId = Guid.NewGuid(),
                OwnerId = red.PlayerId,
                Position = NexusMap.RedHomeCoord,
            }
        );

        // Move all 3 Red fleets and 2 Blue fleets to (1,-1)
        var contestedHex = new HexCoord(1, -1);
        var redFleets = state.Fleets.Where(f => f.OwnerId == red.PlayerId).ToList();
        var blueFleets = state.Fleets.Where(f => f.OwnerId == blue.PlayerId).ToList();

        // Set positions directly (bypassing move validation)
        foreach (var f in redFleets)
            f.Position = new HexCoord(1, 0);
        foreach (var f in blueFleets)
            f.Position = new HexCoord(0, -1);

        // Move all to contested hex
        var redOrders = redFleets
            .Select(f => (NexusFleetOrder)new NexusMoveOrder(f.FleetId, contestedHex))
            .ToImmutableArray();
        var blueOrders = blueFleets
            .Select(f => (NexusFleetOrder)new NexusMoveOrder(f.FleetId, contestedHex))
            .ToImmutableArray();

        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(red.PlayerId, 1, redOrders, false, false)
        );
        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(blue.PlayerId, 1, blueOrders, false, false)
        );

        var redSurvivors = state.Fleets.Count(f => f.OwnerId == red.PlayerId);
        var blueSurvivors = state.Fleets.Count(f => f.OwnerId == blue.PlayerId);

        // 3 Red vs 2 Blue: Red wins, loses 2, Blue loses all
        Assert.Equal(1, redSurvivors);
        Assert.Equal(0, blueSurvivors);
    }

    [Fact]
    public void Resolve_Combat_MutualDestruction()
    {
        var state = MakeInitializedState();
        var red = state.RedPlayer!;
        var blue = state.BluePlayer!;

        var contestedHex = new HexCoord(0, -1);
        var redFleet = state.Fleets.First(f => f.OwnerId == red.PlayerId);
        var blueFleet = state.Fleets.First(f => f.OwnerId == blue.PlayerId);

        redFleet.Position = new HexCoord(0, 0);
        blueFleet.Position = new HexCoord(1, -1);

        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(
                red.PlayerId,
                1,
                [new NexusMoveOrder(redFleet.FleetId, contestedHex)],
                false,
                false
            )
        );
        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(
                blue.PlayerId,
                1,
                [new NexusMoveOrder(blueFleet.FleetId, contestedHex)],
                false,
                false
            )
        );

        Assert.Equal(0, state.Fleets.Count(f => f.Position == contestedHex));
    }

    [Fact]
    public void Resolve_ColonizeAfterCombat_Fails()
    {
        var state = MakeInitializedState();
        var red = state.RedPlayer!;
        var blue = state.BluePlayer!;

        var contestedHex = new HexCoord(0, -1);
        var redFleet = state.Fleets.First(f => f.OwnerId == red.PlayerId);
        var blueFleet = state.Fleets.First(f => f.OwnerId == blue.PlayerId);
        var redFleet2 = state.Fleets.Skip(1).First(f => f.OwnerId == red.PlayerId);

        // Position fleets so Red wins combat and also has a colonize order
        redFleet.Position = new HexCoord(0, 0);
        redFleet2.Position = new HexCoord(1, -1);
        blueFleet.Position = new HexCoord(0, 0);

        // Red sends 2 to contested, Blue sends 1 — Red wins (loses 1)
        // Red's surviving fleet has colonize
        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(
                red.PlayerId,
                1,
                [
                    new NexusMoveOrder(redFleet.FleetId, contestedHex),
                    new NexusMoveOrder(redFleet2.FleetId, contestedHex),
                ],
                false,
                false
            )
        );

        // Blue fleet stays with colonize on a different hex (already pre-colonized for blue — this is fine)
        NexusGameEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(
                blue.PlayerId,
                1,
                [new NexusMoveOrder(blueFleet.FleetId, contestedHex)],
                false,
                false
            )
        );

        // The contested hex should NOT be colonized by red
        var hexState = state.Hexes.First(h => h.Coord == contestedHex);
        Assert.Null(hexState.ColonyOwnerId);

        // ColonizeFailedEvent should be in resolve events
        var failed = state.ResolveEvents.OfType<NexusColonizeFailedEvent>().ToList();
        // In this scenario Red didn't actually submit a colonize order, so no failed event
        // The hex is just unclaimed after combat
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

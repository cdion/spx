namespace Spx.Nexus.Domain.Tests;

// ── HexCoord ─────────────────────────────────────────────────────────────────

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
        Assert.Equal(1, new HexCoord(q1, r1).DistanceTo(new HexCoord(q2, r2)));
    }

    [Fact]
    public void GetNeighbours_AllAtDistanceOne()
    {
        var center = new HexCoord(0, 0);
        Assert.All(center.GetNeighbours(), n => Assert.Equal(1, center.DistanceTo(n)));
    }
}

// ── Map Generation ────────────────────────────────────────────────────────────

public class NexusMapTests
{
    private static readonly Guid P1 = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid P2 = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly List<NexusSystemState> Map = NexusMap.GenerateMap(
        P1,
        P2,
        new Random(42)
    );

    [Fact]
    public void GenerateMap_Has19Systems() => Assert.Equal(19, Map.Count);

    [Fact]
    public void GenerateMap_HasExactlyOneNexus() => Assert.Single(Map, s => s.IsNexus);

    [Fact]
    public void GenerateMap_NexusAtCenter() =>
        Assert.Equal(NexusMap.NexusCoord, Map.Single(s => s.IsNexus).Coord);

    [Fact]
    public void GenerateMap_HasTwoHomeSystems() =>
        Assert.Equal(2, Map.Count(s => s.HomePlayerId.HasValue));

    [Fact]
    public void GenerateMap_HomeSystemsPreControlled() =>
        Assert.All(
            Map.Where(s => s.HomePlayerId.HasValue),
            s => Assert.Equal(s.HomePlayerId, s.ControlOwner)
        );

    [Fact]
    public void GenerateMap_IncomeSystemsHaveValuesInRange()
    {
        var incomeSystems = Map.Where(s => !s.IsNexus && !s.HomePlayerId.HasValue).ToList();
        Assert.Equal(16, incomeSystems.Count);
        Assert.All(incomeSystems, s => Assert.InRange(s.IncomeValue, 1, 3));
    }

    [Fact]
    public void GenerateMap_HomeSystemsHave2Income() =>
        Assert.All(Map.Where(s => s.HomePlayerId.HasValue), s => Assert.Equal(2, s.IncomeValue));

    [Fact]
    public void GenerateMap_NexusHasZeroIncome() =>
        Assert.Equal(0, Map.Single(s => s.IsNexus).IncomeValue);

    [Fact]
    public void GenerateMap_HomeSystemsHaveStartingUnits()
    {
        var p1Home = Map.Single(s => s.HomePlayerId == P1);
        var p2Home = Map.Single(s => s.HomePlayerId == P2);

        Assert.Equal(1, p1Home.GetUnitCount(P1, NexusUnitType.Carrier));
        Assert.Equal(4, p1Home.GetUnitCount(P1, NexusUnitType.Infantry));
        Assert.Equal(2, p1Home.GetUnitCount(P1, NexusUnitType.Fighter));
        Assert.Equal(1, p2Home.GetUnitCount(P2, NexusUnitType.Carrier));
        Assert.Equal(4, p2Home.GetUnitCount(P2, NexusUnitType.Infantry));
        Assert.Equal(2, p2Home.GetUnitCount(P2, NexusUnitType.Fighter));
    }

    [Fact]
    public void Map_AllCoordsWithinRadius2()
    {
        var origin = new HexCoord(0, 0);
        Assert.All(Map, s => Assert.True(origin.DistanceTo(s.Coord) <= 2));
    }

    [Fact]
    public void Map_Is180DegreesRotationallySymmetric()
    {
        foreach (var system in Map)
        {
            var mirrored = new HexCoord(-system.Coord.Q, -system.Coord.R);
            Assert.True(NexusMap.IsValidCoord(mirrored), $"No mirror for {system.Coord}");
        }
    }

    [Fact]
    public void IsValidCoord_ReturnsTrueForNexus() =>
        Assert.True(NexusMap.IsValidCoord(NexusMap.NexusCoord));

    [Fact]
    public void AreAdjacent_ReturnsTrueForNeighbours() =>
        Assert.True(NexusMap.AreAdjacent(new HexCoord(0, 0), new HexCoord(1, 0)));

    [Fact]
    public void AreAdjacent_ReturnsFalseForDistance2() =>
        Assert.False(NexusMap.AreAdjacent(new HexCoord(0, 0), new HexCoord(2, 0)));
}

// ── NexusCombatSpec ───────────────────────────────────────────────────────────

public class NexusCombatSpecTests
{
    [Fact]
    public void P1_Interceptor_Hits_Bomber_At2Plus() =>
        Assert.Equal(
            2,
            NexusCombatSpec.GetHitThreshold(NexusUnitType.Interceptor, 1, NexusUnitType.Bomber)
        );

    [Fact]
    public void P1_Fighter_Hits_AllStrike_At4Plus()
    {
        Assert.Equal(
            4,
            NexusCombatSpec.GetHitThreshold(NexusUnitType.Fighter, 1, NexusUnitType.Interceptor)
        );
        Assert.Equal(
            4,
            NexusCombatSpec.GetHitThreshold(NexusUnitType.Fighter, 1, NexusUnitType.Fighter)
        );
        Assert.Equal(
            4,
            NexusCombatSpec.GetHitThreshold(NexusUnitType.Fighter, 1, NexusUnitType.Bomber)
        );
    }

    [Fact]
    public void P2_Cruiser_Hits_Ships_At3Plus() =>
        Assert.Equal(
            3,
            NexusCombatSpec.GetHitThreshold(NexusUnitType.Cruiser, 2, NexusUnitType.Frigate)
        );

    [Fact]
    public void P4_Armor_Hits_Infantry_At3Plus() =>
        Assert.Equal(
            3,
            NexusCombatSpec.GetHitThreshold(NexusUnitType.Armor, 4, NexusUnitType.Infantry)
        );

    [Fact]
    public void Planetary_NotTargetableInP1() =>
        Assert.False(NexusCombatSpec.IsTargetable(NexusUnitType.Infantry, 1));

    [Fact]
    public void Destroyer_NotTargetableInP1() =>
        Assert.False(NexusCombatSpec.IsTargetable(NexusUnitType.Destroyer, 1));

    [Fact]
    public void Ships_NotTargetableInP4() =>
        Assert.False(NexusCombatSpec.IsTargetable(NexusUnitType.Cruiser, 4));

    [Fact]
    public void Infantry_CanAttackInP4() =>
        Assert.True(NexusCombatSpec.CanAttack(NexusUnitType.Infantry, 4));

    [Fact]
    public void Carrier_CannotAttackInP1() =>
        Assert.False(NexusCombatSpec.CanAttack(NexusUnitType.Carrier, 1));
}

// ── Engine — Initialize ───────────────────────────────────────────────────────

public class NexusGameEngineInitTests
{
    private static readonly Guid P1Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid P2Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

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
        return state;
    }

    [Fact]
    public void Initialize_SetsTwoDistinctPlayers()
    {
        var s = MakeState();
        Assert.Equal(2, s.Players.Count);
        Assert.NotEqual(s.Players[0].PlayerId, s.Players[1].PlayerId);
    }

    [Fact]
    public void Initialize_StartsAtRound1Planning()
    {
        var s = MakeState();
        Assert.Equal(1, s.RoundNumber);
        Assert.Null(s.Completion);
    }

    [Fact]
    public void Initialize_PlayersStartWithZeroEnergy() =>
        Assert.All(MakeState().Players, p => Assert.Equal(0, p.Energy));

    [Fact]
    public void Initialize_Has19Systems() => Assert.Equal(19, MakeState().Systems.Count);

    [Fact]
    public void Initialize_HomeSystemsHaveStartingUnits()
    {
        var s = MakeState();
        var p1Home = s.Systems.Single(sys => sys.HomePlayerId == P1Id);

        Assert.Equal(1, p1Home.GetUnitCount(P1Id, NexusUnitType.Carrier));
        Assert.Equal(4, p1Home.GetUnitCount(P1Id, NexusUnitType.Infantry));
        Assert.Equal(2, p1Home.GetUnitCount(P1Id, NexusUnitType.Fighter));
    }

    [Fact]
    public void Initialize_HomeSystemsPreControlled()
    {
        var s = MakeState();
        Assert.All(
            s.Systems.Where(sys => sys.HomePlayerId.HasValue),
            sys => Assert.Equal(sys.HomePlayerId, sys.ControlOwner)
        );
    }

    [Fact]
    public void Initialize_NexusIsUncontrolled()
    {
        var nexus = MakeState().Systems.Single(s => s.IsNexus);
        Assert.Null(nexus.ControlOwner);
    }

    [Fact]
    public void Initialize_RequiresExactlyTwoPlayers()
    {
        var state = new NexusState();
        Assert.Throws<InvalidOperationException>(() =>
            NexusEngine.Initialize(
                state,
                new InitializeNexusGameCommand(ImmutableArray.Create(new NexusSessionPlayer(P1Id))),
                new Random(0)
            )
        );
    }
}

// ── Engine — Move Validation ──────────────────────────────────────────────────

public class NexusMoveValidationTests
{
    private static readonly Guid P1Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid P2Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    // P1 home is Player1HomeCoord = (2,-2); valid adjacent target is (1,-2) or (2,-1)
    private static readonly HexCoord P1Home = NexusMap.Player1HomeCoord;
    private static readonly HexCoord P2Home = NexusMap.Player2HomeCoord;
    private static readonly HexCoord Adjacent1 = new(1, -2);
    private static readonly HexCoord Adjacent2 = new(2, -1);
    private static readonly HexCoord P2Adjacent = new(-1, 2);

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

    private static NexusMoveOrder Move(
        HexCoord from,
        HexCoord to,
        params (NexusUnitType, int)[] units
    ) => new(from, to, units.ToImmutableDictionary(x => x.Item1, x => x.Item2));

    [Fact]
    public void Move_ToNonAdjacentSystem_IsRejected()
    {
        var s = MakeState();
        // (2,-2) to (0,-2) is distance 2, not adjacent
        var result = Submit(
            s,
            P1Id,
            [Move(P1Home, new HexCoord(0, -2), (NexusUnitType.Carrier, 1))]
        );
        Assert.IsType<NexusTurnOrdersRejected>(result);
    }

    [Fact]
    public void Move_WithNoUnits_IsRejected()
    {
        var s = MakeState();
        var order = new NexusMoveOrder(
            P1Home,
            Adjacent1,
            ImmutableDictionary<NexusUnitType, int>.Empty
        );
        var result = Submit(s, P1Id, [order]);
        Assert.IsType<NexusTurnOrdersRejected>(result);
    }

    [Fact]
    public void Move_MoreUnitsThanAvailable_IsRejected()
    {
        var s = MakeState();
        // P1 home has 4 Infantry; trying to move 5
        var result = Submit(s, P1Id, [Move(P1Home, Adjacent1, (NexusUnitType.Infantry, 5))]);
        Assert.IsType<NexusTurnOrdersRejected>(result);
    }

    [Fact]
    public void Move_MoreUnitsThanAvailable_ErrorIncludesSectorName()
    {
        var s = MakeState();

        var result = Submit(s, P1Id, [Move(P1Home, Adjacent1, (NexusUnitType.Infantry, 5))]);

        var rejected = Assert.IsType<NexusTurnOrdersRejected>(result);
        Assert.Equal(
            "Insufficient Infantry at Your Home System: need 5, have 4.",
            rejected.ErrorMessage
        );
    }

    [Fact]
    public void Move_FromOpponentHomeSystem_ErrorUsesOpponentHomeSystemName()
    {
        var s = MakeState();

        var result = Submit(s, P1Id, [Move(P2Home, P2Adjacent, (NexusUnitType.Carrier, 1))]);

        var rejected = Assert.IsType<NexusTurnOrdersRejected>(result);
        Assert.Equal(
            "Insufficient Fleet Capacity at Opponent Home System: need 8, have 0.",
            rejected.ErrorMessage
        );
    }

    [Fact]
    public void Move_StrikeWithNoCarrier_IsRejected()
    {
        var s = MakeState();
        // Moving 3 Fighters (need 3 capacity) but only providing 0 ship capacity
        // Remove carrier first so only fighters remain
        s.Systems.First(sys => sys.HomePlayerId == P1Id)
            .RemoveUnits(P1Id, NexusUnitType.Carrier, 1);
        s.Systems.First(sys => sys.HomePlayerId == P1Id).AddUnits(P1Id, NexusUnitType.Frigate, 0); // no extra ships
        var result = Submit(s, P1Id, [Move(P1Home, Adjacent1, (NexusUnitType.Fighter, 1))]);

        var rejected = Assert.IsType<NexusTurnOrdersRejected>(result);
        Assert.Equal(
            "Insufficient Fleet Capacity for move from Your Home System to Pi: need 1, have 0.",
            rejected.ErrorMessage
        );
    }

    [Fact]
    public void Move_CarrierWithStrike_IsAccepted()
    {
        var s = MakeState();
        // Carrier (cap 8) + 1 Fighter (consumes 1) → accepted
        var result = Submit(
            s,
            P1Id,
            [Move(P1Home, Adjacent1, (NexusUnitType.Carrier, 1), (NexusUnitType.Fighter, 1))]
        );
        Assert.IsType<NexusTurnOrdersAccepted>(result);
    }

    [Fact]
    public void Move_ShipOnlyMove_IsAccepted()
    {
        var s = MakeState();
        // Carrier alone (no strike/planetary) needs 0 capacity — always accepted
        var result = Submit(s, P1Id, [Move(P1Home, Adjacent1, (NexusUnitType.Carrier, 1))]);
        Assert.IsType<NexusTurnOrdersAccepted>(result);
    }

    [Fact]
    public void Move_MultipleOrdersFromSameSystem_AggregatesUnits()
    {
        var s = MakeState();
        // 4 Infantry available; move 2 to Adjacent1 and 3 to Adjacent2 → total 5 > 4 → rejected
        var result = Submit(
            s,
            P1Id,
            [
                Move(P1Home, Adjacent1, (NexusUnitType.Infantry, 2)),
                Move(P1Home, Adjacent2, (NexusUnitType.Infantry, 3)),
            ]
        );
        Assert.IsType<NexusTurnOrdersRejected>(result);
    }

    [Fact]
    public void SubmitOrders_RejectsDuplicateSubmission()
    {
        var s = MakeState();
        var cmd = new NexusTurnOrdersCommand(
            P1Id,
            1,
            ImmutableArray<NexusMoveOrder>.Empty,
            ImmutableArray<NexusBuildOrder>.Empty,
            false
        );
        NexusEngine.SubmitOrders(s, cmd, new Random(42));
        var result = NexusEngine.SubmitOrders(s, cmd, new Random(42));
        Assert.IsType<NexusTurnOrdersRejected>(result);
    }

    [Fact]
    public void SubmitOrders_RejectsWrongRoundNumber()
    {
        var s = MakeState();
        var cmd = new NexusTurnOrdersCommand(
            P1Id,
            99,
            ImmutableArray<NexusMoveOrder>.Empty,
            ImmutableArray<NexusBuildOrder>.Empty,
            false
        );
        Assert.IsType<NexusTurnOrdersRejected>(NexusEngine.SubmitOrders(s, cmd, new Random(42)));
    }
}

// ── Engine — Round Resolution ─────────────────────────────────────────────────

public class NexusRoundResolutionTests
{
    private static readonly Guid P1Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid P2Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

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
        return state;
    }

    private static void SubmitBoth(
        NexusState state,
        ImmutableArray<NexusMoveOrder> p1Moves = default,
        ImmutableArray<NexusMoveOrder> p2Moves = default
    )
    {
        NexusEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(
                P1Id,
                state.RoundNumber,
                p1Moves.IsDefault ? [] : p1Moves,
                [],
                false
            ),
            new Random(42)
        );
        NexusEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(
                P2Id,
                state.RoundNumber,
                p2Moves.IsDefault ? [] : p2Moves,
                [],
                false
            ),
            new Random(42)
        );
    }

    [Fact]
    public void BothPlayersSubmit_AdvancesRound()
    {
        var s = MakeState();
        SubmitBoth(s);
        Assert.Equal(2, s.RoundNumber);
        Assert.Null(s.Completion);
        Assert.All(s.Players, p => Assert.False(p.HasSubmittedOrders));
    }

    [Fact]
    public void AfterRound_HomeIncomeAdded()
    {
        var s = MakeState();
        SubmitBoth(s);
        // Each player gets home income (2E) + any controlled income systems
        // At start each only controls their home; home value = 2
        Assert.True(s.Players[0].Energy >= 2);
        Assert.True(s.Players[1].Energy >= 2);
    }

    [Fact]
    public void Move_UnitsArriveAtDestination()
    {
        var s = MakeState();
        var target = new HexCoord(1, -2); // adjacent to P1 home (2,-2)
        var homeUnits = s.Systems.First(sys => sys.HomePlayerId == P1Id).GetPlayerUnits(P1Id);
        var carriersAtHome = homeUnits.GetValueOrDefault(NexusUnitType.Carrier);

        SubmitBoth(
            s,
            p1Moves:
            [
                new NexusMoveOrder(
                    NexusMap.Player1HomeCoord,
                    target,
                    ImmutableDictionary<NexusUnitType, int>
                        .Empty.Add(NexusUnitType.Carrier, 1)
                        .Add(NexusUnitType.Infantry, 1)
                ),
            ]
        );

        var dst = s.Systems.First(sys => sys.Coord == target);
        Assert.Equal(1, dst.GetUnitCount(P1Id, NexusUnitType.Carrier));
        Assert.Equal(1, dst.GetUnitCount(P1Id, NexusUnitType.Infantry));

        var home = s.Systems.First(sys => sys.HomePlayerId == P1Id);
        Assert.Equal(carriersAtHome - 1, home.GetUnitCount(P1Id, NexusUnitType.Carrier));
    }

    [Fact]
    public void Move_IntoUncontrolledSystem_WithGF_GrantsControl()
    {
        var s = MakeState();
        var target = new HexCoord(1, -2); // starts uncontrolled

        SubmitBoth(
            s,
            p1Moves:
            [
                new NexusMoveOrder(
                    NexusMap.Player1HomeCoord,
                    target,
                    ImmutableDictionary<NexusUnitType, int>
                        .Empty.Add(NexusUnitType.Carrier, 1)
                        .Add(NexusUnitType.Infantry, 1)
                ),
            ]
        );

        var sys = s.Systems.First(sys => sys.Coord == target);
        Assert.Equal(P1Id, sys.ControlOwner);
    }

    [Fact]
    public void Move_ShipsOnly_DoNotGrantControl()
    {
        var s = MakeState();
        var target = new HexCoord(1, -2);

        SubmitBoth(
            s,
            p1Moves:
            [
                new NexusMoveOrder(
                    NexusMap.Player1HomeCoord,
                    target,
                    ImmutableDictionary<NexusUnitType, int>.Empty.Add(NexusUnitType.Carrier, 1)
                ),
            ]
        );

        var sys = s.Systems.First(sys => sys.Coord == target);
        Assert.Null(sys.ControlOwner); // ships don't grant control
    }

    [Fact]
    public void BuildOrder_UnitAppearsAtHome()
    {
        var s = MakeState();
        // Give P1 enough energy
        s.Players[0].Energy = 10;

        NexusEngine.SubmitOrders(
            s,
            new NexusTurnOrdersCommand(
                P1Id,
                1,
                [],
                [new NexusBuildOrder(NexusUnitType.Infantry, 2)],
                false
            ),
            new Random(42)
        );
        NexusEngine.SubmitOrders(
            s,
            new NexusTurnOrdersCommand(P2Id, 1, [], [], false),
            new Random(42)
        );

        var home = s.Systems.First(sys => sys.HomePlayerId == P1Id);
        // Started with 4 Infantry, built 2 more = 6
        Assert.Equal(6, home.GetUnitCount(P1Id, NexusUnitType.Infantry));
    }

    [Fact]
    public void Combat_EmitsNexusCombatBeganEvent()
    {
        var s = MakeState();
        // Place P2 units adjacent to P1 home so combat occurs there
        var p1Home = s.Systems.First(sys => sys.HomePlayerId == P1Id);
        // Move P2 infantry to a position adjacent to P1 home
        var nearP1 = new HexCoord(1, -2); // adjacent to P1 home (2,-2)
        s.Systems.First(sys => sys.Coord == nearP1).AddUnits(P2Id, NexusUnitType.Infantry, 2);
        s.Systems.First(sys => sys.Coord == nearP1).AddUnits(P2Id, NexusUnitType.Carrier, 1);

        // P1 moves carrier + infantry to nearP1; P2 stays
        SubmitBoth(
            s,
            p1Moves:
            [
                new NexusMoveOrder(
                    NexusMap.Player1HomeCoord,
                    nearP1,
                    ImmutableDictionary<NexusUnitType, int>
                        .Empty.Add(NexusUnitType.Carrier, 1)
                        .Add(NexusUnitType.Infantry, 1)
                ),
            ]
        );

        Assert.Contains(s.LastResolveEvents, e => e is NexusCombatBeganEvent);
    }

    [Fact]
    public void Combat_MultipleContested_ResolvesInSpiralOrder()
    {
        var s = MakeState();
        // Place both players' units at Alpha (1,-1) Ring 1 and Eta (2,-1) Ring 2.
        // Alpha is earlier in spiral order → its NexusCombatBeganEvent must appear first.
        var alpha = s.Systems.First(sys => sys.Coord == new HexCoord(1, -1));
        alpha.AddUnits(P1Id, NexusUnitType.Carrier, 1);
        alpha.AddUnits(P1Id, NexusUnitType.Infantry, 1);
        alpha.AddUnits(P2Id, NexusUnitType.Carrier, 1);
        alpha.AddUnits(P2Id, NexusUnitType.Infantry, 1);

        var eta = s.Systems.First(sys => sys.Coord == new HexCoord(2, -1));
        eta.AddUnits(P1Id, NexusUnitType.Carrier, 1);
        eta.AddUnits(P1Id, NexusUnitType.Infantry, 1);
        eta.AddUnits(P2Id, NexusUnitType.Carrier, 1);
        eta.AddUnits(P2Id, NexusUnitType.Infantry, 1);

        SubmitBoth(s);

        var combatStarts = s
            .LastResolveEvents.OfType<NexusCombatBeganEvent>()
            .Select(e => e.System)
            .ToList();

        Assert.Equal(2, combatStarts.Count);
        Assert.Equal(new HexCoord(1, -1), combatStarts[0]); // Alpha before Eta
        Assert.Equal(new HexCoord(2, -1), combatStarts[1]);
    }

    [Fact]
    public void Abandon_SetsOpponentAsWinner()
    {
        var s = MakeState();
        NexusEngine.Abandon(s, P1Id);
        Assert.Equal(NexusGameOutcome.Victory, s.Completion!.Outcome);
        Assert.Equal(P2Id, s.Completion.WinnerId);
    }
}

// ── Engine — Persistent Damage ────────────────────────────────────────────────

public class NexusPersistentDamageTests
{
    private static readonly Guid P1Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid P2Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

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
        return state;
    }

    private static void SubmitBoth(
        NexusState state,
        ImmutableArray<NexusMoveOrder> p1Moves = default,
        ImmutableArray<NexusMoveOrder> p2Moves = default
    )
    {
        NexusEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(
                P1Id,
                state.RoundNumber,
                p1Moves.IsDefault ? [] : p1Moves,
                [],
                false
            ),
            new Random(42)
        );
        NexusEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(
                P2Id,
                state.RoundNumber,
                p2Moves.IsDefault ? [] : p2Moves,
                [],
                false
            ),
            new Random(42)
        );
    }

    [Fact]
    public void Damage_PersistsBetweenRounds()
    {
        var s = MakeState();
        var p1Home = s.Systems.First(sys => sys.HomePlayerId == P1Id);

        // Replace P1's home units with a single Carrier that already has 1 hit absorbed
        p1Home.Units[P1Id] =
        [
            new NexusUnitStack
            {
                UnitType = NexusUnitType.Carrier,
                HitsAbsorbed = 1,
                Count = 1,
            },
        ];

        // Submit a round with no moves and no combat (P2 stays home)
        SubmitBoth(s);

        // Damage must survive the round unchanged
        var stack = p1Home.GetPlayerStacks(P1Id).Single(st => st.UnitType == NexusUnitType.Carrier);
        Assert.Equal(1, stack.HitsAbsorbed);
        Assert.Equal(1, stack.Count);
    }

    [Fact]
    public void Move_FromContested_TakesMostDamagedFirst()
    {
        var s = MakeState();
        var p1Home = s.Systems.First(sys => sys.HomePlayerId == P1Id);

        // Replace P1 home units: Infantry + Fighters + 2 Frigates (no Carrier).
        // Keeping capitals to exactly 2 ensures supply pool (2) is never exceeded,
        // so the supply check cannot disband the retreating Frigate.
        p1Home.Units[P1Id] =
        [
            new NexusUnitStack
            {
                UnitType = NexusUnitType.Infantry,
                HitsAbsorbed = 0,
                Count = 4,
            },
            new NexusUnitStack
            {
                UnitType = NexusUnitType.Fighter,
                HitsAbsorbed = 0,
                Count = 2,
            },
            new NexusUnitStack
            {
                UnitType = NexusUnitType.Frigate,
                HitsAbsorbed = 1,
                Count = 1,
            },
            new NexusUnitStack
            {
                UnitType = NexusUnitType.Frigate,
                HitsAbsorbed = 0,
                Count = 1,
            },
        ];

        // Place P2 in P1's home system to make it contested
        p1Home.Units[P2Id] =
        [
            new NexusUnitStack
            {
                UnitType = NexusUnitType.Frigate,
                HitsAbsorbed = 0,
                Count = 1,
            },
        ];

        var adjacent = new HexCoord(1, -2); // adjacent to P1 home (2,-2)

        // P1 retreats 1 Frigate; P2 stays
        SubmitBoth(
            s,
            p1Moves:
            [
                new NexusMoveOrder(
                    NexusMap.Player1HomeCoord,
                    adjacent,
                    ImmutableDictionary<NexusUnitType, int>.Empty.Add(NexusUnitType.Frigate, 1)
                ),
            ]
        );

        // The most-damaged Frigate (HitsAbsorbed=1) should have moved to the destination
        var dst = s.Systems.First(sys => sys.Coord == adjacent);
        var movedStack = dst.GetPlayerStacks(P1Id)
            .Single(st => st.UnitType == NexusUnitType.Frigate);
        Assert.Equal(1, movedStack.HitsAbsorbed);
    }

    [Fact]
    public void Move_Event_IsRetreat_SetCorrectly()
    {
        var s = MakeState();
        var p1Home = s.Systems.First(sys => sys.HomePlayerId == P1Id);
        var p2Home = s.Systems.First(sys => sys.HomePlayerId == P2Id);

        // Make P1 home contested by adding a P2 unit there
        p1Home.Units[P2Id] =
        [
            new NexusUnitStack
            {
                UnitType = NexusUnitType.Frigate,
                HitsAbsorbed = 0,
                Count = 1,
            },
        ];

        var adjacentToP1 = new HexCoord(1, -2); // adjacent to P1 home (2,-2)
        var adjacentToP2 = new HexCoord(-1, 2); // adjacent to P2 home (-2,2)

        // P1 retreats from contested home; P2 advances from non-contested home
        SubmitBoth(
            s,
            p1Moves:
            [
                new NexusMoveOrder(
                    NexusMap.Player1HomeCoord,
                    adjacentToP1,
                    ImmutableDictionary<NexusUnitType, int>.Empty.Add(NexusUnitType.Carrier, 1)
                ),
            ],
            p2Moves:
            [
                new NexusMoveOrder(
                    NexusMap.Player2HomeCoord,
                    adjacentToP2,
                    ImmutableDictionary<NexusUnitType, int>.Empty.Add(NexusUnitType.Carrier, 1)
                ),
            ]
        );

        var p1MoveEvent = s
            .LastResolveEvents.OfType<NexusUnitsMovedEvent>()
            .Single(e => e.PlayerId == P1Id);
        var p2MoveEvent = s
            .LastResolveEvents.OfType<NexusUnitsMovedEvent>()
            .Single(e => e.PlayerId == P2Id);

        Assert.True(p1MoveEvent.IsRetreat);
        Assert.False(p2MoveEvent.IsRetreat);
    }
}

// ── Engine — Gate ─────────────────────────────────────────────────────────────

public class NexusGateTests
{
    private static readonly Guid P1Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid P2Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

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
        return state;
    }

    [Fact]
    public void Gate_RejectedWhenNoGFOnNexus()
    {
        var s = MakeState();
        s.Players[0].Energy = 50;

        var result = NexusEngine.SubmitOrders(
            s,
            new NexusTurnOrdersCommand(P1Id, 1, [], [], BeginNexusGate: true),
            new Random(42)
        );

        Assert.IsType<NexusTurnOrdersRejected>(result);
    }

    [Fact]
    public void Gate_RejectedWhenInsufficientEnergy()
    {
        var s = MakeState();
        s.Players[0].Energy = 5; // need 12
        var nexus = s.Systems.Single(sys => sys.IsNexus);
        nexus.AddUnits(P1Id, NexusUnitType.Infantry, 1);

        var result = NexusEngine.SubmitOrders(
            s,
            new NexusTurnOrdersCommand(P1Id, 1, [], [], BeginNexusGate: true),
            new Random(42)
        );

        Assert.IsType<NexusTurnOrdersRejected>(result);
    }

    [Fact]
    public void Gate_StartsWhenGFOnNexusAndEnoughEnergy()
    {
        var s = MakeState();
        s.Players[0].Energy = 50;
        var nexus = s.Systems.Single(sys => sys.IsNexus);
        nexus.AddUnits(P1Id, NexusUnitType.Infantry, 1);

        NexusEngine.SubmitOrders(
            s,
            new NexusTurnOrdersCommand(P1Id, 1, [], [], BeginNexusGate: true),
            new Random(42)
        );
        NexusEngine.SubmitOrders(
            s,
            new NexusTurnOrdersCommand(P2Id, 1, [], [], false),
            new Random(42)
        );

        Assert.Equal(NexusGateProgress.Started, s.Players[0].GateProgress);
        Assert.Contains(s.LastResolveEvents, e => e is NexusGateStartedEvent);
    }

    [Fact]
    public void Gate_CompletesAfterTwoSuccessfulTurns()
    {
        var s = MakeState();
        s.Players[0].Energy = 200;
        var nexus = s.Systems.Single(sys => sys.IsNexus);
        nexus.AddUnits(P1Id, NexusUnitType.Infantry, 1);

        void Submit(int round)
        {
            NexusEngine.SubmitOrders(
                s,
                new NexusTurnOrdersCommand(P1Id, round, [], [], BeginNexusGate: true),
                new Random(42)
            );
            NexusEngine.SubmitOrders(
                s,
                new NexusTurnOrdersCommand(P2Id, round, [], [], false),
                new Random(42)
            );
        }

        Submit(1);
        Assert.Equal(NexusGateProgress.Started, s.Players[0].GateProgress);

        Submit(2);
        Assert.Equal(NexusGateProgress.Completed, s.Players[0].GateProgress);
        Assert.Equal(NexusGameOutcome.Victory, s.Completion!.Outcome);
        Assert.Equal(P1Id, s.Completion.WinnerId);
    }

    [Fact]
    public void Gate_CancelledWhenPlayerDoesNotCommitNextTurn()
    {
        var s = MakeState();
        s.Players[0].Energy = 200;
        var nexus = s.Systems.Single(sys => sys.IsNexus);
        nexus.AddUnits(P1Id, NexusUnitType.Infantry, 1);

        // Turn 1: start gate
        NexusEngine.SubmitOrders(
            s,
            new NexusTurnOrdersCommand(P1Id, 1, [], [], BeginNexusGate: true),
            new Random(42)
        );
        NexusEngine.SubmitOrders(
            s,
            new NexusTurnOrdersCommand(P2Id, 1, [], [], false),
            new Random(42)
        );
        Assert.Equal(NexusGateProgress.Started, s.Players[0].GateProgress);

        // Turn 2: don't commit
        NexusEngine.SubmitOrders(
            s,
            new NexusTurnOrdersCommand(P2Id, 2, [], [], false),
            new Random(42)
        );
        NexusEngine.SubmitOrders(
            s,
            new NexusTurnOrdersCommand(P1Id, 2, [], [], BeginNexusGate: false),
            new Random(42)
        );

        Assert.Equal(NexusGateProgress.None, s.Players[0].GateProgress);
        Assert.Contains(s.LastResolveEvents, e => e is NexusGateCancelledEvent);
    }
}

// ── Supply Check ──────────────────────────────────────────────────────────────

public class NexusSupplyCheckTests
{
    private static readonly Guid P1Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid P2Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly Guid GameId = Guid.Parse("cccccccc-0000-0000-0000-000000000003");

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
        return state;
    }

    private static void SubmitBoth(NexusState state)
    {
        NexusEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(P1Id, state.RoundNumber, [], [], false),
            new Random(42)
        );
        NexusEngine.SubmitOrders(
            state,
            new NexusTurnOrdersCommand(P2Id, state.RoundNumber, [], [], false),
            new Random(42)
        );
    }

    [Fact]
    public void SupplyCheck_NoDeficit_NoEventsEmitted()
    {
        // Initial: P1 has 1 Carrier at home (supply pool=2, capital count=1) — no deficit.
        var s = MakeState();
        SubmitBoth(s);
        Assert.DoesNotContain(s.LastResolveEvents, e => e is NexusCapitalDisbandedEvent);
    }

    [Fact]
    public void SupplyCheck_Deficit_DisbandsCheapestCapitalFirst()
    {
        var s = MakeState();
        // P1 home has 1 Carrier. Add 2 Frigates + 1 Carrier to Nexus for P1.
        // Total capitals = 4 (Frigate×2, Carrier×2). Supply pool = 2 (home only). Deficit = 2.
        // Frigates are cheaper (cost 4 vs 8) and Nexus is first in spiral → 2 Frigates disbanded.
        var nexus = s.Systems.First(sys => sys.IsNexus);
        nexus.AddUnits(P1Id, NexusUnitType.Frigate, 2);
        nexus.AddUnits(P1Id, NexusUnitType.Carrier, 1);

        SubmitBoth(s);

        var disbandEvents = s
            .LastResolveEvents.OfType<NexusCapitalDisbandedEvent>()
            .Where(e => e.PlayerId == P1Id)
            .ToList();
        Assert.Single(disbandEvents);
        var ev = disbandEvents[0];
        Assert.Equal(NexusUnitType.Frigate, ev.UnitType);
        Assert.Equal(NexusMap.NexusCoord, ev.System);
        Assert.Equal(2, ev.Count);
    }

    [Fact]
    public void SupplyCheck_SpiralOrder_Ring1DisbandedBeforeHome()
    {
        var s = MakeState();
        // P1 home already has 1 Carrier. Add 1 Frigate to Alpha (Ring 1) and 1 Frigate to home (Ring 2).
        // Total capitals = 3. Supply pool = 2. Deficit = 1.
        // Alpha (1,-1) is earlier in spiral than home (2,-2) → Alpha Frigate disbanded first.
        var alpha = s.Systems.First(sys => sys.Coord == new HexCoord(1, -1));
        alpha.AddUnits(P1Id, NexusUnitType.Frigate, 1);
        var p1Home = s.Systems.First(sys => sys.HomePlayerId == P1Id);
        p1Home.AddUnits(P1Id, NexusUnitType.Frigate, 1);

        SubmitBoth(s);

        var disbanded = s
            .LastResolveEvents.OfType<NexusCapitalDisbandedEvent>()
            .Where(e => e.PlayerId == P1Id)
            .ToList();
        Assert.Single(disbanded);
        Assert.Equal(new HexCoord(1, -1), disbanded[0].System);
        Assert.Equal(NexusUnitType.Frigate, disbanded[0].UnitType);
        Assert.Equal(1, disbanded[0].Count);
    }

    [Fact]
    public void SupplyCheck_PlanetaryUnits_NeverDisbanded()
    {
        var s = MakeState();
        // Add many Infantry (planetary) to Nexus — planetary units don't count as Capitals.
        // Supply pool = 2, capital count = 1 (home Carrier), no deficit.
        var nexus = s.Systems.First(sys => sys.IsNexus);
        nexus.AddUnits(P1Id, NexusUnitType.Infantry, 10);
        nexus.AddUnits(P1Id, NexusUnitType.Fighter, 5);

        SubmitBoth(s);

        Assert.DoesNotContain(s.LastResolveEvents, e => e is NexusCapitalDisbandedEvent);
    }

    [Fact]
    public void SupplyCheck_UncontrolledSystem_ContributesZeroSupply()
    {
        var s = MakeState();
        // P1 has 3 Frigates at Alpha (uncontrolled). Supply pool = 2 (home only). Deficit = 2.
        var alpha = s.Systems.First(sys => sys.Coord == new HexCoord(1, -1));
        alpha.AddUnits(P1Id, NexusUnitType.Frigate, 3);

        SubmitBoth(s);

        var disbandEvents = s
            .LastResolveEvents.OfType<NexusCapitalDisbandedEvent>()
            .Where(e => e.PlayerId == P1Id)
            .ToList();
        Assert.Equal(2, disbandEvents.Sum(e => e.Count));
    }

    [Fact]
    public void SupplyCheck_NexusContributesZeroSupply()
    {
        var s = MakeState();
        // Manually give P1 control of Nexus (IncomeValue = 0). Supply pool should still be 2.
        var nexus = s.Systems.First(sys => sys.IsNexus);
        nexus.ControlOwner = P1Id;
        // P1 has 1 Carrier at home. Supply = 2 + 0 = 2. Capital count = 1. No deficit.

        SubmitBoth(s);

        Assert.DoesNotContain(s.LastResolveEvents, e => e is NexusCapitalDisbandedEvent);
    }

    [Fact]
    public void PlayerView_SupplyPool_MatchesControlledIncome()
    {
        var s = MakeState();
        SubmitBoth(s);

        var view = NexusEngine.BuildView(s, GameId, P1Id);
        var expected = s.Systems.Where(sys => sys.ControlOwner == P1Id).Sum(sys => sys.IncomeValue);
        Assert.Equal(expected, view.CurrentPlayer.SupplyPool);
    }

    [Fact]
    public void PlayerView_CapitalCount_MatchesTotalCapitals()
    {
        var s = MakeState();
        SubmitBoth(s);

        var view = NexusEngine.BuildView(s, GameId, P1Id);
        var expected = s.Systems.Sum(sys =>
            sys.GetUnitCount(P1Id, NexusUnitType.Frigate)
            + sys.GetUnitCount(P1Id, NexusUnitType.Destroyer)
            + sys.GetUnitCount(P1Id, NexusUnitType.Cruiser)
            + sys.GetUnitCount(P1Id, NexusUnitType.Carrier)
        );
        Assert.Equal(expected, view.CurrentPlayer.CapitalCount);
    }
}

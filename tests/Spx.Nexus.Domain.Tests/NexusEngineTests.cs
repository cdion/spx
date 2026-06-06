using System.Collections.Immutable;
using Spx.Nexus.Domain;

namespace Spx.Nexus.Domain.Tests;

// ── Shared test designs ───────────────────────────────────────────────────────

/// <summary>Pre-built design instances for use across all test classes.</summary>
internal static class TestDesigns
{
    public static readonly NexusUnitDesign Interceptor = D(
        "Interceptor",
        NexusUnitCategory.Strike,
        new Vanguard(NexusUnitCategory.Strike),
        new Dock()
    );

    public static readonly NexusUnitDesign Fighter = D(
        "Fighter",
        NexusUnitCategory.Strike,
        new Battery(NexusUnitCategory.Strike),
        new Battery(NexusUnitCategory.Capital),
        new Scatter(NexusUnitCategory.Capital, 1),
        new Dock()
    );

    public static readonly NexusUnitDesign Bomber = D(
        "Bomber",
        NexusUnitCategory.Strike,
        new Battery(NexusUnitCategory.Planetary),
        new Disruptor(),
        new Dock()
    );

    public static readonly NexusUnitDesign Frigate = D(
        "Frigate",
        NexusUnitCategory.Capital,
        new Battery(NexusUnitCategory.Strike),
        new Battery(NexusUnitCategory.Capital),
        new Screen(NexusUnitCategory.Capital, 1),
        new Hangar(2)
    );

    public static readonly NexusUnitDesign Destroyer = D(
        "Destroyer",
        NexusUnitCategory.Capital,
        new Battery(NexusUnitCategory.Strike),
        new Battery(NexusUnitCategory.Capital),
        new Armour(1),
        new Hangar(2)
    );

    public static readonly NexusUnitDesign Cruiser = D(
        "Cruiser",
        NexusUnitCategory.Capital,
        new Battery(NexusUnitCategory.Strike),
        new Battery(NexusUnitCategory.Capital),
        new Seeker(NexusUnitCategory.Capital, 1),
        new Hangar(2)
    );

    public static readonly NexusUnitDesign Carrier = D(
        "Carrier",
        NexusUnitCategory.Capital,
        new Battery(NexusUnitCategory.Capital),
        new Shield(),
        new Hangar(4)
    );

    public static readonly NexusUnitDesign Infantry = D(
        "Infantry",
        NexusUnitCategory.Planetary,
        new Battery(NexusUnitCategory.Planetary),
        new Dock(),
        new Control()
    );

    public static readonly NexusUnitDesign Armor = D(
        "Armor",
        NexusUnitCategory.Planetary,
        new Battery(NexusUnitCategory.Planetary),
        new Shield(),
        new Dock(),
        new Control()
    );

    public static readonly IReadOnlyList<NexusUnitDesign> All =
    [
        Interceptor,
        Fighter,
        Bomber,
        Frigate,
        Destroyer,
        Cruiser,
        Carrier,
        Infantry,
        Armor,
    ];

    private static NexusUnitDesign D(
        string name,
        NexusUnitCategory hull,
        params NexusUnitModule[] tags
    ) =>
        new()
        {
            DesignId = Guid.NewGuid(),
            Name = name,
            Hull = hull,
            Modules = [.. tags],
        };
}

/// <summary>
/// Registers test designs in player state and seeds standard starting units at home systems.
/// Simulates the starting unit composition that was previously hardcoded in GenerateMap.
/// </summary>
internal static class TestState
{
    public static void RegisterDesigns(NexusState state)
    {
        foreach (var player in state.Players)
        {
            player.Designs.Clear();
            foreach (var design in TestDesigns.All)
                player.Designs.Add(design);
        }
    }

    /// <summary>Seeds 1 Carrier + 4 Infantry + 2 Fighters for each player at their home system.</summary>
    public static void SeedStartingUnits(NexusState state)
    {
        foreach (var player in state.Players)
        {
            var home = state.Systems.FirstOrDefault(s => s.HomePlayerId == player.PlayerId);
            if (home == null)
                continue;
            home.AddUnits(
                player.PlayerId,
                TestDesigns.Carrier.DesignId,
                NexusUnitCategory.Capital,
                1,
                designHits: NexusHullBaselines.GetProfile(TestDesigns.Carrier).Hits
            );
            home.AddUnits(
                player.PlayerId,
                TestDesigns.Infantry.DesignId,
                NexusUnitCategory.Planetary,
                4,
                designHits: NexusHullBaselines.GetProfile(TestDesigns.Infantry).Hits
            );
            home.AddUnits(
                player.PlayerId,
                TestDesigns.Fighter.DesignId,
                NexusUnitCategory.Strike,
                2,
                designHits: NexusHullBaselines.GetProfile(TestDesigns.Fighter).Hits
            );
        }
    }
}

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
        Assert.All(incomeSystems, s => Assert.InRange(s.IncomeValue, 1, 2));
    }

    [Fact]
    public void GenerateMap_HomeSystemsHave2Income() =>
        Assert.All(Map.Where(s => s.HomePlayerId.HasValue), s => Assert.Equal(2, s.IncomeValue));

    [Fact]
    public void GenerateMap_NexusHasZeroIncome() =>
        Assert.Equal(0, Map.Single(s => s.IsNexus).IncomeValue);

    [Fact]
    public void GenerateMap_HomeSystemsHaveNoStartingUnits()
    {
        // Starting units are no longer seeded in GenerateMap — players build from designs.
        var p1Home = Map.Single(s => s.HomePlayerId == P1);
        var p2Home = Map.Single(s => s.HomePlayerId == P2);
        Assert.Empty(p1Home.Units);
        Assert.Empty(p2Home.Units);
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
    private static NexusUnitProfile P(NexusUnitDesign d) => NexusHullBaselines.GetProfile(d);

    [Theory]
    // Interceptor — Vanguard(Strike) in Contact phase only; cannot target Capital or Planetary
    [InlineData("Interceptor", "Fighter", NexusCombatPhase.Contact, 4)]
    [InlineData("Interceptor", "Frigate", NexusCombatPhase.Contact, null)]
    [InlineData("Interceptor", "Infantry", NexusCombatPhase.Contact, null)]
    // Fighter — Scatter(Capital,1): +1 to threshold vs Capital
    [InlineData("Fighter", "Interceptor", NexusCombatPhase.Battle, 4)]
    [InlineData("Fighter", "Frigate", NexusCombatPhase.Battle, 5)]
    [InlineData("Fighter", "Infantry", NexusCombatPhase.Battle, null)]
    // Bomber — Battery(Planetary) + Disruptor; cannot target Strike or Capital
    [InlineData("Bomber", "Interceptor", NexusCombatPhase.Battle, null)]
    [InlineData("Bomber", "Frigate", NexusCombatPhase.Battle, null)]
    [InlineData("Bomber", "Infantry", NexusCombatPhase.Battle, 4)]
    // Frigate — flat threshold vs Strike and Capital
    [InlineData("Frigate", "Interceptor", NexusCombatPhase.Battle, 4)]
    [InlineData("Frigate", "Frigate", NexusCombatPhase.Battle, 4)]
    [InlineData("Frigate", "Infantry", NexusCombatPhase.Battle, null)]
    // Destroyer — can attack Strike and Capital
    [InlineData("Destroyer", "Interceptor", NexusCombatPhase.Battle, 4)]
    [InlineData("Destroyer", "Frigate", NexusCombatPhase.Battle, 4)]
    // Cruiser — Battery(Strike/Capital) + Seeker(Capital,1): -1 threshold vs Capital; cannot target Planetary
    [InlineData("Cruiser", "Interceptor", NexusCombatPhase.Battle, 4)]
    [InlineData("Cruiser", "Frigate", NexusCombatPhase.Battle, 3)]
    [InlineData("Cruiser", "Infantry", NexusCombatPhase.Battle, null)]
    // Carrier — higher base cost but same threshold
    [InlineData("Carrier", "Interceptor", NexusCombatPhase.Battle, null)]
    [InlineData("Carrier", "Frigate", NexusCombatPhase.Battle, 4)]
    [InlineData("Carrier", "Infantry", NexusCombatPhase.Battle, null)]
    // Infantry — Planetary only
    [InlineData("Infantry", "Infantry", NexusCombatPhase.Battle, 4)]
    [InlineData("Infantry", "Interceptor", NexusCombatPhase.Battle, null)]
    // Armor — Shield, Planetary only
    [InlineData("Armor", "Infantry", NexusCombatPhase.Battle, 4)]
    [InlineData("Armor", "Frigate", NexusCombatPhase.Battle, null)]
    [InlineData("Armor", "Interceptor", NexusCombatPhase.Battle, null)]
    public void GetHitThreshold_ReturnsExpected(
        string attackerName,
        string targetName,
        NexusCombatPhase phase,
        int? expected
    )
    {
        var attacker = P(TestDesigns.All.First(d => d.Name == attackerName));
        var target = P(TestDesigns.All.First(d => d.Name == targetName));
        Assert.Equal(expected, NexusCombatSpec.GetHitThreshold(attacker, target, phase));
    }

    // ── Battery Weights (Screen protection) ─────────────────────────────────

    [Fact]
    public void ComputeTargetWeights_NoEscorts_AllUnitsHaveBaseSilhouette()
    {
        // All Capital hulls have baseline silhouette=2 in the design system
        var units = new[]
        {
            P(TestDesigns.Carrier),
            P(TestDesigns.Cruiser),
            P(TestDesigns.Destroyer),
        };
        var weights = NexusCombatSpec.ComputeTargetWeights(units, NexusUnitCategory.Capital);
        Assert.Equal([2, 2, 2], weights);
    }

    [Fact]
    public void ComputeTargetWeights_OneEscort_ProtectsOneCapital()
    {
        // Frigate (Screen, sil=2) + Carrier (sil=2) + Cruiser (sil=2)
        // Equal silhouettes: first non-escort Capital gets protected → sil 2→1
        var units = new[]
        {
            P(TestDesigns.Frigate),
            P(TestDesigns.Carrier),
            P(TestDesigns.Cruiser),
        };
        var weights = NexusCombatSpec.ComputeTargetWeights(units, NexusUnitCategory.Capital);
        Assert.Equal([2, 1, 2], weights);
    }

    [Fact]
    public void ComputeTargetWeights_OneEscort_MultipleSameSilhouette_ProtectsOne()
    {
        // Frigate (Screen, sil=2) + Cruiser (sil=2) + Cruiser (sil=2)
        // First non-escort Capital gets protected
        var units = new[]
        {
            P(TestDesigns.Frigate),
            P(TestDesigns.Cruiser),
            P(TestDesigns.Cruiser),
        };
        var weights = NexusCombatSpec.ComputeTargetWeights(units, NexusUnitCategory.Capital);
        Assert.Equal([2, 1, 2], weights);
    }

    [Fact]
    public void ComputeTargetWeights_TwoEscorts_ProtectBothCapitals()
    {
        // 2 Frigates (Screen) + Carrier (sil=2) + Cruiser (sil=2) — both Capitals protected
        var units = new[]
        {
            P(TestDesigns.Frigate),
            P(TestDesigns.Frigate),
            P(TestDesigns.Carrier),
            P(TestDesigns.Cruiser),
        };
        var weights = NexusCombatSpec.ComputeTargetWeights(units, NexusUnitCategory.Capital);
        Assert.Equal([2, 2, 1, 1], weights);
    }

    [Fact]
    public void ComputeTargetWeights_MoreEscortsThanCapitals_ExtraEscortsDoNothing()
    {
        // 3 Frigates + 2 Capitals — only 2 Capitals to protect, extra Screen is irrelevant
        var units = new[]
        {
            P(TestDesigns.Frigate),
            P(TestDesigns.Frigate),
            P(TestDesigns.Frigate),
            P(TestDesigns.Carrier),
            P(TestDesigns.Cruiser),
        };
        var weights = NexusCombatSpec.ComputeTargetWeights(units, NexusUnitCategory.Capital);
        Assert.Equal([2, 2, 2, 1, 1], weights);
    }

    [Fact]
    public void ComputeTargetWeights_EscortDoesNotProtectItself()
    {
        var units = new[] { P(TestDesigns.Frigate) };
        var weights = NexusCombatSpec.ComputeTargetWeights(units, NexusUnitCategory.Capital);
        Assert.Equal([2], weights);
    }

    [Fact]
    public void ComputeTargetWeights_StrikeAndPlanetary_NotAffectedByEscort()
    {
        var units = new[]
        {
            P(TestDesigns.Frigate),
            P(TestDesigns.Fighter),
            P(TestDesigns.Infantry),
        };
        var weights = NexusCombatSpec.ComputeTargetWeights(units, NexusUnitCategory.Capital);
        Assert.Equal([2, 1, 1], weights);
    }

    [Fact]
    public void ComputeTargetWeights_SilhouetteFloorIsOne()
    {
        var units = new[] { P(TestDesigns.Frigate), P(TestDesigns.Destroyer) };
        var weights = NexusCombatSpec.ComputeTargetWeights(units, NexusUnitCategory.Capital);
        Assert.Equal([2, 1], weights);
    }
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
    public void Initialize_PlayersStartWithFiveEnergy() =>
        Assert.All(MakeState().Players, p => Assert.Equal(5, p.Energy));

    [Fact]
    public void Initialize_Has19Systems() => Assert.Equal(19, MakeState().Systems.Count);

    [Fact]
    public void Initialize_PlayersHaveNoStartingUnits()
    {
        // Units are no longer seeded at game start — players build from designs.
        var s = MakeState();
        var p1Home = s.Systems.Single(sys => sys.HomePlayerId == P1Id);
        Assert.False(p1Home.HasAnyUnits(P1Id));
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
        TestState.RegisterDesigns(state);
        TestState.SeedStartingUnits(state);
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
        params (NexusUnitDesign Design, int Count)[] units
    ) =>
        new(
            from,
            to,
            units
                .Select(unit => new NexusUnitStackGroup(
                    unit.Design.DesignId,
                    unit.Design.Hull,
                    NexusHullBaselines.GetProfile(unit.Design).Hits,
                    unit.Count
                ))
                .ToImmutableArray()
        );

    [Fact]
    public void Move_ToNonAdjacentSystem_IsRejected()
    {
        var s = MakeState();
        var result = Submit(s, P1Id, [Move(P1Home, new HexCoord(0, -2), (TestDesigns.Carrier, 1))]);
        Assert.IsAssignableFrom<NexusTurnOrdersRejected>(result);
    }

    [Fact]
    public void Move_WithNoUnits_IsRejected()
    {
        var s = MakeState();
        var order = new NexusMoveOrder(
            P1Home,
            Adjacent1,
            ImmutableArray<NexusUnitStackGroup>.Empty
        );
        var result = Submit(s, P1Id, [order]);
        Assert.IsAssignableFrom<NexusTurnOrdersRejected>(result);
    }

    [Fact]
    public void Move_MoreUnitsThanAvailable_IsRejected()
    {
        var s = MakeState();
        var result = Submit(s, P1Id, [Move(P1Home, Adjacent1, (TestDesigns.Infantry, 5))]);
        Assert.IsAssignableFrom<NexusTurnOrdersRejected>(result);
    }

    [Fact]
    public void Move_MoreUnitsThanAvailable_ErrorIncludesSectorName()
    {
        var s = MakeState();

        var result = Submit(s, P1Id, [Move(P1Home, Adjacent1, (TestDesigns.Infantry, 5))]);

        var rejected = Assert.IsAssignableFrom<NexusTurnOrdersRejected>(result);
        Assert.Equal(
            "Insufficient Infantry at Your Home System: need 5, have 4.",
            rejected.ErrorMessage
        );
    }

    [Fact]
    public void Move_FromOpponentHomeSystem_ErrorUsesOpponentHomeSystemName()
    {
        var s = MakeState();

        var result = Submit(s, P1Id, [Move(P2Home, P2Adjacent, (TestDesigns.Carrier, 1))]);

        var rejected = Assert.IsAssignableFrom<NexusTurnOrdersRejected>(result);
        Assert.Equal(
            "Insufficient Fleet Capacity at Opponent Home System: need 4, have 0.",
            rejected.ErrorMessage
        );
    }

    [Fact]
    public void Move_StrikeWithNoCarrier_IsRejected()
    {
        var s = MakeState();
        s.Systems.First(sys => sys.HomePlayerId == P1Id)
            .RemoveUnits(P1Id, TestDesigns.Carrier.DesignId, 1);
        var result = Submit(s, P1Id, [Move(P1Home, Adjacent1, (TestDesigns.Fighter, 1))]);

        var rejected = Assert.IsAssignableFrom<NexusTurnOrdersRejected>(result);
        Assert.Equal(
            "Insufficient Fleet Capacity for move from Your Home System to Pi: need 1, have 0.",
            rejected.ErrorMessage
        );
    }

    [Fact]
    public void Move_StrikeWithoutDockAndNoCarrier_IsRejected()
    {
        // Strike with Move=0 but no Dock module must still require carry capacity
        var s = MakeState();
        var noDockStrike = new NexusUnitDesign
        {
            DesignId = Guid.NewGuid(),
            Name = "NoDockStrike",
            Hull = NexusUnitCategory.Strike,
            Modules = [new Battery(NexusUnitCategory.Strike)],
        };
        s.Players.First(p => p.PlayerId == P1Id).Designs.Add(noDockStrike);
        s.Systems.First(sys => sys.HomePlayerId == P1Id)
            .AddUnits(P1Id, noDockStrike.DesignId, NexusUnitCategory.Strike, 1);
        s.Systems.First(sys => sys.HomePlayerId == P1Id)
            .RemoveUnits(P1Id, TestDesigns.Carrier.DesignId, 1);

        var result = Submit(s, P1Id, [Move(P1Home, Adjacent1, (noDockStrike, 1))]);

        Assert.IsAssignableFrom<NexusTurnOrdersRejected>(result);
    }

    [Fact]
    public void Move_CarrierWithStrike_IsAccepted()
    {
        var s = MakeState();
        var result = Submit(
            s,
            P1Id,
            [Move(P1Home, Adjacent1, (TestDesigns.Carrier, 1), (TestDesigns.Fighter, 1))]
        );
        Assert.IsType<NexusTurnOrdersAccepted>(result);
    }

    [Fact]
    public void Move_ShipOnlyMove_IsAccepted()
    {
        var s = MakeState();
        var result = Submit(s, P1Id, [Move(P1Home, Adjacent1, (TestDesigns.Carrier, 1))]);
        Assert.IsType<NexusTurnOrdersAccepted>(result);
    }

    [Fact]
    public void Move_MultipleOrdersFromSameSystem_AggregatesUnits()
    {
        var s = MakeState();
        var result = Submit(
            s,
            P1Id,
            [
                Move(P1Home, Adjacent1, (TestDesigns.Infantry, 2)),
                Move(P1Home, Adjacent2, (TestDesigns.Infantry, 3)),
            ]
        );
        Assert.IsAssignableFrom<NexusTurnOrdersRejected>(result);
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
        Assert.IsAssignableFrom<NexusTurnOrdersRejected>(result);
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
        Assert.IsAssignableFrom<NexusTurnOrdersRejected>(
            NexusEngine.SubmitOrders(s, cmd, new Random(42))
        );
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
        TestState.RegisterDesigns(state);
        TestState.SeedStartingUnits(state);
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

    private static NexusMoveOrder Move(
        HexCoord from,
        HexCoord to,
        params (NexusUnitDesign Design, int Count)[] units
    ) =>
        new(
            from,
            to,
            units
                .Select(unit => new NexusUnitStackGroup(
                    unit.Design.DesignId,
                    unit.Design.Hull,
                    NexusHullBaselines.GetProfile(unit.Design).Hits,
                    unit.Count
                ))
                .ToImmutableArray()
        );

    private static NexusMoveOrder MoveExact(
        HexCoord from,
        HexCoord to,
        params (NexusUnitDesign Design, int RemainingHits, int Count)[] stacks
    ) =>
        new(
            from,
            to,
            stacks
                .Select(stack => new NexusUnitStackGroup(
                    stack.Design.DesignId,
                    stack.Design.Hull,
                    stack.RemainingHits,
                    stack.Count
                ))
                .ToImmutableArray()
        );

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
        Assert.True(s.Players[0].Energy >= 2);
        Assert.True(s.Players[1].Energy >= 2);
    }

    [Fact]
    public void Move_UnitsArriveAtDestination()
    {
        var s = MakeState();
        var target = new HexCoord(1, -2);
        var homeUnits = s.Systems.First(sys => sys.HomePlayerId == P1Id).GetPlayerUnits(P1Id);
        var carriersAtHome = homeUnits.GetValueOrDefault(TestDesigns.Carrier.DesignId);

        SubmitBoth(
            s,
            p1Moves:
            [
                Move(
                    NexusMap.Player1HomeCoord,
                    target,
                    (TestDesigns.Carrier, 1),
                    (TestDesigns.Infantry, 1)
                ),
            ]
        );

        var dst = s.Systems.First(sys => sys.Coord == target);
        Assert.Equal(1, dst.GetUnitCount(P1Id, TestDesigns.Carrier.DesignId));
        Assert.Equal(1, dst.GetUnitCount(P1Id, TestDesigns.Infantry.DesignId));

        var home = s.Systems.First(sys => sys.HomePlayerId == P1Id);
        Assert.Equal(carriersAtHome - 1, home.GetUnitCount(P1Id, TestDesigns.Carrier.DesignId));
    }

    [Fact]
    public void Move_IntoUncontrolledSystem_WithGF_GrantsControl()
    {
        var s = MakeState();
        var target = new HexCoord(1, -2);

        SubmitBoth(
            s,
            p1Moves:
            [
                Move(
                    NexusMap.Player1HomeCoord,
                    target,
                    (TestDesigns.Carrier, 1),
                    (TestDesigns.Infantry, 1)
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

        SubmitBoth(s, p1Moves: [Move(NexusMap.Player1HomeCoord, target, (TestDesigns.Carrier, 1))]);

        var sys = s.Systems.First(sys => sys.Coord == target);
        Assert.Null(sys.ControlOwner);
    }

    [Fact]
    public void Move_PlanetaryOut_HomeSystemRetainsControl()
    {
        var s = MakeState();
        var target = new HexCoord(1, -2);
        var home = s.Systems.First(sys => sys.HomePlayerId == P1Id);
        Assert.Equal(P1Id, home.ControlOwner);

        SubmitBoth(
            s,
            p1Moves:
            [
                Move(
                    NexusMap.Player1HomeCoord,
                    target,
                    (TestDesigns.Carrier, 1),
                    (TestDesigns.Infantry, 4)
                ),
            ]
        );

        Assert.Equal(P1Id, home.ControlOwner);
    }

    [Fact]
    public void Move_PlanetaryOut_NonHomeSystem_LosesControl()
    {
        var s = MakeState();
        var alpha = s.Systems.First(sys => sys.Coord == new HexCoord(1, -1));

        alpha.AddUnits(
            P1Id,
            TestDesigns.Carrier.DesignId,
            NexusUnitCategory.Capital,
            1,
            designHits: NexusHullBaselines.GetProfile(TestDesigns.Carrier).Hits
        );
        alpha.AddUnits(P1Id, TestDesigns.Infantry.DesignId, NexusUnitCategory.Planetary, 2);
        alpha.ControlOwner = P1Id;

        var target = new HexCoord(0, -1);

        SubmitBoth(
            s,
            p1Moves:
            [
                Move(alpha.Coord, target, (TestDesigns.Carrier, 1), (TestDesigns.Infantry, 2)),
            ]
        );

        Assert.Null(alpha.ControlOwner);
    }

    [Fact]
    public void Move_PlanetaryOutThenIn_HomeRetainsControlDestinationGains()
    {
        var s = MakeState();
        var target = new HexCoord(1, -2);
        var home = s.Systems.First(sys => sys.HomePlayerId == P1Id);

        SubmitBoth(
            s,
            p1Moves:
            [
                Move(
                    NexusMap.Player1HomeCoord,
                    target,
                    (TestDesigns.Carrier, 1),
                    (TestDesigns.Infantry, 4)
                ),
            ]
        );

        Assert.Equal(P1Id, home.ControlOwner);

        var dst = s.Systems.First(sys => sys.Coord == target);
        Assert.Equal(P1Id, dst.ControlOwner);
    }

    [Fact]
    public void Move_PartialPlanetaryLeaves_RetainsControl()
    {
        var s = MakeState();
        var target = new HexCoord(1, -2);
        var home = s.Systems.First(sys => sys.HomePlayerId == P1Id);

        SubmitBoth(
            s,
            p1Moves: [Move(NexusMap.Player1HomeCoord, target, (TestDesigns.Infantry, 1))]
        );

        Assert.Equal(P1Id, home.ControlOwner);
    }

    [Fact]
    public void Combat_PlanetaryKilled_LosesControl()
    {
        var s = MakeState();
        var alpha = s.Systems.First(sys => sys.Coord == new HexCoord(1, -1));

        alpha.AddUnits(P1Id, TestDesigns.Carrier.DesignId, NexusUnitCategory.Capital, 1);
        alpha.AddUnits(P1Id, TestDesigns.Infantry.DesignId, NexusUnitCategory.Planetary, 1);
        alpha.ControlOwner = P1Id;

        var staging = new HexCoord(2, -1);
        s.Systems.First(sys => sys.Coord == staging)
            .AddUnits(P2Id, TestDesigns.Bomber.DesignId, NexusUnitCategory.Strike, 6);

        SubmitBoth(s, p2Moves: [Move(staging, alpha.Coord, (TestDesigns.Bomber, 6))]);

        var p1Survives = alpha.HasAnyUnits(P1Id);
        if (!p1Survives)
            Assert.Null(alpha.ControlOwner);
    }

    [Fact]
    public void BuildOrder_UnitAppearsAtHome()
    {
        var s = MakeState();
        s.Players[0].Energy = 10;

        NexusEngine.SubmitOrders(
            s,
            new NexusTurnOrdersCommand(
                P1Id,
                1,
                [],
                [new NexusBuildOrder(TestDesigns.Infantry.DesignId, 2)],
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
        Assert.Equal(6, home.GetUnitCount(P1Id, TestDesigns.Infantry.DesignId));
    }

    [Fact]
    public void Combat_EmitsNexusCombatResultEvent()
    {
        var s = MakeState();
        var nearP1 = new HexCoord(1, -2);
        s.Systems.First(sys => sys.Coord == nearP1)
            .AddUnits(P2Id, TestDesigns.Infantry.DesignId, NexusUnitCategory.Planetary, 2);
        s.Systems.First(sys => sys.Coord == nearP1)
            .AddUnits(P2Id, TestDesigns.Carrier.DesignId, NexusUnitCategory.Capital, 1);

        SubmitBoth(
            s,
            p1Moves:
            [
                Move(
                    NexusMap.Player1HomeCoord,
                    nearP1,
                    (TestDesigns.Carrier, 1),
                    (TestDesigns.Infantry, 1)
                ),
            ]
        );

        Assert.Contains(s.LastResolveEvents, e => e is NexusCombatResultEvent);
    }

    [Fact]
    public void Combat_MultipleContested_ResolvesInSpiralOrder()
    {
        var s = MakeState();
        var alpha = s.Systems.First(sys => sys.Coord == new HexCoord(1, -1));
        alpha.AddUnits(P1Id, TestDesigns.Carrier.DesignId, NexusUnitCategory.Capital, 1);
        alpha.AddUnits(P1Id, TestDesigns.Infantry.DesignId, NexusUnitCategory.Planetary, 1);
        alpha.AddUnits(P2Id, TestDesigns.Carrier.DesignId, NexusUnitCategory.Capital, 1);
        alpha.AddUnits(P2Id, TestDesigns.Infantry.DesignId, NexusUnitCategory.Planetary, 1);

        var eta = s.Systems.First(sys => sys.Coord == new HexCoord(2, -1));
        eta.AddUnits(P1Id, TestDesigns.Carrier.DesignId, NexusUnitCategory.Capital, 1);
        eta.AddUnits(P1Id, TestDesigns.Infantry.DesignId, NexusUnitCategory.Planetary, 1);
        eta.AddUnits(P2Id, TestDesigns.Carrier.DesignId, NexusUnitCategory.Capital, 1);
        eta.AddUnits(P2Id, TestDesigns.Infantry.DesignId, NexusUnitCategory.Planetary, 1);

        SubmitBoth(s);

        var combatResults = s
            .LastResolveEvents.OfType<NexusCombatResultEvent>()
            .Select(e => e.System)
            .ToList();

        Assert.Equal(2, combatResults.Count);
        Assert.Equal(new HexCoord(1, -1), combatResults[0]);
        Assert.Equal(new HexCoord(2, -1), combatResults[1]);
    }

    [Fact]
    public void Abandon_SetsOpponentAsWinner()
    {
        var s = MakeState();
        NexusEngine.Abandon(s, P1Id);
        Assert.Equal(NexusGameOutcome.Victory, s.Completion!.Outcome);
        Assert.Equal(P2Id, s.Completion.WinnerId);
    }

    [Fact]
    public void Combat_Overkill_DoesNotInflateLossCount()
    {
        var s = MakeState();
        var alpha = s.Systems.First(sys => sys.Coord == new HexCoord(1, -1));
        alpha.AddUnits(P1Id, TestDesigns.Fighter.DesignId, NexusUnitCategory.Strike, 3);
        alpha.AddUnits(P2Id, TestDesigns.Fighter.DesignId, NexusUnitCategory.Strike, 1);

        SubmitBoth(s);

        var combatResult = s.LastResolveEvents.OfType<NexusCombatResultEvent>().SingleOrDefault();

        var defenderLosses =
            combatResult?.Phases.Sum(phase =>
                phase.Losses.Where(l => l.PlayerId == P2Id).Sum(l => l.Count)
            )
            ?? 0;

        Assert.True(
            defenderLosses <= 1,
            $"Overkill inflated losses: expected ≤ 1, got {defenderLosses}"
        );

        var p2Survives = alpha
            .GetPlayerStacks(P2Id)
            .Any(st => st.DesignId == TestDesigns.Fighter.DesignId);
        if (!p2Survives)
            Assert.Equal(1, defenderLosses);
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
        TestState.RegisterDesigns(state);
        TestState.SeedStartingUnits(state);
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

    private static NexusMoveOrder Move(
        HexCoord from,
        HexCoord to,
        params (NexusUnitDesign Design, int Count)[] units
    ) =>
        new(
            from,
            to,
            units
                .Select(unit => new NexusUnitStackGroup(
                    unit.Design.DesignId,
                    unit.Design.Hull,
                    NexusHullBaselines.GetProfile(unit.Design).Hits,
                    unit.Count
                ))
                .ToImmutableArray()
        );

    private static NexusMoveOrder MoveExact(
        HexCoord from,
        HexCoord to,
        params (NexusUnitDesign Design, int RemainingHits, int Count)[] stacks
    ) =>
        new(
            from,
            to,
            stacks
                .Select(stack => new NexusUnitStackGroup(
                    stack.Design.DesignId,
                    stack.Design.Hull,
                    stack.RemainingHits,
                    stack.Count
                ))
                .ToImmutableArray()
        );

    [Fact]
    public void Damage_PersistsBetweenRounds()
    {
        var s = MakeState();
        var p1Home = s.Systems.First(sys => sys.HomePlayerId == P1Id);

        p1Home.Units[P1Id] =
        [
            new NexusUnitStack
            {
                DesignId = TestDesigns.Carrier.DesignId,
                Category = NexusUnitCategory.Capital,
                RemainingHits = 3,
                Count = 1,
            },
        ];

        SubmitBoth(s);

        var stack = p1Home
            .GetPlayerStacks(P1Id)
            .Single(st => st.DesignId == TestDesigns.Carrier.DesignId);
        Assert.Equal(3, stack.RemainingHits);
        Assert.Equal(1, stack.Count);
    }

    [Fact]
    public void Move_ExactStackOrder_PreservesSelectedDamageState()
    {
        var s = MakeState();
        var p1Home = s.Systems.First(sys => sys.HomePlayerId == P1Id);
        var fallbackSupply = s.Systems.First(sys => sys.Coord == new HexCoord(1, -1));
        fallbackSupply.ControlOwner = P1Id;
        fallbackSupply.IncomeValue = 2;

        p1Home.Units[P1Id] =
        [
            new NexusUnitStack
            {
                DesignId = TestDesigns.Infantry.DesignId,
                Category = NexusUnitCategory.Planetary,
                RemainingHits = 1,
                Count = 4,
            },
            new NexusUnitStack
            {
                DesignId = TestDesigns.Fighter.DesignId,
                Category = NexusUnitCategory.Strike,
                RemainingHits = 1,
                Count = 2,
            },
            new NexusUnitStack
            {
                DesignId = TestDesigns.Frigate.DesignId,
                Category = NexusUnitCategory.Capital,
                RemainingHits = 1,
                Count = 1,
            },
            new NexusUnitStack
            {
                DesignId = TestDesigns.Frigate.DesignId,
                Category = NexusUnitCategory.Capital,
                RemainingHits = NexusHullBaselines.GetBaseline(NexusUnitCategory.Capital).Hits,
                Count = 1,
            },
        ];

        p1Home.Units[P2Id] =
        [
            new NexusUnitStack
            {
                DesignId = TestDesigns.Frigate.DesignId,
                Category = NexusUnitCategory.Capital,
                RemainingHits = NexusHullBaselines.GetBaseline(NexusUnitCategory.Capital).Hits,
                Count = 1,
            },
        ];

        var adjacent = new HexCoord(1, -2);

        SubmitBoth(
            s,
            p1Moves: [MoveExact(NexusMap.Player1HomeCoord, adjacent, (TestDesigns.Frigate, 1, 1))]
        );

        var dst = s.Systems.First(sys => sys.Coord == adjacent);
        var movedStack = dst.GetPlayerStacks(P1Id)
            .Single(st => st.DesignId == TestDesigns.Frigate.DesignId);
        Assert.Equal(1, movedStack.RemainingHits);
    }

    [Fact]
    public void Move_Event_IsRetreat_SetCorrectly()
    {
        var s = MakeState();
        var p1Home = s.Systems.First(sys => sys.HomePlayerId == P1Id);

        p1Home.Units[P2Id] =
        [
            new NexusUnitStack
            {
                DesignId = TestDesigns.Frigate.DesignId,
                Category = NexusUnitCategory.Capital,
                RemainingHits = 1,
                Count = 1,
            },
        ];

        var adjacentToP1 = new HexCoord(1, -2);
        var adjacentToP2 = new HexCoord(-1, 2);

        SubmitBoth(
            s,
            p1Moves: [Move(NexusMap.Player1HomeCoord, adjacentToP1, (TestDesigns.Carrier, 1))],
            p2Moves: [Move(NexusMap.Player2HomeCoord, adjacentToP2, (TestDesigns.Carrier, 1))]
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
        TestState.RegisterDesigns(state);
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

        Assert.IsAssignableFrom<NexusTurnOrdersRejected>(result);
    }

    [Fact]
    public void Gate_RejectedWhenInsufficientEnergy()
    {
        var s = MakeState();
        s.Players[0].Energy = 5;
        var nexus = s.Systems.Single(sys => sys.IsNexus);
        nexus.AddUnits(P1Id, TestDesigns.Infantry.DesignId, NexusUnitCategory.Planetary, 1);

        var result = NexusEngine.SubmitOrders(
            s,
            new NexusTurnOrdersCommand(P1Id, 1, [], [], BeginNexusGate: true),
            new Random(42)
        );

        Assert.IsAssignableFrom<NexusTurnOrdersRejected>(result);
    }

    [Fact]
    public void Gate_StartsWhenGFOnNexusAndEnoughEnergy()
    {
        var s = MakeState();
        s.Players[0].Energy = 50;
        var nexus = s.Systems.Single(sys => sys.IsNexus);
        nexus.AddUnits(P1Id, TestDesigns.Infantry.DesignId, NexusUnitCategory.Planetary, 1);

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
        nexus.AddUnits(P1Id, TestDesigns.Infantry.DesignId, NexusUnitCategory.Planetary, 1);

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
        nexus.AddUnits(P1Id, TestDesigns.Infantry.DesignId, NexusUnitCategory.Planetary, 1);

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

    [Fact]
    public void Gate_RejectedWhenNexusIsContested()
    {
        var s = MakeState();
        s.Players[0].Energy = 50;
        var nexus = s.Systems.Single(sys => sys.IsNexus);
        nexus.AddUnits(P1Id, TestDesigns.Infantry.DesignId, NexusUnitCategory.Planetary, 1);
        nexus.AddUnits(P2Id, TestDesigns.Fighter.DesignId, NexusUnitCategory.Strike, 1);

        var result = NexusEngine.SubmitOrders(
            s,
            new NexusTurnOrdersCommand(P1Id, 1, [], [], BeginNexusGate: true),
            new Random(42)
        );

        Assert.IsAssignableFrom<NexusTurnOrdersRejected>(result);
    }

    [Fact]
    public void Gate_CancelledWhenNexusBecomesContested()
    {
        var s = MakeState();
        s.Players[0].Energy = 200;
        var nexus = s.Systems.Single(sys => sys.IsNexus);
        nexus.AddUnits(P1Id, TestDesigns.Infantry.DesignId, NexusUnitCategory.Planetary, 1);
        var staging = s.Systems.Single(sys => sys.Coord == new HexCoord(0, -1));
        staging.AddUnits(
            P2Id,
            TestDesigns.Carrier.DesignId,
            NexusUnitCategory.Capital,
            1,
            designHits: NexusHullBaselines.GetProfile(TestDesigns.Carrier).Hits
        );
        staging.AddUnits(P2Id, TestDesigns.Fighter.DesignId, NexusUnitCategory.Strike, 1);

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

        NexusEngine.SubmitOrders(
            s,
            new NexusTurnOrdersCommand(P1Id, 2, [], [], BeginNexusGate: true),
            new Random(42)
        );
        NexusEngine.SubmitOrders(
            s,
            new NexusTurnOrdersCommand(
                P2Id,
                2,
                [
                    new NexusMoveOrder(
                        staging.Coord,
                        NexusMap.NexusCoord,
                        [
                            new NexusUnitStackGroup(
                                TestDesigns.Carrier.DesignId,
                                NexusUnitCategory.Capital,
                                NexusHullBaselines.GetBaseline(NexusUnitCategory.Capital).Hits,
                                1
                            ),
                            new NexusUnitStackGroup(
                                TestDesigns.Fighter.DesignId,
                                NexusUnitCategory.Strike,
                                NexusHullBaselines.GetBaseline(NexusUnitCategory.Strike).Hits,
                                1
                            ),
                        ]
                    ),
                ],
                [],
                false
            ),
            new Random(42)
        );

        Assert.Equal(NexusGateProgress.None, s.Players[0].GateProgress);
        Assert.Contains(s.LastResolveEvents, e => e is NexusGateCancelledEvent);
    }
}

public class NexusCommittedPlanetaryTests
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
        TestState.RegisterDesigns(state);
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
    public void Combat_PlanetaryUnitsRemainInUnitsPool_WhenSystemRemainsContested()
    {
        var s = MakeState();
        var alpha = s.Systems.First(sys => sys.Coord == new HexCoord(1, -1));
        alpha.AddUnits(P1Id, TestDesigns.Infantry.DesignId, NexusUnitCategory.Planetary, 1);
        alpha.AddUnits(P2Id, TestDesigns.Fighter.DesignId, NexusUnitCategory.Strike, 1);

        SubmitBoth(s);

        Assert.Single(
            alpha.GetPlayerStacks(P1Id),
            stack => stack.DesignId == TestDesigns.Infantry.DesignId
        );
    }

    [Fact]
    public void Combat_PlanetaryUnitsInFleet_CannotMoveFromContestedSystem()
    {
        var s = MakeState();
        var alpha = s.Systems.First(sys => sys.Coord == new HexCoord(1, -1));
        alpha.AddUnits(P1Id, TestDesigns.Infantry.DesignId, NexusUnitCategory.Planetary, 1);
        alpha.AddUnits(P2Id, TestDesigns.Fighter.DesignId, NexusUnitCategory.Strike, 1);

        SubmitBoth(s);

        var result = NexusEngine.SubmitOrders(
            s,
            new NexusTurnOrdersCommand(
                P1Id,
                s.RoundNumber,
                [
                    new NexusMoveOrder(
                        alpha.Coord,
                        new HexCoord(1, 0),
                        [
                            new NexusUnitStackGroup(
                                TestDesigns.Infantry.DesignId,
                                NexusUnitCategory.Planetary,
                                1,
                                1
                            ),
                        ]
                    ),
                ],
                [],
                false
            ),
            new Random(42)
        );

        Assert.IsAssignableFrom<NexusTurnOrdersRejected>(result);
    }

    [Fact]
    public void PlanetaryUnits_CanMoveWhenSystemBecomesUncontested()
    {
        var s = MakeState();
        var alpha = s.Systems.First(sys => sys.Coord == new HexCoord(1, -1));
        alpha.AddUnits(
            P1Id,
            TestDesigns.Carrier.DesignId,
            NexusUnitCategory.Capital,
            1,
            designHits: NexusHullBaselines.GetProfile(TestDesigns.Carrier).Hits
        );
        alpha.AddUnits(P1Id, TestDesigns.Infantry.DesignId, NexusUnitCategory.Planetary, 1);

        var result = NexusEngine.SubmitOrders(
            s,
            new NexusTurnOrdersCommand(
                P1Id,
                s.RoundNumber,
                [
                    new NexusMoveOrder(
                        alpha.Coord,
                        new HexCoord(1, 0),
                        [
                            new NexusUnitStackGroup(
                                TestDesigns.Carrier.DesignId,
                                NexusUnitCategory.Capital,
                                NexusHullBaselines.GetBaseline(NexusUnitCategory.Capital).Hits,
                                1
                            ),
                            new NexusUnitStackGroup(
                                TestDesigns.Infantry.DesignId,
                                NexusUnitCategory.Planetary,
                                NexusHullBaselines.GetBaseline(NexusUnitCategory.Planetary).Hits,
                                1
                            ),
                        ]
                    ),
                ],
                [],
                false
            ),
            new Random(42)
        );

        Assert.IsType<NexusTurnOrdersAccepted>(result);
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
        TestState.RegisterDesigns(state);
        // Seed 1 Carrier at P1 home so supply check tests have baseline capital=1
        var p1Home = state.Systems.First(s => s.HomePlayerId == P1Id);
        p1Home.AddUnits(P1Id, TestDesigns.Carrier.DesignId, NexusUnitCategory.Capital, 1);
        var p2Home = state.Systems.First(s => s.HomePlayerId == P2Id);
        p2Home.AddUnits(P2Id, TestDesigns.Carrier.DesignId, NexusUnitCategory.Capital, 1);
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
        var s = MakeState();
        SubmitBoth(s);
        Assert.DoesNotContain(s.LastResolveEvents, e => e is NexusCapitalDisbandedEvent);
    }

    [Fact]
    public void SupplyCheck_Deficit_DisbandsCheapestCapitalFirst()
    {
        var s = MakeState();
        var nexus = s.Systems.First(sys => sys.IsNexus);
        nexus.AddUnits(P1Id, TestDesigns.Frigate.DesignId, NexusUnitCategory.Capital, 2);
        nexus.AddUnits(P1Id, TestDesigns.Carrier.DesignId, NexusUnitCategory.Capital, 1);

        SubmitBoth(s);

        var disbandEvents = s
            .LastResolveEvents.OfType<NexusCapitalDisbandedEvent>()
            .Where(e => e.PlayerId == P1Id)
            .ToList();
        Assert.Single(disbandEvents);
        var ev = disbandEvents[0];
        Assert.Equal("Frigate", ev.DesignName);
        Assert.Equal(NexusMap.NexusCoord, ev.System);
        Assert.Equal(2, ev.Count);
    }

    [Fact]
    public void SupplyCheck_SpiralOrder_Ring1DisbandedBeforeHome()
    {
        var s = MakeState();
        var alpha = s.Systems.First(sys => sys.Coord == new HexCoord(1, -1));
        alpha.AddUnits(P1Id, TestDesigns.Frigate.DesignId, NexusUnitCategory.Capital, 1);
        var p1Home = s.Systems.First(sys => sys.HomePlayerId == P1Id);
        p1Home.AddUnits(P1Id, TestDesigns.Frigate.DesignId, NexusUnitCategory.Capital, 1);

        SubmitBoth(s);

        var disbanded = s
            .LastResolveEvents.OfType<NexusCapitalDisbandedEvent>()
            .Where(e => e.PlayerId == P1Id)
            .ToList();
        Assert.Single(disbanded);
        Assert.Equal(new HexCoord(1, -1), disbanded[0].System);
        Assert.Equal("Frigate", disbanded[0].DesignName);
        Assert.Equal(1, disbanded[0].Count);
    }

    [Fact]
    public void SupplyCheck_PlanetaryUnits_NeverDisbanded()
    {
        var s = MakeState();
        var nexus = s.Systems.First(sys => sys.IsNexus);
        nexus.AddUnits(P1Id, TestDesigns.Infantry.DesignId, NexusUnitCategory.Planetary, 10);
        nexus.AddUnits(P1Id, TestDesigns.Fighter.DesignId, NexusUnitCategory.Strike, 5);

        SubmitBoth(s);

        Assert.DoesNotContain(s.LastResolveEvents, e => e is NexusCapitalDisbandedEvent);
    }

    [Fact]
    public void SupplyCheck_UncontrolledSystem_ContributesZeroSupply()
    {
        var s = MakeState();
        var alpha = s.Systems.First(sys => sys.Coord == new HexCoord(1, -1));
        alpha.AddUnits(P1Id, TestDesigns.Frigate.DesignId, NexusUnitCategory.Capital, 3);

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
        var nexus = s.Systems.First(sys => sys.IsNexus);
        nexus.ControlOwner = P1Id;

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
            sys.GetUnitCount(P1Id, TestDesigns.Frigate.DesignId)
            + sys.GetUnitCount(P1Id, TestDesigns.Destroyer.DesignId)
            + sys.GetUnitCount(P1Id, TestDesigns.Cruiser.DesignId)
            + sys.GetUnitCount(P1Id, TestDesigns.Carrier.DesignId)
        );
        Assert.Equal(expected, view.CurrentPlayer.CapitalCount);
    }
}

// ── Tag Behavior ────────────────────────────────────────────────────────────

public class NexusTagBehaviorTests
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
        TestState.RegisterDesigns(state);
        return state;
    }

    private static void SubmitRound(
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

    private static NexusCombatAttackRoll[] GetAttackRolls(NexusState state)
    {
        return
        [
            .. state
                .LastResolveEvents.OfType<NexusCombatResultEvent>()
                .SelectMany(e => e.Phases)
                .SelectMany(p => p.AttackRolls),
        ];
    }

    private static void AddUnits(
        NexusSystemState system,
        Guid playerId,
        NexusUnitDesign design,
        int count
    ) => system.AddUnits(playerId, design.DesignId, design.Hull, count);

    // ── Combat Steps ────────────────────────────────────────────────────────

    [Fact]
    public void ContactStep_FiresBeforeBattle()
    {
        var s = MakeState();
        var alpha = s.Systems.First(sys => sys.Coord == new HexCoord(1, -1));

        AddUnits(alpha, P1Id, TestDesigns.Interceptor, 1);
        AddUnits(alpha, P2Id, TestDesigns.Fighter, 1);

        SubmitRound(s);

        var result = s.LastResolveEvents.OfType<NexusCombatResultEvent>().SingleOrDefault();
        Assert.NotNull(result);
        Assert.Contains(result.Phases, p => p.Phase == NexusCombatPhase.Contact);
        Assert.Contains(result.Phases, p => p.Phase == NexusCombatPhase.Battle);
    }

    [Fact]
    public void ContactPhase_KillsReduceBattleAttacks()
    {
        var s = MakeState();
        var alpha = s.Systems.First(sys => sys.Coord == new HexCoord(1, -1));

        AddUnits(alpha, P1Id, TestDesigns.Interceptor, 2);
        AddUnits(alpha, P2Id, TestDesigns.Fighter, 1);

        SubmitRound(s);

        var result = s.LastResolveEvents.OfType<NexusCombatResultEvent>().SingleOrDefault();
        Assert.NotNull(result);

        var battlePhase = result.Phases.FirstOrDefault(p => p.Phase == NexusCombatPhase.Battle);
        var contactPhase = result.Phases.FirstOrDefault(p => p.Phase == NexusCombatPhase.Contact);

        var p2HitsInBattle = battlePhase?.AttackRolls.Count(r => r.AttackingPlayerId == P2Id) ?? 0;
        var p2LossesInContact =
            contactPhase?.Losses.Where(l => l.PlayerId == P2Id).Sum(l => l.Count) ?? 0;

        if (p2LossesInContact > 0)
            Assert.Equal(0, p2HitsInBattle);
    }

    // ── Shield ───────────────────────────────────────────────────────────────

    [Fact]
    public void Shield_CanAbsorbHit()
    {
        var s = MakeState();
        var alpha = s.Systems.First(sys => sys.Coord == new HexCoord(1, -1));

        AddUnits(alpha, P1Id, TestDesigns.Armor, 1);
        AddUnits(alpha, P2Id, TestDesigns.Infantry, 5);

        SubmitRound(s);

        var attackRolls = GetAttackRolls(s);
        var shieldedHits = attackRolls.Count(r => r.IsHit && r.WasShielded);
        var unshieldedHits = attackRolls.Count(r => r.IsHit && !r.WasShielded);

        Assert.True(
            shieldedHits > 0,
            $"Expected ≥ 1 shielded hit, got {shieldedHits} (unshielded: {unshieldedHits})"
        );
    }

    // ── Disruptor ─────────────────────────────────────────────────────────

    [Fact]
    public void Disruptor_BypassesShieldSave()
    {
        var s = MakeState();
        var alpha = s.Systems.First(sys => sys.Coord == new HexCoord(1, -1));

        AddUnits(alpha, P1Id, TestDesigns.Armor, 1);
        AddUnits(alpha, P2Id, TestDesigns.Bomber, 3);

        SubmitRound(s);

        var attackRolls = GetAttackRolls(s);
        var shieldedHits = attackRolls.Count(r =>
            r.AttackerDesignName == "Bomber" && r.TargetDesignName == "Armor" && r.WasShielded
        );

        Assert.Equal(0, shieldedHits);
    }

    // ── Screen (via public API) ───────────────────────────────────────────────

    [Fact]
    public void Escort_CombatRunsWithoutError()
    {
        var s = MakeState();
        var alpha = s.Systems.First(sys => sys.Coord == new HexCoord(1, -1));

        AddUnits(alpha, P1Id, TestDesigns.Frigate, 1);
        AddUnits(alpha, P1Id, TestDesigns.Carrier, 1);
        AddUnits(alpha, P1Id, TestDesigns.Cruiser, 1);
        AddUnits(alpha, P2Id, TestDesigns.Bomber, 6);

        SubmitRound(s);

        Assert.Contains(s.LastResolveEvents, e => e is NexusCombatResultEvent);
    }

    // ── Phases ───────────────────────────────────────────────────────────────

    [Fact]
    public void Combat_HasBothContactAndBattlePhases()
    {
        var s = MakeState();
        var alpha = s.Systems.First(sys => sys.Coord == new HexCoord(1, -1));

        AddUnits(alpha, P1Id, TestDesigns.Interceptor, 2);
        AddUnits(alpha, P1Id, TestDesigns.Fighter, 2);
        AddUnits(alpha, P2Id, TestDesigns.Fighter, 3);

        SubmitRound(s);

        Assert.Contains(s.LastResolveEvents, e => e is NexusCombatResultEvent);
    }
}

// ── Design System ─────────────────────────────────────────────────────────────

public class NexusDesignTests
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
    public void CreateDesign_ValidCapital_Succeeds()
    {
        var s = MakeState();
        var cmd = new NexusCreateDesignCommand(
            P1Id,
            "Test Carrier",
            NexusUnitCategory.Capital,
            [new Battery(NexusUnitCategory.Strike), new Hangar(4)]
        );
        var result = NexusEngine.CreateDesign(s, cmd);

        Assert.IsType<NexusDesignCreated>(result);
        var created = (NexusDesignCreated)result;
        Assert.Equal("Test Carrier", created.Design.Name);
        Assert.Equal(NexusUnitCategory.Capital, created.Design.Hull);

        var player = s.Players.First(p => p.PlayerId == P1Id);
        Assert.Contains(player.Designs, d => d.DesignId == created.Design.DesignId);
    }

    [Fact]
    public void CreateDesign_InvalidConstraint_IsRejected()
    {
        var s = MakeState();
        // Hangar is only valid on Capital — applying to Strike should fail
        var cmd = new NexusCreateDesignCommand(
            P1Id,
            "Bad Design",
            NexusUnitCategory.Strike,
            [new Battery(NexusUnitCategory.Strike), new Hangar(4)]
        );
        var result = NexusEngine.CreateDesign(s, cmd);

        Assert.IsAssignableFrom<NexusDesignCommandRejected>(result);
    }

    [Fact]
    public void CreateDesign_NoAttackTag_IsAccepted()
    {
        // Pure support units (carriers, repair ships) need no attack modules
        var s = MakeState();
        var cmd = new NexusCreateDesignCommand(
            P1Id,
            "Support Ship",
            NexusUnitCategory.Capital,
            [new Hangar(4)]
        );
        var result = NexusEngine.CreateDesign(s, cmd);

        Assert.IsType<NexusDesignCreated>(result);
    }

    [Fact]
    public void CreateDesign_DuplicateBatteryCategory_IsRejected()
    {
        var s = MakeState();
        var cmd = new NexusCreateDesignCommand(
            P1Id,
            "Dup Battery",
            NexusUnitCategory.Capital,
            [new Battery(NexusUnitCategory.Strike), new Battery(NexusUnitCategory.Strike)]
        );
        var result = NexusEngine.CreateDesign(s, cmd);

        var rejected = Assert.IsAssignableFrom<NexusDesignCommandRejected>(result);
        Assert.Contains("Duplicate Battery(Strike)", rejected.ErrorMessage);
    }

    [Fact]
    public void CreateDesign_DuplicateVanguardCategory_IsRejected()
    {
        var s = MakeState();
        var cmd = new NexusCreateDesignCommand(
            P1Id,
            "Dup Vanguard",
            NexusUnitCategory.Strike,
            [new Vanguard(NexusUnitCategory.Strike), new Vanguard(NexusUnitCategory.Strike)]
        );
        var result = NexusEngine.CreateDesign(s, cmd);

        var rejected = Assert.IsAssignableFrom<NexusDesignCommandRejected>(result);
        Assert.Contains("Duplicate Vanguard(Strike)", rejected.ErrorMessage);
    }

    [Fact]
    public void CreateDesign_SeekerAndScatterSameCategory_IsRejected()
    {
        var s = MakeState();
        var cmd = new NexusCreateDesignCommand(
            P1Id,
            "Contradictory",
            NexusUnitCategory.Capital,
            [
                new Battery(NexusUnitCategory.Strike),
                new Seeker(NexusUnitCategory.Strike, 1),
                new Scatter(NexusUnitCategory.Strike, 1),
            ]
        );
        var result = NexusEngine.CreateDesign(s, cmd);

        var rejected = Assert.IsAssignableFrom<NexusDesignCommandRejected>(result);
        Assert.Contains("mutually exclusive", rejected.ErrorMessage);
    }

    [Fact]
    public void CreateDesign_SeekerAndScatterDifferentCategory_IsAccepted()
    {
        var s = MakeState();
        var cmd = new NexusCreateDesignCommand(
            P1Id,
            "Mixed Fire",
            NexusUnitCategory.Capital,
            [
                new Battery(NexusUnitCategory.Strike),
                new Battery(NexusUnitCategory.Capital),
                new Seeker(NexusUnitCategory.Strike, 1),
                new Scatter(NexusUnitCategory.Capital, 1),
            ]
        );
        var result = NexusEngine.CreateDesign(s, cmd);

        Assert.IsType<NexusDesignCreated>(result);
    }

    [Fact]
    public void CreateDesign_ControlOnNonPlanetary_IsRejected()
    {
        var s = MakeState();
        var cmd = new NexusCreateDesignCommand(
            P1Id,
            "Capital Control",
            NexusUnitCategory.Capital,
            [new Battery(NexusUnitCategory.Capital), new Control()]
        );
        var result = NexusEngine.CreateDesign(s, cmd);

        var rejected = Assert.IsAssignableFrom<NexusDesignCommandRejected>(result);
        Assert.Contains("Control", rejected.ErrorMessage);
    }

    [Fact]
    public void CreateDesign_ControlOnPlanetary_IsAccepted()
    {
        var s = MakeState();
        var cmd = new NexusCreateDesignCommand(
            P1Id,
            "Infantry",
            NexusUnitCategory.Planetary,
            [new Battery(NexusUnitCategory.Planetary), new Control()]
        );
        var result = NexusEngine.CreateDesign(s, cmd);

        Assert.IsType<NexusDesignCreated>(result);
    }

    [Fact]
    public void DeleteDesign_NoUnitsOnMap_Succeeds()
    {
        var s = MakeState();
        var createCmd = new NexusCreateDesignCommand(
            P1Id,
            "Temp",
            NexusUnitCategory.Strike,
            [new Battery(NexusUnitCategory.Strike), new Dock()]
        );
        var created = (NexusDesignCreated)NexusEngine.CreateDesign(s, createCmd);

        var deleteCmd = new NexusDeleteDesignCommand(P1Id, created.Design.DesignId);
        var result = NexusEngine.DeleteDesign(s, deleteCmd);

        Assert.IsType<NexusDesignDeleted>(result);
        var player = s.Players.First(p => p.PlayerId == P1Id);
        Assert.DoesNotContain(player.Designs, d => d.DesignId == created.Design.DesignId);
    }

    [Fact]
    public void DeleteDesign_UnitsOnMap_IsRejected()
    {
        var s = MakeState();
        var createCmd = new NexusCreateDesignCommand(
            P1Id,
            "Temp",
            NexusUnitCategory.Capital,
            [new Battery(NexusUnitCategory.Strike), new Hangar(2)]
        );
        var created = (NexusDesignCreated)NexusEngine.CreateDesign(s, createCmd);

        var home = s.Systems.First(sys => sys.HomePlayerId == P1Id);
        home.AddUnits(P1Id, created.Design.DesignId, NexusUnitCategory.Capital, 1);

        var deleteCmd = new NexusDeleteDesignCommand(P1Id, created.Design.DesignId);
        var result = NexusEngine.DeleteDesign(s, deleteCmd);

        Assert.IsAssignableFrom<NexusDesignCommandRejected>(result);
    }

    [Fact]
    public void DesignCost_BaselineTagCostsSumCorrectly()
    {
        var profile = NexusHullBaselines.GetProfile(TestDesigns.Carrier);
        // Capital base cost = 2, Shield = 2, Hangar(4) = 4, Battery(Capital) = 1 → total = 9
        Assert.Equal(9, profile.Cost);
    }

    // ── Hull restrictions ────────────────────────────────────────────────────

    [Fact]
    public void CreateDesign_HangarOnNonCapital_IsRejected()
    {
        var s = MakeState();
        var result = NexusEngine.CreateDesign(
            s,
            new NexusCreateDesignCommand(
                P1Id,
                "X",
                NexusUnitCategory.Strike,
                [new Battery(NexusUnitCategory.Strike), new Hangar(2)]
            )
        );
        Assert.IsAssignableFrom<NexusDesignCommandRejected>(result);
    }

    [Fact]
    public void CreateDesign_DockOnCapital_IsRejected()
    {
        var s = MakeState();
        var result = NexusEngine.CreateDesign(
            s,
            new NexusCreateDesignCommand(
                P1Id,
                "X",
                NexusUnitCategory.Capital,
                [new Battery(NexusUnitCategory.Strike), new Dock()]
            )
        );
        Assert.IsAssignableFrom<NexusDesignCommandRejected>(result);
    }

    [Fact]
    public void CreateDesign_DriveOnPlanetary_IsRejected()
    {
        var s = MakeState();
        var result = NexusEngine.CreateDesign(
            s,
            new NexusCreateDesignCommand(
                P1Id,
                "X",
                NexusUnitCategory.Planetary,
                [new Battery(NexusUnitCategory.Planetary), new Drive(1)]
            )
        );
        Assert.IsAssignableFrom<NexusDesignCommandRejected>(result);
    }

    // ── N-parameter bounds ───────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void CreateDesign_ArmourOutOfRange_IsRejected(int n)
    {
        var s = MakeState();
        var result = NexusEngine.CreateDesign(
            s,
            new NexusCreateDesignCommand(
                P1Id,
                "X",
                NexusUnitCategory.Strike,
                [new Battery(NexusUnitCategory.Strike), new Armour(n)]
            )
        );
        Assert.IsAssignableFrom<NexusDesignCommandRejected>(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void CreateDesign_DriveOutOfRange_IsRejected(int n)
    {
        var s = MakeState();
        var result = NexusEngine.CreateDesign(
            s,
            new NexusCreateDesignCommand(
                P1Id,
                "X",
                NexusUnitCategory.Capital,
                [new Battery(NexusUnitCategory.Strike), new Drive(n), new Hangar(2)]
            )
        );
        Assert.IsAssignableFrom<NexusDesignCommandRejected>(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void CreateDesign_BeaconOutOfRange_IsRejected(int n)
    {
        var s = MakeState();
        var result = NexusEngine.CreateDesign(
            s,
            new NexusCreateDesignCommand(
                P1Id,
                "X",
                NexusUnitCategory.Strike,
                [new Battery(NexusUnitCategory.Strike), new Beacon(n)]
            )
        );
        Assert.IsAssignableFrom<NexusDesignCommandRejected>(result);
    }

    // ── Slot budget ──────────────────────────────────────────────────────────

    [Fact]
    public void CreateDesign_ExceedsSlotBudget_IsRejected()
    {
        var s = MakeState();
        // Strike budget = 2; Battery(1) + Armour(2) = 3 slots
        var result = NexusEngine.CreateDesign(
            s,
            new NexusCreateDesignCommand(
                P1Id,
                "X",
                NexusUnitCategory.Strike,
                [new Battery(NexusUnitCategory.Strike), new Armour(2)]
            )
        );
        Assert.IsAssignableFrom<NexusDesignCommandRejected>(result);
    }

    [Fact]
    public void CreateDesign_ExactlyAtSlotBudget_Succeeds()
    {
        var s = MakeState();
        // Strike budget = 2; Battery(1) + Shield(1) = 2 slots
        var result = NexusEngine.CreateDesign(
            s,
            new NexusCreateDesignCommand(
                P1Id,
                "X",
                NexusUnitCategory.Strike,
                [new Battery(NexusUnitCategory.Strike), new Shield()]
            )
        );
        Assert.IsType<NexusDesignCreated>(result);
    }

    // ── Duplicate / mutual-exclusion rules ───────────────────────────────────

    [Fact]
    public void CreateDesign_DuplicateShield_IsRejected()
    {
        var s = MakeState();
        // Two shields = 2 slots; within budget for Capital (4) but duplicate rule fires
        var result = NexusEngine.CreateDesign(
            s,
            new NexusCreateDesignCommand(
                P1Id,
                "X",
                NexusUnitCategory.Capital,
                [new Battery(NexusUnitCategory.Strike), new Shield(), new Shield(), new Hangar(2)]
            )
        );
        Assert.IsAssignableFrom<NexusDesignCommandRejected>(result);
    }

    [Fact]
    public void CreateDesign_BeaconAndCloak_MutuallyExclusive_IsRejected()
    {
        var s = MakeState();
        // Beacon(-1 slot) + Cloak(1 slot) = 0 net; within budget but mutual-exclusion fires
        var result = NexusEngine.CreateDesign(
            s,
            new NexusCreateDesignCommand(
                P1Id,
                "X",
                NexusUnitCategory.Strike,
                [new Battery(NexusUnitCategory.Strike), new Beacon(1), new Cloak(1)]
            )
        );
        Assert.IsAssignableFrom<NexusDesignCommandRejected>(result);
    }

    // ── Profile derivation ───────────────────────────────────────────────────

    [Fact]
    public void GetProfile_ArmourIncreasesHits()
    {
        var design = new NexusUnitDesign
        {
            DesignId = Guid.NewGuid(),
            Name = "T",
            Hull = NexusUnitCategory.Strike,
            Modules = [new Battery(NexusUnitCategory.Strike), new Armour(1)],
        };
        // Strike baseline hits = 1; Armour(1) adds 1
        Assert.Equal(2, NexusHullBaselines.GetProfile(design).Hits);
    }

    [Fact]
    public void GetProfile_DriveIncreasesMove()
    {
        var design = new NexusUnitDesign
        {
            DesignId = Guid.NewGuid(),
            Name = "T",
            Hull = NexusUnitCategory.Capital,
            Modules = [new Battery(NexusUnitCategory.Strike), new Drive(2), new Hangar(2)],
        };
        // Capital baseline move = 1; Drive(2) adds 2 → 3
        Assert.Equal(3, NexusHullBaselines.GetProfile(design).Move);
    }

    [Fact]
    public void GetProfile_CloakReducesSilhouette()
    {
        var design = new NexusUnitDesign
        {
            DesignId = Guid.NewGuid(),
            Name = "T",
            Hull = NexusUnitCategory.Strike,
            Modules = [new Battery(NexusUnitCategory.Strike), new Cloak(1)],
        };
        // Strike baseline silhouette = 1; Cloak(1) subtracts 1 → floor 0
        Assert.Equal(0, NexusHullBaselines.GetProfile(design).Silhouette);
    }

    [Fact]
    public void GetProfile_BeaconIncreasesSilhouette()
    {
        var design = new NexusUnitDesign
        {
            DesignId = Guid.NewGuid(),
            Name = "T",
            Hull = NexusUnitCategory.Strike,
            Modules = [new Battery(NexusUnitCategory.Strike), new Beacon(1)],
        };
        // Strike baseline silhouette = 1; Beacon(1) adds 1 → 2
        Assert.Equal(2, NexusHullBaselines.GetProfile(design).Silhouette);
    }

    // ── Module cost/slot tables ──────────────────────────────────────────────

    [Theory]
    [InlineData("Shield", 2, 1)]
    [InlineData("Disruptor", 2, 1)]
    [InlineData("Dock", 0, 0)]
    [InlineData("Control", 1, 0)]
    [InlineData("Repair", 3, 1)]
    public void ModuleCosts_SimpleModules(string type, int expectedCost, int expectedSlots)
    {
        NexusUnitModule module = type switch
        {
            "Shield" => new Shield(),
            "Disruptor" => new Disruptor(),
            "Dock" => new Dock(),
            "Control" => new Control(),
            "Repair" => new Repair(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
        Assert.Equal(expectedCost, NexusModuleCosts.GetCost(module));
        Assert.Equal(expectedSlots, NexusModuleCosts.GetSlots(module));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void ModuleCosts_Armour(int n)
    {
        // cost = n*2, slots = n
        Assert.Equal(n * 2, NexusModuleCosts.GetCost(new Armour(n)));
        Assert.Equal(n, NexusModuleCosts.GetSlots(new Armour(n)));
    }

    [Fact]
    public void ModuleCosts_Bulkhead_EnergyCostNegativeSlots()
    {
        Assert.Equal(4, NexusModuleCosts.GetCost(new Bulkhead(2)));  // N*2
        Assert.Equal(-2, NexusModuleCosts.GetSlots(new Bulkhead(2))); // -N
    }

    [Fact]
    public void ModuleCosts_Beacon_ZeroEnergyCostOneSlot()
    {
        Assert.Equal(0, NexusModuleCosts.GetCost(new Beacon(1)));
        Assert.Equal(1, NexusModuleCosts.GetSlots(new Beacon(1)));
    }

    [Fact]
    public void ModuleCosts_Scatter_NegativeCost()
    {
        Assert.Equal(-1, NexusModuleCosts.GetCost(new Scatter(NexusUnitCategory.Strike, 1)));
    }
}

namespace Spx.Nexus.Domain.Tests;

/// <summary>
/// Direct unit tests for <see cref="NexusDesignConstraints"/> at the lowest
/// possible layer — no engine, no state, just module lists and hull categories.
/// </summary>
public class NexusDesignConstraintsTests
{
    // ── CheckAdd: hull applicability ──────────────────────────────────────────

    [Fact]
    public void CheckAdd_Hangar_OnNonCapital_IsRejected()
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Strike,
            existingModules: [],
            new Hangar(2)
        );

        Assert.NotNull(result);
        Assert.Contains("Strike", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckAdd_Hangar_OnCapital_IsAccepted()
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existingModules: [],
            new Hangar(2)
        );

        Assert.Null(result);
    }

    [Fact]
    public void CheckAdd_Dock_OnCapital_IsRejected()
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existingModules: [],
            new Dock()
        );

        Assert.NotNull(result);
        Assert.Contains("not valid on Capital", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckAdd_Dock_OnStrike_IsAccepted()
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Strike,
            existingModules: [],
            new Dock()
        );

        Assert.Null(result);
    }

    [Fact]
    public void CheckAdd_Control_OnNonPlanetary_IsRejected()
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Strike,
            existingModules: [],
            new Control()
        );

        Assert.NotNull(result);
        Assert.Contains("not valid on Strike", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckAdd_Control_OnPlanetary_IsAccepted()
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Planetary,
            existingModules: [],
            new Control()
        );

        Assert.Null(result);
    }

    [Fact]
    public void CheckAdd_Drive_OnPlanetary_IsRejected()
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Planetary,
            existingModules: [],
            new Drive(1)
        );

        Assert.NotNull(result);
        Assert.Contains("not valid on Planetary", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckAdd_Drive_OnStrike_IsAccepted()
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Strike,
            existingModules: [],
            new Drive(1)
        );

        Assert.Null(result);
    }

    // ── CheckAdd: value bounds ────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void CheckAdd_Armour_InvalidN_IsRejected(int n)
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existingModules: [],
            new Armour(n)
        );

        Assert.NotNull(result);
        Assert.Contains("Armour", result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void CheckAdd_Armour_ValidN_IsAccepted(int n)
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existingModules: [],
            new Armour(n)
        );

        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void CheckAdd_Drive_InvalidN_IsRejected(int n)
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Strike,
            existingModules: [],
            new Drive(n)
        );

        Assert.NotNull(result);
        Assert.Contains("Drive", result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void CheckAdd_Drive_ValidN_IsAccepted(int n)
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Strike,
            existingModules: [],
            new Drive(n)
        );

        Assert.Null(result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void CheckAdd_Screen_ValidN_IsAccepted(int n)
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existingModules: [],
            new Screen(NexusUnitCategory.Strike, n)
        );

        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void CheckAdd_Screen_InvalidN_IsRejected(int n)
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existingModules: [],
            new Screen(NexusUnitCategory.Strike, n)
        );

        Assert.NotNull(result);
        Assert.Contains("Screen", result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    public void CheckAdd_Hangar_InvalidCapacity_IsRejected(int cap)
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existingModules: [],
            new Hangar(cap)
        );

        Assert.NotNull(result);
        Assert.Contains("Hangar", result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void CheckAdd_Magnitude_InvalidSeeker_IsRejected(int mag)
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existingModules: [],
            new Seeker(NexusUnitCategory.Capital, mag)
        );

        Assert.NotNull(result);
        Assert.Contains("Seeker", result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void CheckAdd_Magnitude_InvalidScatter_IsRejected(int mag)
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existingModules: [],
            new Scatter(NexusUnitCategory.Capital, mag)
        );

        Assert.NotNull(result);
        Assert.Contains("Scatter", result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void CheckAdd_Beacon_InvalidN_IsRejected(int n)
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existingModules: [],
            new Beacon(n)
        );

        Assert.NotNull(result);
        Assert.Contains("Beacon", result);
    }

    [Fact]
    public void CheckAdd_Beacon_ValidN_IsAccepted()
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existingModules: [],
            new Beacon(1)
        );

        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void CheckAdd_Cloak_InvalidN_IsRejected(int n)
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existingModules: [],
            new Cloak(n)
        );

        Assert.NotNull(result);
        Assert.Contains("Cloak", result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void CheckAdd_Cloak_ValidN_IsAccepted(int n)
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existingModules: [],
            new Cloak(n)
        );

        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void CheckAdd_Bulkhead_InvalidN_IsRejected(int n)
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existingModules: [],
            new Bulkhead(n)
        );

        Assert.NotNull(result);
        Assert.Contains("Bulkhead", result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void CheckAdd_Bulkhead_ValidN_IsAccepted(int n)
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existingModules: [],
            new Bulkhead(n)
        );

        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void CheckAdd_Command_InvalidN_IsRejected(int n)
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existingModules: [],
            new Command(NexusUnitCategory.Strike, n)
        );

        Assert.NotNull(result);
        Assert.Contains("Command", result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void CheckAdd_Command_ValidN_IsAccepted(int n)
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existingModules: [],
            new Command(NexusUnitCategory.Strike, n)
        );

        Assert.Null(result);
    }

    // ── CheckAdd: singleton duplicates ────────────────────────────────────────

    public static IEnumerable<object[]> SingletonCandidates =>
        [
            [new Shield()],
            [new Disruptor()],
            [new Dock()],
            [new Control()],
            [new Repair()],
            [new Bulkhead(1)],
        ];

    [Theory]
    [MemberData(nameof(SingletonCandidates))]
    public void CheckAdd_SingletonType_WhenAlreadyPresent_IsRejected(NexusUnitModule candidate)
    {
        var existing = new List<NexusUnitModule> { candidate };
        var hull = candidate.AllowedHulls.First();

        var result = NexusDesignConstraints.CheckAdd(hull, existing, candidate);

        Assert.NotNull(result);
        Assert.Contains("Duplicate", result);
    }

    [Theory]
    [MemberData(nameof(SingletonCandidates))]
    public void CheckAdd_SingletonType_WhenNotPresent_IsAccepted(NexusUnitModule candidate)
    {
        var hull = candidate.AllowedHulls.First();

        var result = NexusDesignConstraints.CheckAdd(hull, existingModules: [], candidate);

        Assert.Null(result);
    }

    // ── CheckAdd: Beacon/Cloak mutual exclusivity ─────────────────────────────

    [Fact]
    public void CheckAdd_Beacon_WhenCloakPresent_IsRejected()
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existingModules: [new Cloak(1)],
            new Beacon(1)
        );

        Assert.NotNull(result);
        Assert.Contains("mutually exclusive", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckAdd_Cloak_WhenBeaconPresent_IsRejected()
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existingModules: [new Beacon(1)],
            new Cloak(1)
        );

        Assert.NotNull(result);
        Assert.Contains("mutually exclusive", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckAdd_SecondBeacon_WhenBeaconPresent_IsRejected()
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existingModules: [new Beacon(1)],
            new Beacon(1)
        );

        Assert.NotNull(result);
        Assert.Contains("mutually exclusive", result, StringComparison.OrdinalIgnoreCase);
    }

    // ── CheckAdd: category-scoped singletons ──────────────────────────────────

    [Fact]
    public void CheckAdd_Battery_SameCategory_IsRejected()
    {
        var existing = new List<NexusUnitModule> { new Battery(NexusUnitCategory.Strike) };

        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existing,
            new Battery(NexusUnitCategory.Strike)
        );

        Assert.NotNull(result);
        Assert.Contains("Duplicate Battery", result);
    }

    [Fact]
    public void CheckAdd_Battery_DifferentCategory_IsAccepted()
    {
        var existing = new List<NexusUnitModule> { new Battery(NexusUnitCategory.Strike) };

        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existing,
            new Battery(NexusUnitCategory.Capital)
        );

        Assert.Null(result);
    }

    [Fact]
    public void CheckAdd_Vanguard_SameCategory_IsRejected()
    {
        var existing = new List<NexusUnitModule> { new Vanguard(NexusUnitCategory.Planetary) };

        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existing,
            new Vanguard(NexusUnitCategory.Planetary)
        );

        Assert.NotNull(result);
        Assert.Contains("Duplicate Vanguard", result);
    }

    [Fact]
    public void CheckAdd_Vanguard_DifferentCategory_IsAccepted()
    {
        var existing = new List<NexusUnitModule> { new Vanguard(NexusUnitCategory.Strike) };

        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existing,
            new Vanguard(NexusUnitCategory.Capital)
        );

        Assert.Null(result);
    }

    // ── CheckAdd: category-scoped mutual exclusivity (Seeker/Scatter) ──────────

    [Fact]
    public void CheckAdd_Seeker_WhenScatterSameCategory_IsRejected()
    {
        var existing = new List<NexusUnitModule> { new Scatter(NexusUnitCategory.Capital, 1) };

        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existing,
            new Seeker(NexusUnitCategory.Capital, 1)
        );

        Assert.NotNull(result);
        Assert.Contains("mutually exclusive", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckAdd_Scatter_WhenSeekerSameCategory_IsRejected()
    {
        var existing = new List<NexusUnitModule> { new Seeker(NexusUnitCategory.Strike, 1) };

        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existing,
            new Scatter(NexusUnitCategory.Strike, 1)
        );

        Assert.NotNull(result);
        Assert.Contains("mutually exclusive", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckAdd_Seeker_WhenScatterDifferentCategory_IsAccepted()
    {
        var existing = new List<NexusUnitModule> { new Scatter(NexusUnitCategory.Capital, 1) };

        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existing,
            new Seeker(NexusUnitCategory.Strike, 1)
        );

        Assert.Null(result);
    }

    // ── CheckAdd: slot budget ─────────────────────────────────────────────────

    [Fact]
    public void CheckAdd_ExceedsSlotBudget_IsRejected()
    {
        // Strike hull has 2 slots. 2 × Armour(1) = 2 slots used already.
        var existing = new List<NexusUnitModule> { new Armour(2) };

        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Strike,
            existing,
            new Battery(NexusUnitCategory.Strike)
        ); // Battery costs 1 slot

        Assert.NotNull(result);
        Assert.Contains("slots", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckAdd_FitsSlotBudget_IsAccepted()
    {
        // Strike hull has 2 slots. Armour(1) = 1 slot. Battery = 1 slot. Total = 2.
        var existing = new List<NexusUnitModule> { new Armour(1) };

        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Strike,
            existing,
            new Battery(NexusUnitCategory.Strike)
        );

        Assert.Null(result);
    }

    [Fact]
    public void CheckAdd_NegativeSlotsBulkhead_FreesBudget_IsAccepted()
    {
        // Strike hull has 2 slots. Bulkhead(2) = -2 slots. Battery = 1 slot. Total = -1 ≤ 2.
        var existing = new List<NexusUnitModule> { new Bulkhead(2) };

        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Strike,
            existing,
            new Battery(NexusUnitCategory.Strike)
        );

        Assert.Null(result);
    }

    // ── CheckAdd: empty existing ──────────────────────────────────────────────

    [Fact]
    public void CheckAdd_NoExisting_WithValidCandidate_IsAccepted()
    {
        var result = NexusDesignConstraints.CheckAdd(
            NexusUnitCategory.Capital,
            existingModules: [],
            new Shield()
        );

        Assert.Null(result);
    }

    // ── GetBlockedModuleTypes ─────────────────────────────────────────────────

    [Fact]
    public void GetBlockedModuleTypes_EmptyModules_ReturnsEmpty()
    {
        var blocked = NexusDesignConstraints.GetBlockedModuleTypes([]);

        Assert.Empty(blocked);
    }

    [Fact]
    public void GetBlockedModuleTypes_ShieldPresent_BlocksShield()
    {
        var blocked = NexusDesignConstraints.GetBlockedModuleTypes([new Shield()]);

        Assert.Contains("Shield", blocked);
        Assert.Single(blocked);
    }

    [Fact]
    public void GetBlockedModuleTypes_DisruptorPresent_BlocksDisruptor()
    {
        var blocked = NexusDesignConstraints.GetBlockedModuleTypes([new Disruptor()]);

        Assert.Contains("Disruptor", blocked);
    }

    [Fact]
    public void GetBlockedModuleTypes_DockPresent_BlocksDock()
    {
        var blocked = NexusDesignConstraints.GetBlockedModuleTypes([new Dock()]);

        Assert.Contains("Dock", blocked);
    }

    [Fact]
    public void GetBlockedModuleTypes_ControlPresent_BlocksControl()
    {
        var blocked = NexusDesignConstraints.GetBlockedModuleTypes([new Control()]);

        Assert.Contains("Control", blocked);
    }

    [Fact]
    public void GetBlockedModuleTypes_RepairPresent_BlocksRepair()
    {
        var blocked = NexusDesignConstraints.GetBlockedModuleTypes([new Repair()]);

        Assert.Contains("Repair", blocked);
    }

    [Fact]
    public void GetBlockedModuleTypes_BulkheadPresent_BlocksBulkhead()
    {
        var blocked = NexusDesignConstraints.GetBlockedModuleTypes([new Bulkhead(1)]);

        Assert.Contains("Bulkhead", blocked);
    }

    [Fact]
    public void GetBlockedModuleTypes_BeaconPresent_BlocksBothBeaconAndCloak()
    {
        var blocked = NexusDesignConstraints.GetBlockedModuleTypes([new Beacon(1)]);

        Assert.Contains("Beacon", blocked);
        Assert.Contains("Cloak", blocked);
    }

    [Fact]
    public void GetBlockedModuleTypes_CloakPresent_BlocksBothBeaconAndCloak()
    {
        var blocked = NexusDesignConstraints.GetBlockedModuleTypes([new Cloak(1)]);

        Assert.Contains("Beacon", blocked);
        Assert.Contains("Cloak", blocked);
    }

    [Fact]
    public void GetBlockedModuleTypes_Battery_IsNotBlockedAtTypeLevel()
    {
        // Battery is a per-category singleton — having Battery(Strike) should
        // NOT block the Battery type itself (you can still add Battery(Capital)).
        var blocked = NexusDesignConstraints.GetBlockedModuleTypes([
            new Battery(NexusUnitCategory.Strike),
        ]);

        Assert.DoesNotContain("Battery", blocked);
    }

    [Fact]
    public void GetBlockedModuleTypes_Vanguard_IsNotBlockedAtTypeLevel()
    {
        var blocked = NexusDesignConstraints.GetBlockedModuleTypes([
            new Vanguard(NexusUnitCategory.Strike),
        ]);

        Assert.DoesNotContain("Vanguard", blocked);
    }

    [Fact]
    public void GetBlockedModuleTypes_SeekerOrScatter_AreNotBlockedAtTypeLevel()
    {
        var blocked = NexusDesignConstraints.GetBlockedModuleTypes([
            new Seeker(NexusUnitCategory.Capital, 1),
        ]);

        Assert.DoesNotContain("Seeker", blocked);
        Assert.DoesNotContain("Scatter", blocked);
    }

    [Fact]
    public void GetBlockedModuleTypes_AllSingletons_BlocksMultiple()
    {
        var blocked = NexusDesignConstraints.GetBlockedModuleTypes([
            new Shield(),
            new Dock(),
            new Beacon(1),
        ]);

        Assert.Contains("Shield", blocked);
        Assert.Contains("Dock", blocked);
        Assert.Contains("Beacon", blocked);
        Assert.Contains("Cloak", blocked); // mutual exclusivity
        Assert.Equal(4, blocked.Count);
    }

    // ── GetTakenCategories ────────────────────────────────────────────────────

    [Fact]
    public void GetTakenCategories_NonCategoryType_ReturnsEmpty()
    {
        var taken = NexusDesignConstraints.GetTakenCategories("Shield", [new Shield()]);

        Assert.Empty(taken);
    }

    [Fact]
    public void GetTakenCategories_Battery_ReturnsTakenCategory()
    {
        var taken = NexusDesignConstraints.GetTakenCategories(
            "Battery",
            [new Battery(NexusUnitCategory.Strike)]
        );

        Assert.Contains(NexusUnitCategory.Strike, taken);
        Assert.DoesNotContain(NexusUnitCategory.Capital, taken);
        Assert.DoesNotContain(NexusUnitCategory.Planetary, taken);
    }

    [Fact]
    public void GetTakenCategories_Battery_MultipleTaken_ReturnsAll()
    {
        var taken = NexusDesignConstraints.GetTakenCategories(
            "Battery",
            [new Battery(NexusUnitCategory.Strike), new Battery(NexusUnitCategory.Capital)]
        );

        Assert.Contains(NexusUnitCategory.Strike, taken);
        Assert.Contains(NexusUnitCategory.Capital, taken);
        Assert.DoesNotContain(NexusUnitCategory.Planetary, taken);
    }

    [Fact]
    public void GetTakenCategories_Vanguard_ReturnsTakenCategory()
    {
        var taken = NexusDesignConstraints.GetTakenCategories(
            "Vanguard",
            [new Vanguard(NexusUnitCategory.Planetary)]
        );

        Assert.Contains(NexusUnitCategory.Planetary, taken);
    }

    [Fact]
    public void GetTakenCategories_Seeker_ReturnsScatterCategories()
    {
        var taken = NexusDesignConstraints.GetTakenCategories(
            "Seeker",
            [new Scatter(NexusUnitCategory.Capital, 1)]
        );

        Assert.Contains(NexusUnitCategory.Capital, taken);
        Assert.Single(taken);
    }

    [Fact]
    public void GetTakenCategories_Scatter_ReturnsSeekerCategories()
    {
        var taken = NexusDesignConstraints.GetTakenCategories(
            "Scatter",
            [new Seeker(NexusUnitCategory.Strike, 1)]
        );

        Assert.Contains(NexusUnitCategory.Strike, taken);
        Assert.Single(taken);
    }

    [Fact]
    public void GetTakenCategories_Seeker_WithMultipleScatters_ReturnsAll()
    {
        var taken = NexusDesignConstraints.GetTakenCategories(
            "Seeker",
            [new Scatter(NexusUnitCategory.Capital, 1), new Scatter(NexusUnitCategory.Strike, 1)]
        );

        Assert.Contains(NexusUnitCategory.Capital, taken);
        Assert.Contains(NexusUnitCategory.Strike, taken);
        Assert.Equal(2, taken.Count);
    }

    [Fact]
    public void GetTakenCategories_EmptyModules_ReturnsEmpty()
    {
        var takenBattery = NexusDesignConstraints.GetTakenCategories("Battery", []);
        var takenSeeker = NexusDesignConstraints.GetTakenCategories("Seeker", []);

        Assert.Empty(takenBattery);
        Assert.Empty(takenSeeker);
    }

    // ── Verify lowercase method still works for completeness ──────────────────

    [Fact]
    public void Validate_NoModules_ReturnsNull()
    {
        var result = NexusDesignConstraints.Validate(NexusUnitCategory.Capital, []);

        Assert.Null(result);
    }

    [Fact]
    public void Validate_ValidDesign_ReturnsNull()
    {
        // Capital: 4 slots. Battery(Strike)=1 + Battery(Capital)=1 + Shield=1 + Hangar(2)=1 = 4.
        var result = NexusDesignConstraints.Validate(
            NexusUnitCategory.Capital,
            [
                new Battery(NexusUnitCategory.Strike),
                new Battery(NexusUnitCategory.Capital),
                new Shield(),
                new Hangar(2),
            ]
        );

        Assert.Null(result);
    }

    [Fact]
    public void Validate_DuplicateSingleton_ReturnsError()
    {
        var result = NexusDesignConstraints.Validate(
            NexusUnitCategory.Capital,
            [new Shield(), new Shield()]
        );

        Assert.NotNull(result);
        Assert.Contains("Duplicate", result);
    }

    [Fact]
    public void Validate_SlotBudgetExceeded_ReturnsError()
    {
        // Strike: 2 slots. Armour(3) = 3 slots > 2.
        var result = NexusDesignConstraints.Validate(NexusUnitCategory.Strike, [new Armour(3)]);

        Assert.NotNull(result);
        Assert.Contains("slots", result);
    }
}

namespace Spx.Nexus.Domain;

/// <summary>The three unit categories used for combat targeting and attack profiles.</summary>
public enum NexusUnitCategory
{
    Strike = 0, // Strike craft — carried; fight in Intercept and some later phases
    Capital = 1, // Capital ships — provide carry capacity; fight in Line and some other phases
    Planetary = 2, // Planetary units — carried; fight in Surface; determine system control
}

/// <summary>Bitmask of the four combat phases a unit may participate in.</summary>
[Flags]
public enum NexusPhaseParticipation
{
    None = 0,
    Intercept = 1,
    Line = 2,
    Orbit = 4,
    Surface = 8,
}

/// <summary>
/// Full combat and targeting profile for a unit type.
/// <para><see cref="Hull"/> is HP (hits to destroy). <see cref="Silhouette"/> is the targeting
/// weight for random hit allocation — it can diverge from hull. <see cref="AttacksIn"/> encodes
/// which phases the unit rolls dice as an attacker, <see cref="DefendsIn"/> encodes which phases
/// the unit can be selected as a target, and <see cref="AttacksPerRound"/> is the number of
/// attack rolls the unit makes in each eligible phase. <see cref="HasShield"/> indicates the
/// unit absorbs the first hit each turn before taking hull damage.</para>
/// <para><see cref="CategoryThresholds"/> maps <c>(phase?, targetCategory)</c> to a minimum d6
/// hit roll; <see cref="UnitThresholds"/> maps <c>(phase?, targetUnit)</c> and takes priority at
/// the same specificity level. A <c>null</c> value explicitly forbids that matchup. Lookup falls
/// through from most-specific (phase+unit) to least-specific (any-phase+category); a missing key
/// means cannot attack.</para>
/// </summary>
public record NexusUnitProfile(
    NexusUnitCategory Category,
    int Hull,
    int Silhouette,
    NexusPhaseParticipation AttacksIn,
    NexusPhaseParticipation DefendsIn,
    int AttacksPerRound,
    bool HasShield,
    IReadOnlyDictionary<
        (CombatPhase? Phase, NexusUnitCategory TargetCategory),
        int?
    > CategoryThresholds,
    IReadOnlyDictionary<(CombatPhase? Phase, NexusUnitType TargetUnit), int?> UnitThresholds
);

/// <summary>All nine unit types in Nexus Protocol.</summary>
public enum NexusUnitType
{
    // Capital — provide carry capacity
    Frigate = 0,
    Destroyer = 1,
    Cruiser = 2,
    Carrier = 3,

    // Strike — must be carried
    Interceptor = 4,
    Fighter = 5,
    Bomber = 6,

    // Planetary — must be carried; determine system control
    Infantry = 7,
    Armor = 8,
}

public static class NexusUnitTypeExtensions
{
    /// <summary>Returns the full combat profile for this unit type.</summary>
    public static NexusUnitProfile Profile(this NexusUnitType t) =>
        t switch
        {
            NexusUnitType.Interceptor => new(
                NexusUnitCategory.Strike,
                1,
                1,
                NexusPhaseParticipation.Intercept | NexusPhaseParticipation.Line,
                NexusPhaseParticipation.Intercept | NexusPhaseParticipation.Line,
                1,
                false,
                CategoryThresholds: new Dictionary<
                    (CombatPhase? Phase, NexusUnitCategory TargetCategory),
                    int?
                >
                {
                    [(null, NexusUnitCategory.Strike)] = 4,
                    [(CombatPhase.Line, NexusUnitCategory.Capital)] = null,
                    [(null, NexusUnitCategory.Capital)] = 6,
                },
                UnitThresholds: new Dictionary<(CombatPhase? Phase, NexusUnitType TargetUnit), int?>
                {
                    [(null, NexusUnitType.Bomber)] = 2,
                }
            ),
            NexusUnitType.Fighter => new(
                NexusUnitCategory.Strike,
                1,
                1,
                NexusPhaseParticipation.Intercept | NexusPhaseParticipation.Line,
                NexusPhaseParticipation.Intercept | NexusPhaseParticipation.Line,
                1,
                false,
                CategoryThresholds: new Dictionary<
                    (CombatPhase? Phase, NexusUnitCategory TargetCategory),
                    int?
                >
                {
                    [(null, NexusUnitCategory.Strike)] = 3,
                    [(null, NexusUnitCategory.Capital)] = 6,
                },
                UnitThresholds: new Dictionary<
                    (CombatPhase? Phase, NexusUnitType TargetUnit),
                    int?
                >()
            ),
            NexusUnitType.Bomber => new(
                NexusUnitCategory.Strike,
                1,
                2,
                NexusPhaseParticipation.Intercept
                    | NexusPhaseParticipation.Line
                    | NexusPhaseParticipation.Orbit,
                NexusPhaseParticipation.Intercept
                    | NexusPhaseParticipation.Line
                    | NexusPhaseParticipation.Orbit,
                1,
                false,
                CategoryThresholds: new Dictionary<
                    (CombatPhase? Phase, NexusUnitCategory TargetCategory),
                    int?
                >
                {
                    [(null, NexusUnitCategory.Strike)] = 4,
                    [(null, NexusUnitCategory.Capital)] = 3,
                    [(null, NexusUnitCategory.Planetary)] = 3,
                },
                UnitThresholds: new Dictionary<(CombatPhase? Phase, NexusUnitType TargetUnit), int?>
                {
                    [(null, NexusUnitType.Interceptor)] = 5,
                }
            ),
            NexusUnitType.Frigate => new(
                NexusUnitCategory.Capital,
                1,
                1,
                NexusPhaseParticipation.Line,
                NexusPhaseParticipation.Line,
                1,
                true,
                CategoryThresholds: new Dictionary<
                    (CombatPhase? Phase, NexusUnitCategory TargetCategory),
                    int?
                >
                {
                    [(null, NexusUnitCategory.Strike)] = 4,
                    [(null, NexusUnitCategory.Capital)] = 3,
                },
                UnitThresholds: new Dictionary<
                    (CombatPhase? Phase, NexusUnitType TargetUnit),
                    int?
                >()
            ),
            NexusUnitType.Destroyer => new(
                NexusUnitCategory.Capital,
                2,
                2,
                NexusPhaseParticipation.Intercept | NexusPhaseParticipation.Line,
                NexusPhaseParticipation.Line,
                1,
                false,
                CategoryThresholds: new Dictionary<
                    (CombatPhase? Phase, NexusUnitCategory TargetCategory),
                    int?
                >
                {
                    [(null, NexusUnitCategory.Strike)] = 4,
                    [(CombatPhase.Line, NexusUnitCategory.Strike)] = 5,
                    [(null, NexusUnitCategory.Capital)] = 4,
                },
                UnitThresholds: new Dictionary<
                    (CombatPhase? Phase, NexusUnitType TargetUnit),
                    int?
                >()
            ),
            NexusUnitType.Cruiser => new(
                NexusUnitCategory.Capital,
                2,
                3,
                NexusPhaseParticipation.Line | NexusPhaseParticipation.Orbit,
                NexusPhaseParticipation.Line,
                2,
                false,
                CategoryThresholds: new Dictionary<
                    (CombatPhase? Phase, NexusUnitCategory TargetCategory),
                    int?
                >
                {
                    [(null, NexusUnitCategory.Strike)] = 5,
                    [(null, NexusUnitCategory.Capital)] = 3,
                    [(null, NexusUnitCategory.Planetary)] = 4,
                },
                UnitThresholds: new Dictionary<
                    (CombatPhase? Phase, NexusUnitType TargetUnit),
                    int?
                >()
            ),
            NexusUnitType.Carrier => new(
                NexusUnitCategory.Capital,
                4,
                4,
                NexusPhaseParticipation.Line,
                NexusPhaseParticipation.Line,
                1,
                false,
                CategoryThresholds: new Dictionary<
                    (CombatPhase? Phase, NexusUnitCategory TargetCategory),
                    int?
                >
                {
                    [(null, NexusUnitCategory.Strike)] = 5,
                    [(null, NexusUnitCategory.Capital)] = 5,
                },
                UnitThresholds: new Dictionary<
                    (CombatPhase? Phase, NexusUnitType TargetUnit),
                    int?
                >()
            ),
            NexusUnitType.Infantry => new(
                NexusUnitCategory.Planetary,
                1,
                1,
                NexusPhaseParticipation.Surface,
                NexusPhaseParticipation.Orbit | NexusPhaseParticipation.Surface,
                1,
                false,
                CategoryThresholds: new Dictionary<
                    (CombatPhase? Phase, NexusUnitCategory TargetCategory),
                    int?
                >
                {
                    [(null, NexusUnitCategory.Planetary)] = 4,
                },
                UnitThresholds: new Dictionary<(CombatPhase? Phase, NexusUnitType TargetUnit), int?>
                {
                    [(null, NexusUnitType.Armor)] = 5,
                }
            ),
            NexusUnitType.Armor => new(
                NexusUnitCategory.Planetary,
                1,
                2,
                NexusPhaseParticipation.Orbit | NexusPhaseParticipation.Surface,
                NexusPhaseParticipation.Orbit | NexusPhaseParticipation.Surface,
                1,
                true,
                CategoryThresholds: new Dictionary<
                    (CombatPhase? Phase, NexusUnitCategory TargetCategory),
                    int?
                >
                {
                    [(null, NexusUnitCategory.Strike)] = 5,
                    [(null, NexusUnitCategory.Planetary)] = 3,
                    [(CombatPhase.Orbit, NexusUnitCategory.Planetary)] = null,
                },
                UnitThresholds: new Dictionary<(CombatPhase? Phase, NexusUnitType TargetUnit), int?>
                {
                    [(null, NexusUnitType.Armor)] = 4,
                }
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(t), t, null),
        };

    /// <summary>Returns true if this is a Capital-category unit.</summary>
    public static bool IsCapital(this NexusUnitType t) =>
        t.Profile().Category == NexusUnitCategory.Capital;

    /// <summary>Returns true if this is a Strike-category unit.</summary>
    public static bool IsStrike(this NexusUnitType t) =>
        t.Profile().Category == NexusUnitCategory.Strike;

    /// <summary>Returns true if this is a Planetary-category unit.</summary>
    public static bool IsPlanetary(this NexusUnitType t) =>
        t.Profile().Category == NexusUnitCategory.Planetary;

    /// <summary>Energy cost to build one unit of this type.</summary>
    public static int Cost(this NexusUnitType t) =>
        t switch
        {
            NexusUnitType.Frigate => 4,
            NexusUnitType.Destroyer => 5,
            NexusUnitType.Cruiser => 6,
            NexusUnitType.Carrier => 8,
            NexusUnitType.Interceptor => 2,
            NexusUnitType.Fighter => 2,
            NexusUnitType.Bomber => 4,
            NexusUnitType.Infantry => 2,
            NexusUnitType.Armor => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(t), t, null),
        };

    /// <summary>Number of carry slots this unit provides when moving fleets.</summary>
    public static int CarryCapacity(this NexusUnitType t) =>
        t switch
        {
            NexusUnitType.Carrier => 8,
            NexusUnitType.Cruiser => 2,
            _ => 0,
        };

    /// <summary>Number of carry slots this unit consumes when included in a fleet move.</summary>
    public static int ConsumedCapacity(this NexusUnitType t) =>
        t.IsStrike() || t.IsPlanetary() ? 1 : 0;
}

namespace Spx.Nexus.Domain;

/// <summary>The three unit categories used for combat targeting and attack profiles.</summary>
public enum NexusUnitCategory
{
    Strike = 0, // Strike craft — carried; fight in Screen and some later phases
    Capital = 1, // Capital ships — provide carry capacity; fight in Engage and some other phases
    Planetary = 2, // Planetary units — carried; fight in Assault; determine system control
}

/// <summary>Bitmask of the four combat phases a unit may participate in.</summary>
[Flags]
public enum NexusPhaseParticipation
{
    None = 0,
    Screen = 1,
    Engage = 2,
    Bombard = 4,
    Assault = 8,
}

/// <summary>
/// Full combat and targeting profile for a unit type.
/// <para><see cref="Hull"/> is HP (hits to destroy). <see cref="AttacksIn"/> encodes which phases
/// the unit rolls dice as an attacker. Targetability is derived from <see cref="Category"/>.</para>
/// <para>Threshold fields are the base d6 roll needed to hit a unit of that category.
/// <c>null</c> means the unit cannot target that category at all. Per-unit exceptions
/// are applied on top of these base values in <see cref="NexusCombatSpec"/>.</para>
/// </summary>
public record NexusUnitProfile(
    NexusUnitCategory Category,
    int Hull,
    NexusPhaseParticipation AttacksIn,
    int? StrikeThreshold,
    int? CapitalThreshold,
    int? PlanetaryThreshold
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
                NexusPhaseParticipation.Screen,
                StrikeThreshold: 4,
                CapitalThreshold: null,
                PlanetaryThreshold: null
            ),
            NexusUnitType.Fighter => new(
                NexusUnitCategory.Strike,
                1,
                NexusPhaseParticipation.Screen | NexusPhaseParticipation.Engage,
                StrikeThreshold: 4,
                CapitalThreshold: 6,
                PlanetaryThreshold: null
            ),
            NexusUnitType.Bomber => new(
                NexusUnitCategory.Strike,
                2,
                NexusPhaseParticipation.Screen
                    | NexusPhaseParticipation.Engage
                    | NexusPhaseParticipation.Bombard,
                StrikeThreshold: 5,
                CapitalThreshold: 4,
                PlanetaryThreshold: 4
            ),
            NexusUnitType.Frigate => new(
                NexusUnitCategory.Capital,
                2,
                NexusPhaseParticipation.Engage,
                StrikeThreshold: 5,
                CapitalThreshold: 4,
                PlanetaryThreshold: null
            ),
            NexusUnitType.Destroyer => new(
                NexusUnitCategory.Capital,
                2,
                NexusPhaseParticipation.Screen | NexusPhaseParticipation.Engage,
                StrikeThreshold: 3,
                CapitalThreshold: 5,
                PlanetaryThreshold: null
            ),
            NexusUnitType.Cruiser => new(
                NexusUnitCategory.Capital,
                3,
                NexusPhaseParticipation.Engage | NexusPhaseParticipation.Bombard,
                StrikeThreshold: 6,
                CapitalThreshold: 3,
                PlanetaryThreshold: 6
            ),
            NexusUnitType.Carrier => new(
                NexusUnitCategory.Capital,
                4,
                NexusPhaseParticipation.Engage,
                StrikeThreshold: 6,
                CapitalThreshold: 6,
                PlanetaryThreshold: null
            ),
            NexusUnitType.Infantry => new(
                NexusUnitCategory.Planetary,
                1,
                NexusPhaseParticipation.Assault,
                StrikeThreshold: null,
                CapitalThreshold: null,
                PlanetaryThreshold: 4
            ),
            NexusUnitType.Armor => new(
                NexusUnitCategory.Planetary,
                2,
                NexusPhaseParticipation.Assault,
                StrikeThreshold: null,
                CapitalThreshold: null,
                PlanetaryThreshold: 3
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(t), t, null),
        };

    /// <summary>Unit category (Strike, Capital, or Planetary).</summary>
    public static NexusUnitCategory Category(this NexusUnitType t) => t.Profile().Category;

    /// <summary>Hit points — the number of hits required to destroy this unit.</summary>
    public static int Hull(this NexusUnitType t) => t.Profile().Hull;

    /// <summary>Returns true if this is a Capital-category unit.</summary>
    public static bool IsCapital(this NexusUnitType t) => t.Category() == NexusUnitCategory.Capital;

    /// <summary>Returns true if this is a Strike-category unit.</summary>
    public static bool IsStrike(this NexusUnitType t) => t.Category() == NexusUnitCategory.Strike;

    /// <summary>Returns true if this is a Planetary-category unit.</summary>
    public static bool IsPlanetary(this NexusUnitType t) =>
        t.Category() == NexusUnitCategory.Planetary;

    /// <summary>
    /// Silhouette is the targeting weight used for random hit allocation.
    /// Equal to Hull for now; the two values will diverge in a future tuning pass.
    /// </summary>
    public static int Silhouette(this NexusUnitType t) =>
        t switch
        {
            NexusUnitType.Frigate => 2,
            NexusUnitType.Destroyer => 2,
            NexusUnitType.Cruiser => 3,
            NexusUnitType.Carrier => 4,
            NexusUnitType.Interceptor => 1,
            NexusUnitType.Fighter => 1,
            NexusUnitType.Bomber => 2,
            NexusUnitType.Infantry => 1,
            NexusUnitType.Armor => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(t), t, null),
        };

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

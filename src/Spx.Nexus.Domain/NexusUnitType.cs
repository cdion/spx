namespace Spx.Nexus.Domain;

/// <summary>The three unit categories used for combat targeting and carry mechanics.</summary>
public enum NexusUnitCategory
{
    Strike = 0, // Strike craft — must be carried
    Capital = 1, // Capital ships — provide carry capacity
    Planetary = 2, // Planetary units — must be carried; determine system control
}

/// <summary>
/// Full combat profile for a unit type.
/// <para><see cref="Tags"/> encodes special abilities (Shield, FirstAttack*, CanAttack*,
/// BonusVs*, PenaltyVs*). Bonus tags lower the hit threshold by 1 against that category;
/// penalty tags raise it by 1. A missing <c>CanTarget*</c> tag for the target's category
/// means the unit cannot target it.</para>
/// </summary>
public record NexusUnitProfile(
    NexusUnitCategory Category,
    int Hits,
    int Silhouette,
    int Attacks,
    int HitThreshold,
    int Cost,
    NexusUnitTag Tags
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
                Hits: 1,
                Silhouette: 1,
                Attacks: 1,
                HitThreshold: 4,
                Cost: 1,
                Tags: NexusUnitTag.FirstAttackStrike
            ),
            NexusUnitType.Fighter => new(
                NexusUnitCategory.Strike,
                Hits: 1,
                Silhouette: 1,
                Attacks: 1,
                HitThreshold: 4,
                Cost: 1,
                Tags: NexusUnitTag.CanAttackStrike
                    | NexusUnitTag.CanAttackCapital
                    | NexusUnitTag.PenaltyVsCapital
            ),
            NexusUnitType.Bomber => new(
                NexusUnitCategory.Strike,
                Hits: 1,
                Silhouette: 1,
                Attacks: 1,
                HitThreshold: 4,
                Cost: 2,
                Tags: NexusUnitTag.CanAttackStrike
                    | NexusUnitTag.CanAttackCapital
                    | NexusUnitTag.CanAttackPlanetary
                    | NexusUnitTag.PenaltyVsStrike
                    | NexusUnitTag.IgnoreShield
            ),
            NexusUnitType.Frigate => new(
                NexusUnitCategory.Capital,
                Hits: 1,
                Silhouette: 2,
                Attacks: 2,
                HitThreshold: 4,
                Cost: 3,
                Tags: NexusUnitTag.CanAttackStrike
                    | NexusUnitTag.CanAttackCapital
                    | NexusUnitTag.Shield
                    | NexusUnitTag.Escort
            ),
            NexusUnitType.Destroyer => new(
                NexusUnitCategory.Capital,
                Hits: 2,
                Silhouette: 2,
                Attacks: 2,
                HitThreshold: 4,
                Cost: 4,
                Tags: NexusUnitTag.CanAttackStrike
                    | NexusUnitTag.CanAttackCapital
                    | NexusUnitTag.FreeAttackStrike
            ),
            NexusUnitType.Cruiser => new(
                NexusUnitCategory.Capital,
                Hits: 2,
                Silhouette: 3,
                Attacks: 3,
                HitThreshold: 4,
                Cost: 5,
                Tags: NexusUnitTag.CanAttackStrike
                    | NexusUnitTag.CanAttackCapital
                    | NexusUnitTag.CanAttackPlanetary
                    | NexusUnitTag.BonusVsCapital
            ),
            NexusUnitType.Carrier => new(
                NexusUnitCategory.Capital,
                Hits: 2,
                Silhouette: 4,
                Attacks: 2,
                HitThreshold: 5,
                Cost: 6,
                Tags: NexusUnitTag.CanAttackStrike
                    | NexusUnitTag.CanAttackCapital
                    | NexusUnitTag.Shield
            ),
            NexusUnitType.Infantry => new(
                NexusUnitCategory.Planetary,
                Hits: 1,
                Silhouette: 1,
                Attacks: 1,
                HitThreshold: 4,
                Cost: 1,
                Tags: NexusUnitTag.CanAttackPlanetary
            ),
            NexusUnitType.Armor => new(
                NexusUnitCategory.Planetary,
                Hits: 1,
                Silhouette: 2,
                Attacks: 1,
                HitThreshold: 4,
                Cost: 2,
                Tags: NexusUnitTag.CanAttackPlanetary | NexusUnitTag.Shield
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
    public static int Cost(this NexusUnitType t) => t.Profile().Cost;

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

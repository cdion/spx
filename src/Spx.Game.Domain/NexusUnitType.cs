namespace Spx.Game.Domain;

/// <summary>All nine unit types in Nexus Protocol.</summary>
public enum NexusUnitType
{
    // Ships — provide carry capacity; fight in P1 (Destroyer only), P2, and P3 (some)
    Frigate = 0,
    Destroyer = 1,
    Cruiser = 2,
    Carrier = 3,

    // Squadrons — must be carried; fight in P1 and P2 (some)
    Interceptor = 4,
    Fighter = 5,
    Bomber = 6,

    // Ground Forces — must be carried; fight in P4; determine system control
    Infantry = 7,
    Armor = 8,
}

public static class NexusUnitTypeExtensions
{
    public static bool IsShip(this NexusUnitType t) =>
        t
            is NexusUnitType.Frigate
                or NexusUnitType.Destroyer
                or NexusUnitType.Cruiser
                or NexusUnitType.Carrier;

    public static bool IsSquadron(this NexusUnitType t) =>
        t is NexusUnitType.Interceptor or NexusUnitType.Fighter or NexusUnitType.Bomber;

    public static bool IsGroundForce(this NexusUnitType t) =>
        t is NexusUnitType.Infantry or NexusUnitType.Armor;

    /// <summary>
    /// Silhouette doubles as HP. Silhouette-weighted random targeting selects which unit is hit.
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
        t.IsSquadron() || t.IsGroundForce() ? 1 : 0;
}

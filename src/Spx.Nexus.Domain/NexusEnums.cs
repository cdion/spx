using Orleans;

namespace Spx.Nexus.Domain;

/// <summary>The two combat phases: contact fires first, then battle resolves remaining units.</summary>
public enum NexusCombatPhase
{
    Contact = 0,
    Battle = 1,
}

/// <summary>Derived attack capability for one target category: how many attacks in each phase and at what threshold.</summary>
public record NexusAttackSpec(int Battle, int Contact, int Threshold);

/// <summary>Extensions for <see cref="NexusCombatPhase"/>.</summary>
public static class NexusCombatPhaseExtensions
{
    /// <summary>Returns the human-readable display name for this phase.</summary>
    public static string DisplayName(this NexusCombatPhase phase) =>
        phase switch
        {
            NexusCombatPhase.Contact => "Contact",
            NexusCombatPhase.Battle => "Battle",
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null),
        };
}

public enum NexusFactionColor
{
    Red = 0,
    Blue = 1,
}

public enum NexusGateProgress
{
    None = 0,
    Started = 1,
    Completed = 2,
}

/// <summary>Tags model special abilities and behaviours that can be applied to a unit design.</summary>
[GenerateSerializer]
[Alias("NexusUnitModule")]
public abstract record NexusUnitModule
{
    private static readonly IReadOnlySet<NexusUnitCategory> AllHulls =
        new HashSet<NexusUnitCategory>
        {
            NexusUnitCategory.Strike,
            NexusUnitCategory.Capital,
            NexusUnitCategory.Planetary,
        };

    /// <summary>The hull categories this module is valid for. Used to filter available
    /// modules when building a design and to validate hull applicability.</summary>
    public abstract IReadOnlySet<NexusUnitCategory> AllowedHulls { get; }

    /// <summary>Validates this module's intrinsic constraints (parameter bounds, etc.)
    /// for the given <paramref name="hull"/>. Returns null if valid, or an error message.</summary>
    public abstract string? Validate(NexusUnitCategory hull);

    protected static IReadOnlySet<NexusUnitCategory> All() => AllHulls;
}

/// <summary>Absorbs the first hit each combat on a 4+ save.</summary>
[GenerateSerializer]
public sealed record Shield : NexusUnitModule
{
    /// <inheritdoc />
    public override IReadOnlySet<NexusUnitCategory> AllowedHulls => All();

    /// <inheritdoc />
    public override string? Validate(NexusUnitCategory hull) => null;
}

/// <summary>Attacks bypass Shield saves entirely.</summary>
[GenerateSerializer]
public sealed record Disruptor : NexusUnitModule
{
    /// <inheritdoc />
    public override IReadOnlySet<NexusUnitCategory> AllowedHulls => All();

    /// <inheritdoc />
    public override string? Validate(NexusUnitCategory hull) => null;
}

/// <summary>Reduces effective silhouette of up to <see cref="N"/> friendly ships by 1 (min 1) when attacked by <see cref="Category"/> units. Highest silhouette ships covered first.</summary>
[GenerateSerializer]
public sealed record Screen([property: Id(0)] NexusUnitCategory Category, [property: Id(1)] int N)
    : NexusUnitModule
{
    /// <inheritdoc />
    public override IReadOnlySet<NexusUnitCategory> AllowedHulls => All();

    /// <inheritdoc />
    public override string? Validate(NexusUnitCategory hull) =>
        N switch
        {
            <= 0 => "Screen N must be at least 1.",
            > 4 => "Screen N cannot exceed 4.",
            _ => null,
        };
}

/// <summary>Reduces hit threshold by 1 for up to <see cref="N"/> friendly <see cref="Category"/> units when they attack. Highest silhouette units buffed first.</summary>
[GenerateSerializer]
public sealed record Command([property: Id(0)] NexusUnitCategory Category, [property: Id(1)] int N)
    : NexusUnitModule
{
    /// <inheritdoc />
    public override IReadOnlySet<NexusUnitCategory> AllowedHulls => All();

    /// <inheritdoc />
    public override string? Validate(NexusUnitCategory hull) =>
        N switch
        {
            <= 0 => "Command N must be at least 1.",
            > 4 => "Command N cannot exceed 4.",
            _ => null,
        };
}

/// <summary>This unit can be transported by a Capital unit with <see cref="Hangar"/>. Consumes one carry slot.</summary>
[GenerateSerializer]
public sealed record Dock : NexusUnitModule
{
    private static readonly IReadOnlySet<NexusUnitCategory> DockHulls =
        new HashSet<NexusUnitCategory> { NexusUnitCategory.Strike, NexusUnitCategory.Planetary };

    /// <inheritdoc />
    public override IReadOnlySet<NexusUnitCategory> AllowedHulls => DockHulls;

    /// <inheritdoc />
    public override string? Validate(NexusUnitCategory hull) => null;
}

/// <summary>Provides carry capacity for units with <see cref="Dock"/>. Capital hull only.</summary>
[GenerateSerializer]
public sealed record Hangar([property: Id(0)] int Capacity) : NexusUnitModule
{
    private static readonly IReadOnlySet<NexusUnitCategory> HangarHulls =
        new HashSet<NexusUnitCategory> { NexusUnitCategory.Capital };

    /// <inheritdoc />
    public override IReadOnlySet<NexusUnitCategory> AllowedHulls => HangarHulls;

    /// <inheritdoc />
    public override string? Validate(NexusUnitCategory hull) =>
        Capacity switch
        {
            <= 0 => "Hangar Capacity must be at least 1.",
            > 8 => "Hangar Capacity cannot exceed 8.",
            _ => null,
        };
}

/// <summary>Base attacks against <see cref="Category"/> units resolve in the Battle phase.</summary>
[GenerateSerializer]
public sealed record Battery([property: Id(0)] NexusUnitCategory Category) : NexusUnitModule
{
    /// <inheritdoc />
    public override IReadOnlySet<NexusUnitCategory> AllowedHulls => All();

    /// <inheritdoc />
    public override string? Validate(NexusUnitCategory hull) => null;
}

/// <summary>Base attacks against <see cref="Category"/> units resolve in the Contact phase (safe from return fire).</summary>
[GenerateSerializer]
public sealed record Vanguard([property: Id(0)] NexusUnitCategory Category) : NexusUnitModule
{
    /// <inheritdoc />
    public override IReadOnlySet<NexusUnitCategory> AllowedHulls => All();

    /// <inheritdoc />
    public override string? Validate(NexusUnitCategory hull) => null;
}

/// <summary>Hit threshold reduced by <see cref="Magnitude"/> vs <see cref="Category"/> targets.</summary>
[GenerateSerializer]
public sealed record Seeker(
    [property: Id(0)] NexusUnitCategory Category,
    [property: Id(1)] int Magnitude
) : NexusUnitModule
{
    /// <inheritdoc />
    public override IReadOnlySet<NexusUnitCategory> AllowedHulls => All();

    /// <inheritdoc />
    public override string? Validate(NexusUnitCategory hull) =>
        Magnitude switch
        {
            <= 0 => "Seeker Magnitude must be at least 1.",
            > 2 => "Seeker Magnitude cannot exceed 2.",
            _ => null,
        };
}

/// <summary>Hit threshold increased by <see cref="Magnitude"/> vs <see cref="Category"/> targets.</summary>
[GenerateSerializer]
public sealed record Scatter(
    [property: Id(0)] NexusUnitCategory Category,
    [property: Id(1)] int Magnitude
) : NexusUnitModule
{
    /// <inheritdoc />
    public override IReadOnlySet<NexusUnitCategory> AllowedHulls => All();

    /// <inheritdoc />
    public override string? Validate(NexusUnitCategory hull) =>
        Magnitude switch
        {
            <= 0 => "Scatter Magnitude must be at least 1.",
            > 2 => "Scatter Magnitude cannot exceed 2.",
            _ => null,
        };
}

/// <summary>Increases this unit's hit points by <see cref="N"/>.</summary>
[GenerateSerializer]
public sealed record Armour([property: Id(0)] int N) : NexusUnitModule
{
    /// <inheritdoc />
    public override IReadOnlySet<NexusUnitCategory> AllowedHulls => All();

    /// <inheritdoc />
    public override string? Validate(NexusUnitCategory hull) =>
        N switch
        {
            <= 0 => "Armour N must be at least 1.",
            > 4 => "Armour N cannot exceed 4.",
            _ => null,
        };
}

/// <summary>This unit can contest and hold system control. Without this module, a unit's presence alone cannot flip or hold a system.</summary>
[GenerateSerializer]
public sealed record Control : NexusUnitModule
{
    private static readonly IReadOnlySet<NexusUnitCategory> ControlHulls =
        new HashSet<NexusUnitCategory> { NexusUnitCategory.Planetary };

    /// <inheritdoc />
    public override IReadOnlySet<NexusUnitCategory> AllowedHulls => ControlHulls;

    /// <inheritdoc />
    public override string? Validate(NexusUnitCategory hull) => null;
}

/// <summary>Increases this unit's move range by <see cref="N"/> hexes.</summary>
[GenerateSerializer]
public sealed record Drive([property: Id(0)] int N) : NexusUnitModule
{
    private static readonly IReadOnlySet<NexusUnitCategory> DriveHulls =
        new HashSet<NexusUnitCategory> { NexusUnitCategory.Strike, NexusUnitCategory.Capital };

    /// <inheritdoc />
    public override IReadOnlySet<NexusUnitCategory> AllowedHulls => DriveHulls;

    /// <inheritdoc />
    public override string? Validate(NexusUnitCategory hull) =>
        N switch
        {
            <= 0 => "Drive N must be at least 1.",
            > 2 => "Drive N cannot exceed 2.",
            _ => null,
        };
}

/// <summary>Restores one lost hit at the end of each turn's resolution.</summary>
[GenerateSerializer]
public sealed record Repair : NexusUnitModule
{
    /// <inheritdoc />
    public override IReadOnlySet<NexusUnitCategory> AllowedHulls => All();

    /// <inheritdoc />
    public override string? Validate(NexusUnitCategory hull) => null;
}

/// <summary>Grants N extra module slots at the cost of increased silhouette. Making this unit more likely to be targeted.</summary>
[GenerateSerializer]
public sealed record Bulkhead([property: Id(0)] int N) : NexusUnitModule
{
    /// <inheritdoc />
    public override IReadOnlySet<NexusUnitCategory> AllowedHulls => All();

    /// <inheritdoc />
    public override string? Validate(NexusUnitCategory hull) =>
        N switch
        {
            <= 0 => "Bulkhead N must be at least 1.",
            > 3 => "Bulkhead N cannot exceed 3.",
            _ => null,
        };
}

/// <summary>Increases silhouette by <see cref="N"/>, making this unit more likely to be targeted. No slot benefit.</summary>
[GenerateSerializer]
public sealed record Beacon([property: Id(0)] int N) : NexusUnitModule
{
    /// <inheritdoc />
    public override IReadOnlySet<NexusUnitCategory> AllowedHulls => All();

    /// <inheritdoc />
    public override string? Validate(NexusUnitCategory hull) =>
        N switch
        {
            <= 0 => "Beacon N must be at least 1.",
            > 1 => "Beacon N cannot exceed 1.",
            _ => null,
        };
}

/// <summary>Reduces silhouette by <see cref="N"/> (minimum 1), making this unit less likely to be targeted.</summary>
[GenerateSerializer]
public sealed record Cloak([property: Id(0)] int N) : NexusUnitModule
{
    /// <inheritdoc />
    public override IReadOnlySet<NexusUnitCategory> AllowedHulls => All();

    /// <inheritdoc />
    public override string? Validate(NexusUnitCategory hull) =>
        N switch
        {
            <= 0 => "Cloak N must be at least 1.",
            > 2 => "Cloak N cannot exceed 2.",
            _ => null,
        };
}

/// <summary>Central cost and slot table for all modules.</summary>
public static class NexusModuleCosts
{
    public static int GetCost(NexusUnitModule module) =>
        module switch
        {
            Shield => 2,
            Disruptor => 2,
            Screen { N: var n } => n,
            Command { N: var n } => n * 2,
            Dock => 0,
            Hangar { Capacity: var c } => c,
            Battery => 1,
            Vanguard => 2,
            Seeker { Magnitude: var m } => m * 2,
            Scatter { Magnitude: var m } => -m,
            Cloak { N: var n } => n * 2,
            Armour { N: var n } => n * 2,
            Control => 1,
            Drive { N: var n } => n * 2,
            Repair => 3,
            Bulkhead { N: var n } => n * 2,
            Beacon => 0,
            _ => 0,
        };

    public static int GetSlots(NexusUnitModule module) =>
        module switch
        {
            Battery => 1,
            Vanguard => 1,
            Seeker { Magnitude: var m } => m,
            Scatter => 0,
            Shield => 1,
            Disruptor => 1,
            Armour { N: var n } => n,
            Hangar { Capacity: var c } => (c + 1) / 2,
            Dock => 0,
            Screen { N: var n } => n,
            Command { N: var n } => n,
            Control => 0,
            Drive { N: var n } => n,
            Repair => 1,
            Bulkhead { N: var n } => -n,
            Beacon => 1,
            Cloak { N: var n } => n,
            _ => 0,
        };
}

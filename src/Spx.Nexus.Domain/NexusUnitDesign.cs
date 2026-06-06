using Orleans;

namespace Spx.Nexus.Domain;

/// <summary>The three unit categories used for combat targeting and carry mechanics.</summary>
public enum NexusUnitCategory
{
    Strike = 0, // Strike craft — must be carried (unless given Move via tags)
    Capital = 1, // Capital ships — can move independently; provide carry capacity via Hangar tag
    Planetary = 2, // Planetary units — must be carried; determine system control
}

/// <summary>A player-created unit design: a hull category plus a set of tags.</summary>
[GenerateSerializer]
public sealed class NexusUnitDesign
{
    [Id(0)]
    public Guid DesignId { get; set; }

    [Id(1)]
    public string Name { get; set; } = "";

    [Id(2)]
    public NexusUnitCategory Hull { get; set; }

    [Id(3)]
    public List<NexusUnitModule> Modules { get; set; } = [];
}

/// <summary>
/// Full combat and movement profile for a unit, derived from its hull baseline plus modules.
/// <see cref="Move"/> is the number of hex sectors the unit can move independently (0 means it must be carried).
/// <see cref="Attacks"/> is a per-category dictionary mapping target to (Battle attacks, Contact attacks, threshold).
/// </summary>
public record NexusUnitProfile(
    NexusUnitCategory Category,
    int Hits,
    int Silhouette,
    IReadOnlyDictionary<NexusUnitCategory, NexusAttackSpec> Attacks,
    int Cost,
    int Move,
    int CarryCapacity,
    bool RequiresCarrier,
    IReadOnlyList<NexusUnitModule> Modules
);

/// <summary>Baseline stats for each hull category, derived from the simplest unit of that type.</summary>
public static class NexusHullBaselines
{
    private const int StrikeBaseCost = 1;
    private const int CapitalBaseCost = 2;
    private const int PlanetaryBaseCost = 1;

    public static NexusUnitProfile GetBaseline(NexusUnitCategory hull) =>
        hull switch
        {
            NexusUnitCategory.Strike => new(
                NexusUnitCategory.Strike,
                Hits: 1,
                Silhouette: 1,
                Attacks: new Dictionary<NexusUnitCategory, NexusAttackSpec>(),
                Cost: StrikeBaseCost,
                Move: 0,
                CarryCapacity: 0,
                RequiresCarrier: false,
                Modules: []
            ),
            NexusUnitCategory.Capital => new(
                NexusUnitCategory.Capital,
                Hits: 2,
                Silhouette: 2,
                Attacks: new Dictionary<NexusUnitCategory, NexusAttackSpec>(),
                Cost: CapitalBaseCost,
                Move: 1,
                CarryCapacity: 0,
                RequiresCarrier: false,
                Modules: []
            ),
            NexusUnitCategory.Planetary => new(
                NexusUnitCategory.Planetary,
                Hits: 1,
                Silhouette: 1,
                Attacks: new Dictionary<NexusUnitCategory, NexusAttackSpec>(),
                Cost: PlanetaryBaseCost,
                Move: 0,
                CarryCapacity: 0,
                RequiresCarrier: false,
                Modules: []
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(hull), hull, null),
        };

    /// <summary>Derives the full profile for a design: baseline stats + tag costs.</summary>
    public static NexusUnitProfile GetProfile(NexusUnitDesign design)
    {
        var baseline = GetBaseline(design.Hull);
        var cost = baseline.Cost + design.Modules.Sum(NexusModuleCosts.GetCost);
        var extraHits = design.Modules.OfType<Armour>().Sum(t => t.N);
        var extraMove = design.Modules.OfType<Drive>().Sum(t => t.N);
        var extraSilhouette =
            design.Modules.OfType<Bulkhead>().Sum(t => t.N)
            + design.Modules.OfType<Beacon>().Sum(t => t.N)
            - design.Modules.OfType<Cloak>().Sum(t => t.N);

        NexusAttackSpec? DeriveSpec(NexusUnitCategory cat)
        {
            var battle = design.Modules.OfType<Battery>().Count(b => b.Category == cat);
            var contact = design.Modules.OfType<Vanguard>().Count(v => v.Category == cat);
            if (battle + contact == 0)
                return null;
            var seeker = design
                .Modules.OfType<Seeker>()
                .Where(s => s.Category == cat)
                .Sum(s => s.Magnitude);
            var scatter = design
                .Modules.OfType<Scatter>()
                .Where(s => s.Category == cat)
                .Sum(s => s.Magnitude);
            return new(battle, contact, Math.Max(2, 4 - seeker + scatter));
        }

        var attacks = Enum.GetValues<NexusUnitCategory>()
            .Select(cat => (cat, spec: DeriveSpec(cat)))
            .Where(x => x.spec is not null)
            .ToDictionary(x => x.cat, x => x.spec!);

        return baseline with
        {
            Cost = cost,
            Hits = baseline.Hits + extraHits,
            Move = baseline.Move + extraMove,
            Silhouette = Math.Max(0, baseline.Silhouette + extraSilhouette),
            Attacks = attacks,
            CarryCapacity = design.Modules.OfType<Hangar>().Sum(t => t.Capacity),
            RequiresCarrier = baseline.Move + extraMove == 0,
            Modules = design.Modules,
        };
    }

    public static int BaseCost(NexusUnitCategory hull) =>
        hull switch
        {
            NexusUnitCategory.Strike => StrikeBaseCost,
            NexusUnitCategory.Capital => CapitalBaseCost,
            NexusUnitCategory.Planetary => PlanetaryBaseCost,
            _ => throw new ArgumentOutOfRangeException(nameof(hull), hull, null),
        };

    public static int SlotBudget(NexusUnitCategory hull) =>
        hull switch
        {
            NexusUnitCategory.Strike => 2,
            NexusUnitCategory.Capital => 4,
            NexusUnitCategory.Planetary => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(hull), hull, null),
        };
}

/// <summary>Validates tag combinations for a given hull. Returns an error message or null.</summary>
public static class NexusDesignConstraints
{
    public static string? Validate(NexusUnitCategory hull, IReadOnlyList<NexusUnitModule> modules)
    {
        foreach (var module in modules)
        {
            var perModuleError = ValidateModule(hull, module);
            if (perModuleError != null)
                return perModuleError;
        }

        var duplicateError = ValidateDuplicates(modules);
        if (duplicateError != null)
            return duplicateError;

        var usedSlots = modules.Sum(NexusModuleCosts.GetSlots);
        var budget = NexusHullBaselines.SlotBudget(hull);
        if (usedSlots > budget)
            return $"Design uses {usedSlots} slots but {hull} hull only has {budget}.";

        return null;
    }

    private static string? ValidateModule(NexusUnitCategory hull, NexusUnitModule module) =>
        module switch
        {
            Hangar when hull != NexusUnitCategory.Capital =>
                "Hangar module is only valid on Capital hull designs.",
            Hangar { Capacity: <= 0 } => "Hangar Capacity must be at least 1.",
            Hangar { Capacity: > 8 } => "Hangar Capacity cannot exceed 8.",
            Dock when hull == NexusUnitCategory.Capital =>
                "Dock module is not valid on Capital hull designs.",
            Control when hull != NexusUnitCategory.Planetary =>
                "Control module is only valid on Planetary hull designs.",
            Armour { N: <= 0 } => "Armour N must be at least 1.",
            Armour { N: > 4 } => "Armour N cannot exceed 4.",
            Drive when hull == NexusUnitCategory.Planetary =>
                "Drive module is not valid on Planetary hull designs.",
            Drive { N: <= 0 } => "Drive N must be at least 1.",
            Drive { N: > 2 } => "Drive N cannot exceed 2.",
            Seeker { Magnitude: <= 0 } => "Seeker Magnitude must be at least 1.",
            Seeker { Magnitude: > 2 } => "Seeker Magnitude cannot exceed 2.",
            Scatter { Magnitude: <= 0 } => "Scatter Magnitude must be at least 1.",
            Scatter { Magnitude: > 2 } => "Scatter Magnitude cannot exceed 2.",
            Bulkhead { N: <= 0 } => "Bulkhead N must be at least 1.",
            Bulkhead { N: > 3 } => "Bulkhead N cannot exceed 3.",
            Beacon { N: <= 0 } => "Beacon N must be at least 1.",
            Beacon { N: > 1 } => "Beacon N cannot exceed 1.",
            Cloak { N: <= 0 } => "Cloak N must be at least 1.",
            Cloak { N: > 2 } => "Cloak N cannot exceed 2.",
            Screen { N: <= 0 } => "Screen N must be at least 1.",
            Screen { N: > 4 } => "Screen N cannot exceed 4.",
            Command { N: <= 0 } => "Command N must be at least 1.",
            Command { N: > 4 } => "Command N cannot exceed 4.",
            _ => null,
        };

    private static string? ValidateDuplicates(IReadOnlyList<NexusUnitModule> modules)
    {
        if (modules.OfType<Shield>().Count() > 1)
            return "Duplicate Shield module.";
        if (modules.OfType<Disruptor>().Count() > 1)
            return "Duplicate Disruptor module.";
        if (modules.OfType<Dock>().Count() > 1)
            return "Duplicate Dock module.";
        if (modules.OfType<Control>().Count() > 1)
            return "Duplicate Control module.";
        if (modules.OfType<Repair>().Count() > 1)
            return "Duplicate Repair module.";
        if (modules.OfType<Bulkhead>().Count() > 1)
            return "Duplicate Bulkhead module.";
        if (modules.OfType<Beacon>().Count() > 1)
            return "Duplicate Beacon module.";
        if (modules.OfType<Cloak>().Count() > 1)
            return "Duplicate Cloak module.";
        if (modules.OfType<Beacon>().Any() && modules.OfType<Cloak>().Any())
            return "Beacon and Cloak modules are mutually exclusive.";

        foreach (var category in Enum.GetValues<NexusUnitCategory>())
        {
            if (modules.OfType<Battery>().Count(b => b.Category == category) > 1)
                return $"Duplicate Battery({category}) module.";
            if (modules.OfType<Vanguard>().Count(v => v.Category == category) > 1)
                return $"Duplicate Vanguard({category}) module.";
            if (
                modules.OfType<Seeker>().Any(s => s.Category == category)
                && modules.OfType<Scatter>().Any(s => s.Category == category)
            )
                return $"Seeker({category}) and Scatter({category}) modules are mutually exclusive.";
        }

        return null;
    }
}

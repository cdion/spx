using Orleans;

namespace Spx.Nexus.Domain;

/// <summary>Factory for the three starter designs given to every player at game start.</summary>
public static class NexusDefaultDesigns
{
    public static List<NexusUnitDesign> Create() =>
        [
            new()
            {
                DesignId = Guid.NewGuid(),
                Name = "Fighter",
                Hull = NexusUnitCategory.Strike,
                Modules =
                [
                    new Battery(NexusUnitCategory.Strike),
                    new Battery(NexusUnitCategory.Capital),
                    new Dock(),
                ],
            },
            new()
            {
                DesignId = Guid.NewGuid(),
                Name = "Light Freighter",
                Hull = NexusUnitCategory.Capital,
                Modules = [new Hangar(4)],
            },
            new()
            {
                DesignId = Guid.NewGuid(),
                Name = "Light Tank",
                Hull = NexusUnitCategory.Planetary,
                Modules = [new Battery(NexusUnitCategory.Planetary), new Control(), new Dock()],
            },
        ];
}

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

    [Id(4)]
    public bool IsDeleted { get; set; }
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

/// <summary>Validates tag combinations for a given hull. Returns an error message or null.
/// <para>Module-scoped rules (hull applicability, value bounds) are delegated to each
/// module's <see cref="NexusUnitModule.AllowedHulls"/> and <see cref="NexusUnitModule.Validate"/>.
/// This class handles design-wide composition rules: duplicates, mutual exclusivity, slot budget.</para>
/// </summary>
public static class NexusDesignConstraints
{
    public static string? Validate(NexusUnitCategory hull, IReadOnlyList<NexusUnitModule> modules)
    {
        foreach (var module in modules)
        {
            if (!module.AllowedHulls.Contains(hull))
                return $"{module.GetType().Name} module is not valid on {hull} hull designs.";

            var perModuleError = module.Validate(hull);
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

    /// <summary>
    /// Pre-flight check: would adding <paramref name="candidate"/> to
    /// <paramref name="existingModules"/> be valid for <paramref name="hull"/>?
    /// Returns null if valid, or an error message explaining why not.
    /// Covers hull applicability, value bounds, singleton duplicates, mutual
    /// exclusivity, category-scoped rules, and slot budget.
    /// </summary>
    public static string? CheckAdd(
        NexusUnitCategory hull,
        IReadOnlyList<NexusUnitModule> existingModules,
        NexusUnitModule candidate
    )
    {
        // Hull applicability
        if (!candidate.AllowedHulls.Contains(hull))
            return $"{candidate.GetType().Name} module is not valid on {hull} hull designs.";

        // Value bounds
        var boundsError = candidate.Validate(hull);
        if (boundsError is not null)
            return boundsError;

        // Slot budget: existing + candidate must fit
        var existingSlots = existingModules.Sum(NexusModuleCosts.GetSlots);
        var candidateSlots = NexusModuleCosts.GetSlots(candidate);
        var budget = NexusHullBaselines.SlotBudget(hull);
        if (existingSlots + candidateSlots > budget)
            return $"Adding this module would use {existingSlots + candidateSlots} slots (budget: {budget}).";

        // Duplicate singleton checks
        var duplicateError = ValidateAddDuplicates(existingModules, candidate);
        if (duplicateError is not null)
            return duplicateError;

        return null;
    }

    /// <summary>
    /// Returns module type names that are completely blocked from being added,
    /// regardless of parameters. This covers singleton types (already present)
    /// and the Beacon/Cloak mutual exclusivity.
    /// Category-scoped types (Battery, Vanguard, Seeker, Scatter) are NOT in
    /// this set even if some categories are taken — the caller should check
    /// <see cref="GetTakenCategories"/> for per-category filtering.
    /// </summary>
    public static IReadOnlySet<string> GetBlockedModuleTypes(IReadOnlyList<NexusUnitModule> modules)
    {
        var blocked = new HashSet<string>();

        if (modules.OfType<Shield>().Any())
            blocked.Add(nameof(Shield));
        if (modules.OfType<Disruptor>().Any())
            blocked.Add(nameof(Disruptor));
        if (modules.OfType<Dock>().Any())
            blocked.Add(nameof(Dock));
        if (modules.OfType<Control>().Any())
            blocked.Add(nameof(Control));
        if (modules.OfType<Repair>().Any())
            blocked.Add(nameof(Repair));
        if (modules.OfType<Bulkhead>().Any())
            blocked.Add(nameof(Bulkhead));

        // Beacon and Cloak are both singletons AND mutually exclusive.
        // If either is present, both are blocked.
        if (modules.OfType<Beacon>().Any() || modules.OfType<Cloak>().Any())
        {
            blocked.Add(nameof(Beacon));
            blocked.Add(nameof(Cloak));
        }

        return blocked;
    }

    /// <summary>
    /// For the given module type name, returns which <see cref="NexusUnitCategory"/>
    /// values are already taken (cannot be selected). Handles per-category singletons
    /// (Battery, Vanguard) and per-category mutual exclusivity (Seeker/Scatter).
    /// Returns an empty set for types without category parameters.
    /// </summary>
    public static IReadOnlySet<NexusUnitCategory> GetTakenCategories(
        string moduleTypeName,
        IReadOnlyList<NexusUnitModule> modules
    )
    {
        var taken = new HashSet<NexusUnitCategory>();

        if (moduleTypeName is nameof(Battery) or nameof(Vanguard))
        {
            foreach (var m in modules)
            {
                if (m is Battery b && moduleTypeName == nameof(Battery))
                    taken.Add(b.Category);
                if (m is Vanguard v && moduleTypeName == nameof(Vanguard))
                    taken.Add(v.Category);
            }
        }

        if (moduleTypeName is nameof(Seeker))
        {
            foreach (var m in modules.OfType<Scatter>())
                taken.Add(m.Category);
        }

        if (moduleTypeName is nameof(Scatter))
        {
            foreach (var m in modules.OfType<Seeker>())
                taken.Add(m.Category);
        }

        return taken;
    }

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
                return $"Duplicate Battery ({category}) module.";
            if (modules.OfType<Vanguard>().Count(v => v.Category == category) > 1)
                return $"Duplicate Vanguard ({category}) module.";
            if (
                modules.OfType<Seeker>().Any(s => s.Category == category)
                && modules.OfType<Scatter>().Any(s => s.Category == category)
            )
                return $"Seeker({category}) and Scatter({category}) modules are mutually exclusive.";
        }

        return null;
    }

    private static string? ValidateAddDuplicates(
        IReadOnlyList<NexusUnitModule> existing,
        NexusUnitModule candidate
    )
    {
        // Singleton types — no duplicates allowed
        if (candidate is Shield && existing.OfType<Shield>().Any())
            return "Duplicate Shield module.";
        if (candidate is Disruptor && existing.OfType<Disruptor>().Any())
            return "Duplicate Disruptor module.";
        if (candidate is Dock && existing.OfType<Dock>().Any())
            return "Duplicate Dock module.";
        if (candidate is Control && existing.OfType<Control>().Any())
            return "Duplicate Control module.";
        if (candidate is Repair && existing.OfType<Repair>().Any())
            return "Duplicate Repair module.";
        if (candidate is Bulkhead && existing.OfType<Bulkhead>().Any())
            return "Duplicate Bulkhead module.";

        // Beacon and Cloak are both singletons AND mutually exclusive
        if (
            candidate is Beacon or Cloak
            && (existing.OfType<Beacon>().Any() || existing.OfType<Cloak>().Any())
        )
            return "Beacon and Cloak modules are mutually exclusive.";

        return ValidateAddCategoryScoped(existing, candidate);
    }

    private static string? ValidateAddCategoryScoped(
        IReadOnlyList<NexusUnitModule> existing,
        NexusUnitModule candidate
    )
    {
        foreach (var category in Enum.GetValues<NexusUnitCategory>())
        {
            if (
                candidate is Battery { Category: var bCat }
                && bCat == category
                && existing.OfType<Battery>().Any(b => b.Category == category)
            )
                return $"Duplicate Battery ({category}) module.";

            if (
                candidate is Vanguard { Category: var vCat }
                && vCat == category
                && existing.OfType<Vanguard>().Any(v => v.Category == category)
            )
                return $"Duplicate Vanguard ({category}) module.";

            if (
                candidate is Seeker { Category: var sCat }
                && sCat == category
                && existing.OfType<Scatter>().Any(s => s.Category == category)
            )
                return $"Seeker({category}) and Scatter({category}) modules are mutually exclusive.";

            if (
                candidate is Scatter { Category: var scCat }
                && scCat == category
                && existing.OfType<Seeker>().Any(s => s.Category == category)
            )
                return $"Seeker({category}) and Scatter({category}) modules are mutually exclusive.";
        }

        return null;
    }
}

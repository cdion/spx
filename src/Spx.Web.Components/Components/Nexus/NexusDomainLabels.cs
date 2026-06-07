using Spx.Nexus.Domain;

namespace Spx.Web.Components.Nexus;

public static class NexusDomainLabels
{
    public static string GetHullLabel(NexusUnitCategory category) =>
        category switch
        {
            NexusUnitCategory.Strike => "Strike",
            NexusUnitCategory.Capital => "Capital",
            NexusUnitCategory.Planetary => "Planetary",
            _ => category.ToString(),
        };

    public static string GetDesignLabel(NexusUnitStackGroup stack) =>
        stack.DesignName.Length > 0 ? stack.DesignName : stack.Category.ToString();

    /// <summary>
    /// CSS class for the hits pill badge based on remaining vs. baseline hits.
    /// </summary>
    public static string GetHitsPillClass(NexusUnitCategory category, int remainingHits)
    {
        var baselineHits = NexusHullBaselines.GetBaseline(category).Hits;
        if (remainingHits >= baselineHits)
            return "border-emerald-400/30 bg-emerald-500/10 text-emerald-200";

        if (remainingHits > 1)
            return "border-amber-400/30 bg-amber-500/10 text-amber-200";

        return "border-rose-400/30 bg-rose-500/10 text-rose-200";
    }

    public static string GetHitsPillClass(NexusUnitStackGroup stack) =>
        GetHitsPillClass(stack.Category, stack.RemainingHits);

    public static string GetModuleTypeDescription(string moduleType) =>
        moduleType switch
        {
            "Battery" => "Base attacks against the target category resolve in the Battle phase.",
            "Vanguard" =>
                "Base attacks against the target category resolve in the Contact phase, before return fire.",
            "Seeker" =>
                "Reduces hit threshold by the magnitude vs. the target category — easier to score hits.",
            "Scatter" =>
                "Increases hit threshold by the magnitude vs. the target category — harder to score hits.",
            "Shield" => "Absorbs the first hit each combat on a 4+ save.",
            "Disruptor" => "This unit's attacks bypass Shield saves entirely.",
            "Armour" => "Increases this unit's hit points by N.",
            "Screen" =>
                "Reduces effective silhouette of up to N friendly ships by 1 when attacked by the target category.",
            "Command" =>
                "Reduces hit threshold by 1 for up to N friendly units of the target category when they attack.",
            "Dock" =>
                "This unit can be transported by a Capital unit with Hangar. Consumes one carry slot.",
            "Hangar" => "Provides carry capacity for Dock units. Capital hull only.",
            "Control" => "This unit can contest and hold system control. Planetary hull only.",
            "Drive" => "Increases this unit's move range by N hexes.",
            "Repair" => "Restores one lost hit at the end of each turn's resolution.",
            "Bulkhead" => "Grants N extra module slots at the cost of increased silhouette.",
            "Beacon" => "Increases silhouette by N, making this unit more likely to be targeted.",
            "Cloak" =>
                "Reduces silhouette by N (minimum 1), making this unit less likely to be targeted.",
            _ => "",
        };

    public static string GetModuleDescription(NexusUnitModule module) =>
        GetModuleTypeDescription(module.GetType().Name);

    public static string GetModuleLabel(NexusUnitModule module) =>
        module switch
        {
            Battery { Category: var c } => $"Battery ({c})",
            Vanguard { Category: var c } => $"Vanguard ({c})",
            Seeker { Category: var c, Magnitude: var m } => $"Seeker ({c}) ×{m}",
            Scatter { Category: var c, Magnitude: var m } => $"Scatter ({c}) ×{m}",
            Screen { Category: var c, N: var n } => $"Screen ({c}) ×{n}",
            Command { Category: var c, N: var n } => $"Command ({c}) ×{n}",
            Hangar { Capacity: var cap } => $"Hangar ×{cap}",
            Armour { N: var n } => $"Armour ×{n}",
            Drive { N: var n } => $"Drive ×{n}",
            Bulkhead { N: var n } => $"Bulkhead ×{n}",
            Beacon { N: var n } => $"Beacon ×{n}",
            Cloak { N: var n } => $"Cloak ×{n}",
            Shield => "Shield",
            Disruptor => "Disruptor",
            Dock => "Dock",
            Control => "Control",
            Repair => "Repair",
            _ => module.GetType().Name,
        };
}

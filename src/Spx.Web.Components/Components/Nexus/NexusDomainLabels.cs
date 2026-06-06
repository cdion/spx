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
    public static string GetHitsPillClass(NexusUnitStackGroup stack)
    {
        if (stack.RemainingHits >= NexusHullBaselines.GetBaseline(stack.Category).Hits)
            return "border-emerald-400/30 bg-emerald-500/10 text-emerald-200";

        if (stack.RemainingHits > 1)
            return "border-amber-400/30 bg-amber-500/10 text-amber-200";

        return "border-rose-400/30 bg-rose-500/10 text-rose-200";
    }

    public static string GetModuleLabel(NexusUnitModule module) =>
        module switch
        {
            Battery { Category: var c } => $"Battery ({c})",
            Vanguard { Category: var c } => $"Vanguard ({c})",
            Barrage { Category: var c } => $"Barrage ({c})",
            Seeker { Category: var c, Magnitude: var m } => $"Seeker ({c}) ×{m}",
            Scatter { Category: var c, Magnitude: var m } => $"Scatter ({c}) ×{m}",
            Screen { Category: var c, N: var n } => $"Screen ({c}) ×{n}",
            Command { Category: var c, N: var n } => $"Command ({c}) ×{n}",
            Hangar { Capacity: var cap } => $"Hangar ×{cap}",
            Armour { N: var n } => $"Armour ×{n}",
            Drive { N: var n } => $"Drive ×{n}",
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

using Spx.Nexus.Domain;

namespace Spx.Web.Components.Nexus;

/// <summary>
/// Faction-aware CSS class helpers.
/// Every method returns a complete, detectable Tailwind literal — no string interpolation.
/// </summary>
public static class FactionCss
{
    public static string PlayerNameClass(NexusFactionColor faction) =>
        faction switch
        {
            NexusFactionColor.Red => "text-lg font-semibold tracking-wide text-red-300",
            NexusFactionColor.Blue => "text-lg font-semibold tracking-wide text-sky-300",
            _ => "text-lg font-semibold tracking-wide text-slate-200",
        };

    public static string AccentText(NexusFactionColor faction) =>
        faction switch
        {
            NexusFactionColor.Red => "text-red-300",
            NexusFactionColor.Blue => "text-sky-300",
            _ => "text-slate-300",
        };

    public static string ForcesClass(NexusFactionColor faction) =>
        faction switch
        {
            NexusFactionColor.Red => "text-red-400",
            NexusFactionColor.Blue => "text-sky-400",
            _ => "text-slate-400",
        };

    /// <summary>
    /// Returns the accent text class for shade 200.
    /// Used for active stack names.
    /// </summary>
    public static string ActiveStackNameClass(NexusFactionColor faction) =>
        faction switch
        {
            NexusFactionColor.Red => "text-red-200",
            NexusFactionColor.Blue => "text-sky-200",
            _ => "text-slate-200",
        };

    public static string ButtonActiveRow(NexusFactionColor faction) =>
        faction switch
        {
            NexusFactionColor.Red => "ui-button-row-active-danger",
            NexusFactionColor.Blue => "ui-button-row-active-info",
            _ => "ui-button-row-active-info",
        };

    public static string SelectedCountClass(NexusFactionColor faction) =>
        faction switch
        {
            NexusFactionColor.Red => "text-red-300",
            NexusFactionColor.Blue => "text-sky-300",
            _ => "text-slate-300",
        };
}

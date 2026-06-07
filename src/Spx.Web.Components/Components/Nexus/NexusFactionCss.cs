using Spx.Nexus.Domain;

namespace Spx.Web.Components.Nexus;

/// <summary>
/// Faction-aware CSS class helpers.
/// Every method returns a complete, detectable Tailwind literal — no string interpolation.
/// </summary>
public static class NexusFactionCss
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

    public static string PipFill(NexusFactionColor faction) =>
        faction switch
        {
            NexusFactionColor.Red => "rgba(252,165,165,0.9)",
            NexusFactionColor.Blue => "rgba(125,211,252,0.9)",
            _ => "rgba(148,163,184,0.7)",
        };

    public static string PipStroke(NexusFactionColor faction) =>
        faction switch
        {
            NexusFactionColor.Red => "rgba(239,68,68,0.4)",
            NexusFactionColor.Blue => "rgba(14,165,233,0.4)",
            _ => "rgba(255,255,255,0.3)",
        };

    // SVG map colors — returned as raw SVG attribute strings, not Tailwind classes.

    /// <summary>Solid hex fill color for active SVG map elements (paths, strokes, chevrons).</summary>
    public static string SvgFill(NexusFactionColor faction) =>
        faction switch
        {
            NexusFactionColor.Red => "#f87171",
            NexusFactionColor.Blue => "#60a5fa",
            _ => "#94a3b8",
        };

    /// <summary>RGBA dim fill for unit count pip backgrounds on the hex map.</summary>
    public static string SvgUnitDim(NexusFactionColor faction) =>
        faction switch
        {
            NexusFactionColor.Red => "rgba(248,113,113,0.25)",
            NexusFactionColor.Blue => "rgba(96,165,250,0.25)",
            _ => "rgba(148,163,184,0.25)",
        };

    /// <summary>RGBA dim fill for gate progress pip backgrounds on the hex map.</summary>
    public static string SvgGateDim(NexusFactionColor faction) =>
        faction switch
        {
            NexusFactionColor.Red => "rgba(248,113,113,0.18)",
            NexusFactionColor.Blue => "rgba(96,165,250,0.18)",
            _ => "rgba(148,163,184,0.18)",
        };
}

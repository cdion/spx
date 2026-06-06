using System.Text;
using Spx.Game.Application.Nexus;
using Spx.Nexus.Domain;

namespace Spx.Web.Components.Nexus;

internal static class NexusHexHelpers
{
    public const double HexSize = 34.0;
    private static readonly double Sqrt3 = Math.Sqrt(3.0);

    // -------------------------------------------------------------------------
    // Geometry
    // -------------------------------------------------------------------------

    public static (double X, double Y) HexCenter(HexCoord coord) =>
        (HexSize * (Sqrt3 * coord.Q + Sqrt3 / 2.0 * coord.R), HexSize * 1.5 * coord.R);

    public static string HexPoints((double X, double Y) center)
    {
        var parts = new string[6];
        for (var i = 0; i < 6; i++)
        {
            var angle = Math.PI / 180.0 * (60.0 * i - 30.0);
            var x = center.X + HexSize * Math.Cos(angle);
            var y = center.Y + HexSize * Math.Sin(angle);
            parts[i] = $"{Fmt(x)},{Fmt(y)}";
        }

        return string.Join(" ", parts);
    }

    // Top triangle cap: vertices i=5 (top), i=0 (top-right), i=4 (top-left)
    public static string CapPoints((double X, double Y) center)
    {
        var top = (X: center.X, Y: center.Y - HexSize);
        var topRight = (
            X: center.X + HexSize * Math.Cos(Math.PI / 180.0 * -30.0),
            Y: center.Y + HexSize * Math.Sin(Math.PI / 180.0 * -30.0)
        );
        var topLeft = (
            X: center.X + HexSize * Math.Cos(Math.PI / 180.0 * 210.0),
            Y: center.Y + HexSize * Math.Sin(Math.PI / 180.0 * 210.0)
        );

        return $"{Fmt(top.X)},{Fmt(top.Y)} {Fmt(topRight.X)},{Fmt(topRight.Y)} {Fmt(topLeft.X)},{Fmt(topLeft.Y)}";
    }

    // 5-pointed star centered at the cap triangle's centroid (center.X, center.Y - 2*HexSize/3)
    public static string StarPoints((double X, double Y) center)
    {
        const double outerR = 4.0;
        const double innerR = 1.8;
        var starCx = center.X;
        var starCy = center.Y - 2.0 * HexSize / 3.0;

        var sb = new StringBuilder();
        for (var i = 0; i < 5; i++)
        {
            var outerAngle = Math.PI / 180.0 * (-90.0 + i * 72.0);
            var innerAngle = Math.PI / 180.0 * (-90.0 + i * 72.0 + 36.0);

            if (i > 0)
                sb.Append(' ');

            sb.Append(Fmt(starCx + outerR * Math.Cos(outerAngle)));
            sb.Append(',');
            sb.Append(Fmt(starCy + outerR * Math.Sin(outerAngle)));
            sb.Append(' ');
            sb.Append(Fmt(starCx + innerR * Math.Cos(innerAngle)));
            sb.Append(',');
            sb.Append(Fmt(starCy + innerR * Math.Sin(innerAngle)));
        }

        return sb.ToString();
    }

    // ── Colours ───────────────────────────────────────────────────────────────

    /// <summary>Background fill for a hex, tinted by who controls the system.</summary>
    public static string GetHexFill(
        NexusSystemView system,
        Guid currentPlayerId,
        NexusFactionColor currentPlayerFaction
    ) =>
        system.IsNexus
            ? "rgba(139,92,246,0.2)"
            : system.ControlOwner switch
            {
                { } owner when owner == currentPlayerId => currentPlayerFaction
                == NexusFactionColor.Red
                    ? "rgba(127,29,29,0.55)"
                    : "rgba(23,37,84,0.55)",
                not null => currentPlayerFaction == NexusFactionColor.Red
                    ? "rgba(23,37,84,0.45)"
                    : "rgba(127,29,29,0.45)",
                null => "rgba(15,23,42,0.55)",
            };

    /// <summary>Cap triangle fill for home/Nexus systems; null = no cap.</summary>
    public static string? GetCapFill(
        NexusSystemView system,
        Guid currentPlayerId,
        NexusFactionColor currentPlayerFaction
    )
    {
        if (system.IsNexus)
            return "rgba(251,191,36,0.5)";
        if (system.HomePlayerId == currentPlayerId)
            return currentPlayerFaction == NexusFactionColor.Red
                ? "rgba(248,113,113,0.52)"
                : "rgba(96,165,250,0.52)";
        if (system.HomePlayerId.HasValue)
            return currentPlayerFaction == NexusFactionColor.Red
                ? "rgba(96,165,250,0.52)"
                : "rgba(248,113,113,0.52)";
        return null;
    }

    /// <summary>Text colour for faction labels.</summary>
    public static string GetFactionColor(NexusFactionColor faction) =>
        faction switch
        {
            NexusFactionColor.Red => "#f87171",
            NexusFactionColor.Blue => "#60a5fa",
            _ => "#94a3b8",
        };

    // ── Unit counts ───────────────────────────────────────────────────────────

    /// <summary>Returns the Capital/Strike/Planetary breakdown for a single player's units in a system.</summary>
    public static (int Capital, int Strike, int Planetary) GetUnitCounts(
        NexusSystemView system,
        Guid playerId
    )
    {
        var units = system.GetPlayerStacks(playerId);
        if (units.Length == 0)
            return (0, 0, 0);

        var capital = 0;
        var strike = 0;
        var planetary = 0;
        foreach (var stack in units)
        {
            switch (stack.Category)
            {
                case NexusUnitCategory.Capital:
                    capital += stack.Count;
                    break;
                case NexusUnitCategory.Strike:
                    strike += stack.Count;
                    break;
                case NexusUnitCategory.Planetary:
                    planetary += stack.Count;
                    break;
            }
        }

        return (capital, strike, planetary);
    }

    /// <summary>Compact unit label: "C·2 S·1 P·3" (omits zero categories).</summary>
    public static string GetUnitLabel(int capital, int strike, int planetary)
    {
        var parts = new List<string>(3);
        if (capital > 0)
            parts.Add($"C·{capital}");
        if (strike > 0)
            parts.Add($"S·{strike}");
        if (planetary > 0)
            parts.Add($"P·{planetary}");
        return string.Join(" ", parts);
    }

    // ── Category pips ─────────────────────────────────────────────────────────

    /// <summary>
    /// Appends SVG for one category pip to the builder.
    /// Strike = triangle ▲, Capital = diamond ◆, Planetary = circle ●.
    /// When <paramref name="count"/> is 0, the pip renders as an outlined (dim) shape.
    /// </summary>
    public static void AppendCategoryPip(
        StringBuilder sb,
        double cx,
        double cy,
        NexusUnitCategory category,
        string activeColor,
        string dimColor,
        int count
    )
    {
        var fill = count > 0 ? activeColor : "transparent";
        var stroke = count > 0 ? "rgba(255,255,255,0.25)" : dimColor;
        var strokeWidth = count > 0 ? "0.5" : "1";

        var (points, isCircle, r) = category switch
        {
            NexusUnitCategory.Strike => (
                $"{Fmt(cx)},{Fmt(cy - 4)} {Fmt(cx + 4)},{Fmt(cy + 3.5)} {Fmt(cx - 4)},{Fmt(cy + 3.5)}",
                false,
                0.0
            ),
            NexusUnitCategory.Capital => (
                $"{Fmt(cx)},{Fmt(cy - 3.5)} {Fmt(cx + 3.5)},{Fmt(cy)} {Fmt(cx)},{Fmt(cy + 3.5)} {Fmt(cx - 3.5)},{Fmt(cy)}",
                false,
                0.0
            ),
            NexusUnitCategory.Planetary => (string.Empty, true, 3.0),
            _ => (string.Empty, false, 0.0),
        };

        var cxStr = Fmt(cx);
        var cyStr = Fmt(cy);

        if (isCircle)
        {
            sb.Append("<circle");
            sb.Append(" cx=\"");
            sb.Append(cxStr);
            sb.Append('"');
            sb.Append(" cy=\"");
            sb.Append(cyStr);
            sb.Append('"');
            sb.Append(" r=\"");
            sb.Append(Fmt(r));
            sb.Append('"');
        }
        else
        {
            sb.Append("<polygon");
            sb.Append(" points=\"");
            sb.Append(points);
            sb.Append('"');
        }

        sb.Append(" fill=\"");
        sb.Append(fill);
        sb.Append('"');
        sb.Append(" stroke=\"");
        sb.Append(stroke);
        sb.Append('"');
        sb.Append(" stroke-width=\"");
        sb.Append(strokeWidth);
        sb.Append('"');
        sb.Append("/>");
    }

    // ── Formatting ────────────────────────────────────────────────────────────

    public static string Fmt(double v) =>
        v.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
}

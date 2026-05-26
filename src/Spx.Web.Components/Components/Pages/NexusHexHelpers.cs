using System.Text;
using Spx.Game.Domain;

namespace Spx.Web.Components.Pages;

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

    /// <summary>Returns the S/Q/G breakdown for a single player's units in a system.</summary>
    public static (int Ships, int Squadrons, int GroundForces) GetUnitCounts(
        NexusSystemView system,
        Guid playerId
    )
    {
        if (!system.Units.TryGetValue(playerId, out var units))
            return (0, 0, 0);

        var ships = 0;
        var sqd = 0;
        var gnd = 0;
        foreach (var (type, count) in units)
        {
            if (type.IsShip())
                ships += count;
            else if (type.IsSquadron())
                sqd += count;
            else if (type.IsGroundForce())
                gnd += count;
        }

        return (ships, sqd, gnd);
    }

    /// <summary>Compact unit label: "S·2 Q·1 G·3" (omits zero categories).</summary>
    public static string GetUnitLabel(int ships, int squadrons, int groundForces)
    {
        var parts = new List<string>(3);
        if (ships > 0)
            parts.Add($"S·{ships}");
        if (squadrons > 0)
            parts.Add($"Q·{squadrons}");
        if (groundForces > 0)
            parts.Add($"G·{groundForces}");
        return string.Join(" ", parts);
    }

    // ── Formatting ────────────────────────────────────────────────────────────

    public static string Fmt(double v) =>
        v.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
}

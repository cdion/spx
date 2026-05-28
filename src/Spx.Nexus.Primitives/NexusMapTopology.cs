namespace Spx.Nexus.Primitives;

/// <summary>
/// Pure topological queries for the fixed 19-system hex grid used in Nexus Protocol.
/// Contains the set of valid coordinates and adjacency helpers that belong at the
/// primitive layer, shared between the domain and the application seam.
/// </summary>
public static class NexusMapTopology
{
    private static readonly HashSet<HexCoord> AllCoordsSet =
    [
        // Center
        new(0, 0),
        // Ring 1
        new(1, 0),
        new(0, 1),
        new(-1, 1),
        new(-1, 0),
        new(0, -1),
        new(1, -1),
        // Ring 2
        new(2, 0),
        new(1, 1),
        new(0, 2),
        new(-1, 2),
        new(-2, 2),
        new(-2, 1),
        new(-2, 0),
        new(-1, -1),
        new(0, -2),
        new(1, -2),
        new(2, -2),
        new(2, -1),
    ];

    public static IReadOnlySet<HexCoord> AllCoords => AllCoordsSet;

    public static bool IsValidCoord(HexCoord coord) => AllCoordsSet.Contains(coord);

    public static bool AreAdjacent(HexCoord a, HexCoord b) => a.DistanceTo(b) == 1;

    public static readonly HexCoord NexusCoord = new(0, 0);
    public static readonly HexCoord Player1HomeCoord = new(2, -2);
    public static readonly HexCoord Player2HomeCoord = new(-2, 2);

    private static readonly Dictionary<HexCoord, string> SectorNames = new()
    {
        // Ring 1 — clockwise from NE
        [new(1, -1)] = "Alpha",
        [new(1, 0)] = "Beta",
        [new(0, 1)] = "Gamma",
        [new(-1, 1)] = "Delta",
        [new(-1, 0)] = "Epsilon",
        [new(0, -1)] = "Zeta",
        // Ring 2 — clockwise from NE, homes at (2,-2) and (-2,2) skipped
        [new(2, -1)] = "Eta",
        [new(2, 0)] = "Theta",
        [new(1, 1)] = "Iota",
        [new(0, 2)] = "Kappa",
        [new(-1, 2)] = "Lambda",
        [new(-2, 1)] = "Mu",
        [new(-2, 0)] = "Nu",
        [new(-1, -1)] = "Xi",
        [new(0, -2)] = "Omicron",
        [new(1, -2)] = "Pi",
    };

    public static string GetSectorDisplayName(HexCoord coord)
    {
        if (coord == NexusCoord)
            return "Nexus";
        if (coord == Player1HomeCoord || coord == Player2HomeCoord)
            return "home system";
        return SectorNames.TryGetValue(coord, out var name) ? name : coord.ToString();
    }
}

namespace Spx.Game.Domain;

public enum NexusColonyColor
{
    None = 0,
    Red = 1,
    Blue = 2,
    Gold = 3,
    Green = 4,
    Yellow = 5,
}

public sealed record NexusHexDefinition(
    HexCoord Coord,
    NexusColonyColor Color,
    bool IsNexus,
    bool IsHome,
    NexusFactionColor? HomeFaction = null
);

public sealed class NexusMapLayout
{
    public HexCoord NexusCoord { get; }
    public IReadOnlyList<NexusHexDefinition> Hexes { get; }
    public IReadOnlyDictionary<HexCoord, NexusHexDefinition> ByCoord { get; }
    public IReadOnlyDictionary<NexusFactionColor, HexCoord> HomeCoords { get; }
    public IReadOnlyList<NexusColonyColor> ResourceColors { get; }

    public NexusMapLayout(IReadOnlyList<NexusHexDefinition> hexes)
    {
        Hexes = hexes;
        NexusCoord = hexes.Single(h => h.IsNexus).Coord;
        ByCoord = hexes.ToDictionary(h => h.Coord);
        HomeCoords = hexes
            .Where(h => h.HomeFaction.HasValue)
            .ToDictionary(h => h.HomeFaction!.Value, h => h.Coord);
        ResourceColors = hexes
            .Select(h => h.Color)
            .Distinct()
            .Where(c => c != NexusColonyColor.None)
            .ToList();
    }

    public HexCoord GetHomeCoord(NexusFactionColor faction) => HomeCoords[faction];

    public bool IsValidCoord(HexCoord coord) => ByCoord.ContainsKey(coord);

    public static bool AreAdjacent(HexCoord a, HexCoord b) => a.DistanceTo(b) == 1;
}

public static class NexusMap
{
    public static NexusMapLayout ForPlayerCount(int playerCount) =>
        playerCount switch
        {
            2 => TwoPlayerLayout,
            3 => ThreePlayerLayout,
            4 => FourPlayerLayout,
            _ => throw new ArgumentOutOfRangeException(
                nameof(playerCount),
                playerCount,
                "Only 2\u20134 players are supported."
            ),
        };

    // Legacy statics pointing to the 2P layout for callers not yet updated
    public static HexCoord NexusCoord => TwoPlayerLayout.NexusCoord;
    public static IReadOnlyList<NexusHexDefinition> Hexes => TwoPlayerLayout.Hexes;
    public static IReadOnlyDictionary<HexCoord, NexusHexDefinition> ByCoord =>
        TwoPlayerLayout.ByCoord;

    public static HexCoord GetHomeCoord(NexusFactionColor faction) =>
        TwoPlayerLayout.GetHomeCoord(faction);

    public static bool IsValidCoord(HexCoord coord) => TwoPlayerLayout.IsValidCoord(coord);

    public static bool AreAdjacent(HexCoord a, HexCoord b) => NexusMapLayout.AreAdjacent(a, b);

    // 2-player layout: radius-2 disk, 19 hexes
    private static readonly NexusMapLayout TwoPlayerLayout = new([
        // Nexus
        new(new(0, 0), NexusColonyColor.None, IsNexus: true, IsHome: false),
        // Home hexes
        new(
            new(2, -2),
            NexusColonyColor.Red,
            IsNexus: false,
            IsHome: true,
            HomeFaction: NexusFactionColor.Red
        ),
        new(
            new(-2, 2),
            NexusColonyColor.Blue,
            IsNexus: false,
            IsHome: true,
            HomeFaction: NexusFactionColor.Blue
        ),
        // Gold (4) — ring-1 axis
        new(new(1, 0), NexusColonyColor.Gold, IsNexus: false, IsHome: false),
        new(new(-1, 0), NexusColonyColor.Gold, IsNexus: false, IsHome: false),
        new(new(0, -1), NexusColonyColor.Gold, IsNexus: false, IsHome: false),
        new(new(0, 1), NexusColonyColor.Gold, IsNexus: false, IsHome: false),
        // Red (6)
        new(new(2, -1), NexusColonyColor.Red, IsNexus: false, IsHome: false),
        new(new(2, 0), NexusColonyColor.Red, IsNexus: false, IsHome: false),
        new(new(1, -2), NexusColonyColor.Red, IsNexus: false, IsHome: false),
        new(new(0, -2), NexusColonyColor.Red, IsNexus: false, IsHome: false),
        new(new(1, -1), NexusColonyColor.Red, IsNexus: false, IsHome: false),
        new(new(-1, -1), NexusColonyColor.Red, IsNexus: false, IsHome: false),
        // Blue (6)
        new(new(-2, 1), NexusColonyColor.Blue, IsNexus: false, IsHome: false),
        new(new(-2, 0), NexusColonyColor.Blue, IsNexus: false, IsHome: false),
        new(new(-1, 2), NexusColonyColor.Blue, IsNexus: false, IsHome: false),
        new(new(0, 2), NexusColonyColor.Blue, IsNexus: false, IsHome: false),
        new(new(-1, 1), NexusColonyColor.Blue, IsNexus: false, IsHome: false),
        new(new(1, 1), NexusColonyColor.Blue, IsNexus: false, IsHome: false),
    ]);

    // 3-player layout: radius-2 disk, 19 hexes, 120° symmetry
    private static readonly NexusMapLayout ThreePlayerLayout = new([
        // Nexus
        new(new(0, 0), NexusColonyColor.None, IsNexus: true, IsHome: false),
        // Home hexes (ring-2 corners at 120°)
        new(
            new(2, -2),
            NexusColonyColor.Red,
            IsNexus: false,
            IsHome: true,
            HomeFaction: NexusFactionColor.Red
        ),
        new(
            new(0, 2),
            NexusColonyColor.Blue,
            IsNexus: false,
            IsHome: true,
            HomeFaction: NexusFactionColor.Blue
        ),
        new(
            new(-2, 0),
            NexusColonyColor.Green,
            IsNexus: false,
            IsHome: true,
            HomeFaction: NexusFactionColor.Green
        ),
        // Red non-home (3)
        new(new(2, -1), NexusColonyColor.Red, IsNexus: false, IsHome: false),
        new(new(2, 0), NexusColonyColor.Red, IsNexus: false, IsHome: false),
        new(new(1, -2), NexusColonyColor.Red, IsNexus: false, IsHome: false),
        // Blue non-home (3)
        new(new(-1, 2), NexusColonyColor.Blue, IsNexus: false, IsHome: false),
        new(new(-2, 2), NexusColonyColor.Blue, IsNexus: false, IsHome: false),
        new(new(1, 1), NexusColonyColor.Blue, IsNexus: false, IsHome: false),
        // Green non-home (3)
        new(new(-1, -1), NexusColonyColor.Green, IsNexus: false, IsHome: false),
        new(new(0, -2), NexusColonyColor.Green, IsNexus: false, IsHome: false),
        new(new(-2, 1), NexusColonyColor.Green, IsNexus: false, IsHome: false),
        // Gold — all 6 ring-1 hexes
        new(new(1, -1), NexusColonyColor.Gold, IsNexus: false, IsHome: false),
        new(new(1, 0), NexusColonyColor.Gold, IsNexus: false, IsHome: false),
        new(new(0, -1), NexusColonyColor.Gold, IsNexus: false, IsHome: false),
        new(new(-1, 0), NexusColonyColor.Gold, IsNexus: false, IsHome: false),
        new(new(-1, 1), NexusColonyColor.Gold, IsNexus: false, IsHome: false),
        new(new(0, 1), NexusColonyColor.Gold, IsNexus: false, IsHome: false),
    ]);

    // 4-player layout: cross/spoke shape, 25 hexes
    // Arms in 4 axis directions; homes at distance-3 tips; Gold fills diagonal quadrants
    private static readonly NexusMapLayout FourPlayerLayout = new([
        // Nexus
        new(new(0, 0), NexusColonyColor.None, IsNexus: true, IsHome: false),
        // Red arm (north, 0,-1)
        new(new(0, -1), NexusColonyColor.Red, IsNexus: false, IsHome: false),
        new(new(0, -2), NexusColonyColor.Red, IsNexus: false, IsHome: false),
        new(
            new(0, -3),
            NexusColonyColor.Red,
            IsNexus: false,
            IsHome: true,
            HomeFaction: NexusFactionColor.Red
        ),
        // Blue arm (east, 1,0)
        new(new(1, 0), NexusColonyColor.Blue, IsNexus: false, IsHome: false),
        new(new(2, 0), NexusColonyColor.Blue, IsNexus: false, IsHome: false),
        new(
            new(3, 0),
            NexusColonyColor.Blue,
            IsNexus: false,
            IsHome: true,
            HomeFaction: NexusFactionColor.Blue
        ),
        // Green arm (south, 0,1)
        new(new(0, 1), NexusColonyColor.Green, IsNexus: false, IsHome: false),
        new(new(0, 2), NexusColonyColor.Green, IsNexus: false, IsHome: false),
        new(
            new(0, 3),
            NexusColonyColor.Green,
            IsNexus: false,
            IsHome: true,
            HomeFaction: NexusFactionColor.Green
        ),
        // Yellow arm (west, -1,0)
        new(new(-1, 0), NexusColonyColor.Yellow, IsNexus: false, IsHome: false),
        new(new(-2, 0), NexusColonyColor.Yellow, IsNexus: false, IsHome: false),
        new(
            new(-3, 0),
            NexusColonyColor.Yellow,
            IsNexus: false,
            IsHome: true,
            HomeFaction: NexusFactionColor.Yellow
        ),
        // Gold — NE quadrant (between Red and Blue)
        new(new(1, -1), NexusColonyColor.Gold, IsNexus: false, IsHome: false),
        new(new(2, -1), NexusColonyColor.Gold, IsNexus: false, IsHome: false),
        new(new(1, -2), NexusColonyColor.Gold, IsNexus: false, IsHome: false),
        // Gold — SE quadrant (between Blue and Green)
        new(new(1, 1), NexusColonyColor.Gold, IsNexus: false, IsHome: false),
        new(new(2, 1), NexusColonyColor.Gold, IsNexus: false, IsHome: false),
        new(new(1, 2), NexusColonyColor.Gold, IsNexus: false, IsHome: false),
        // Gold — SW quadrant (between Green and Yellow)
        new(new(-1, 1), NexusColonyColor.Gold, IsNexus: false, IsHome: false),
        new(new(-2, 1), NexusColonyColor.Gold, IsNexus: false, IsHome: false),
        new(new(-1, 2), NexusColonyColor.Gold, IsNexus: false, IsHome: false),
        // Gold — NW quadrant (between Yellow and Red)
        new(new(-1, -1), NexusColonyColor.Gold, IsNexus: false, IsHome: false),
        new(new(-2, -1), NexusColonyColor.Gold, IsNexus: false, IsHome: false),
        new(new(-1, -2), NexusColonyColor.Gold, IsNexus: false, IsHome: false),
    ]);
}

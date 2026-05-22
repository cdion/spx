namespace Spx.Game.Domain;

public enum NexusColonyColor
{
    None = 0,
    Red = 1,
    Blue = 2,
    Gold = 3,
}

public sealed record NexusHexDefinition(
    HexCoord Coord,
    NexusColonyColor Color,
    bool IsNexus,
    bool IsHome
);

public static class NexusMap
{
    public static readonly HexCoord NexusCoord = new(0, 0);
    public static readonly HexCoord RedHomeCoord = new(2, -2);
    public static readonly HexCoord BlueHomeCoord = new(-2, 2);

    public static readonly IReadOnlyList<NexusHexDefinition> Hexes =
    [
        // Nexus
        new(new(0, 0), NexusColonyColor.None, IsNexus: true, IsHome: false),
        // Home hexes (Red faction P1, Blue faction P2)
        new(new(2, -2), NexusColonyColor.Red, IsNexus: false, IsHome: true),
        new(new(-2, 2), NexusColonyColor.Blue, IsNexus: false, IsHome: true),
        // Gold (4) — ring-1 axis, naturally contested
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
    ];

    public static readonly IReadOnlyDictionary<HexCoord, NexusHexDefinition> ByCoord =
        Hexes.ToDictionary(h => h.Coord);

    public static HexCoord GetHomeCoord(NexusFactionColor faction) =>
        faction == NexusFactionColor.Red ? RedHomeCoord : BlueHomeCoord;

    public static bool IsValidCoord(HexCoord coord) => ByCoord.ContainsKey(coord);

    public static bool AreAdjacent(HexCoord a, HexCoord b) => a.DistanceTo(b) == 1;
}

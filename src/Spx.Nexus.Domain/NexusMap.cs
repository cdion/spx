namespace Spx.Nexus.Domain;

/// <summary>
/// Generates and queries the fixed 19-system hex grid used in Nexus Protocol (2-player).
/// All coordinates use axial representation (Q, R) on a radius-2 hex disk.
/// </summary>
public static class NexusMap
{
    /// <summary>Axial coordinate of the central Nexus system.</summary>
    public static readonly HexCoord NexusCoord = NexusMapTopology.NexusCoord;

    /// <summary>Home system for player 1 (assigned by <see cref="GenerateMap"/>).</summary>
    public static readonly HexCoord Player1HomeCoord = NexusMapTopology.Player1HomeCoord;

    /// <summary>Home system for player 2 (assigned by <see cref="GenerateMap"/>).</summary>
    public static readonly HexCoord Player2HomeCoord = NexusMapTopology.Player2HomeCoord;

    /// <summary>
    /// Returns a human-readable display name for <paramref name="coord"/>:
    /// the Greek sector name for neutral systems, <c>"Nexus"</c> for the central system,
    /// and <c>"home system"</c> for the two home systems.
    /// </summary>
    public static string GetSectorDisplayName(HexCoord coord) =>
        NexusMapTopology.GetSectorDisplayName(coord);

    /// <summary>Returns <c>true</c> when <paramref name="coord"/> is a valid system on the map.</summary>
    public static bool IsValidCoord(HexCoord coord) => NexusMapTopology.IsValidCoord(coord);

    /// <summary>Returns <c>true</c> when <paramref name="a"/> and <paramref name="b"/> are adjacent systems.</summary>
    public static bool AreAdjacent(HexCoord a, HexCoord b) => NexusMapTopology.AreAdjacent(a, b);

    /// <summary>
    /// All 19 system coordinates in spiral order: Nexus first, then Ring 1 clockwise from NE,
    /// then Ring 2 clockwise from NE. Used for supply-check disbanding (innermost systems
    /// evaluated first; home systems are last).
    /// </summary>
    public static readonly IReadOnlyList<HexCoord> SystemsInSpiralOrder = new List<HexCoord>
    {
        // Ring 0
        new(0, 0),
        // Ring 1 — clockwise from NE
        new(1, -1), // Alpha
        new(1, 0), // Beta
        new(0, 1), // Gamma
        new(-1, 1), // Delta
        new(-1, 0), // Epsilon
        new(0, -1), // Zeta
        // Ring 2 — clockwise from NE
        new(2, -1), // Eta
        new(2, 0), // Theta
        new(1, 1), // Iota
        new(0, 2), // Kappa
        new(-1, 2), // Lambda
        new(-2, 2), // P2 Home
        new(-2, 1), // Mu
        new(-2, 0), // Nu
        new(-1, -1), // Xi
        new(0, -2), // Omicron
        new(1, -2), // Pi
        new(2, -2), // P1 Home
    }.AsReadOnly();

    /// <summary>
    /// Fixed (Energy, Supply) stat table for all 19 systems.
    /// Layout is mirror-symmetric: each income system at coord (q,r)
    /// has the same stats as its mirror at (-q,-r), so both players
    /// have equal access to all profiles.
    /// Home systems are (2,2); Nexus is (0,0).
    /// </summary>
    private static readonly Dictionary<HexCoord, (int Energy, int Supply)> SystemStats =
        new Dictionary<HexCoord, (int, int)>
        {
            // Ring 1 — mirror pairs (q,r) ↔ (-q,-r)
            [new(1, -1)] = (2, 1), // Alpha  ↔ Delta   — Core World
            [new(-1, 1)] = (2, 1), // Delta  ↔ Alpha
            [new(1, 0)] = (1, 1), // Beta   ↔ Epsilon — Colony
            [new(-1, 0)] = (1, 1), // Epsilon↔ Beta
            [new(0, 1)] = (2, 2), // Gamma  ↔ Zeta    — Capital
            [new(0, -1)] = (2, 2), // Zeta   ↔ Gamma
            // Ring 2 — mirror pairs (q,r) ↔ (-q,-r)
            [new(2, -1)] = (2, 0), // Eta    ↔ Mu      — Trade Port
            [new(-2, 1)] = (2, 0), // Mu     ↔ Eta
            [new(2, 0)] = (0, 2), // Theta  ↔ Nu      — Depot
            [new(-2, 0)] = (0, 2), // Nu     ↔ Theta
            [new(1, 1)] = (1, 0), // Iota   ↔ Xi      — Refinery
            [new(-1, -1)] = (1, 0), // Xi     ↔ Iota
            [new(0, 2)] = (1, 2), // Kappa  ↔ Omicron — Garrison
            [new(0, -2)] = (1, 2), // Omicron↔ Kappa
            [new(-1, 2)] = (0, 1), // Lambda ↔ Pi      — Outpost
            [new(1, -2)] = (0, 1), // Pi     ↔ Lambda
        };

    /// <summary>
    /// Generates the initial <see cref="NexusSystemState"/> list for a new game.
    /// Each system gets a fixed (Energy, Supply) stat pair from the stat table;
    /// home systems are (2,2); Nexus is (0,0).
    /// </summary>
    public static List<NexusSystemState> GenerateMap(Guid player1Id, Guid player2Id, Random rng)
    {
        _ = rng; // kept for interface compat; stats are now deterministic
        var systems = new List<NexusSystemState>(19);

        foreach (var coord in NexusMapTopology.AllCoords)
        {
            var isNexus = coord == NexusCoord;
            var isPlayer1Home = coord == Player1HomeCoord;
            var isPlayer2Home = coord == Player2HomeCoord;

            Guid? homePlayerId =
                isPlayer1Home ? player1Id
                : isPlayer2Home ? player2Id
                : null;

            var energyValue =
                isNexus ? 0
                : homePlayerId.HasValue ? 2
                : SystemStats.TryGetValue(coord, out var stats) ? stats.Energy
                : 1; // fallback — should not happen

            var supplyValue =
                isNexus ? 0
                : homePlayerId.HasValue ? 2
                : SystemStats.TryGetValue(coord, out var stats2) ? stats2.Supply
                : 1; // fallback

            var system = new NexusSystemState
            {
                Coord = coord,
                IsNexus = isNexus,
                HomePlayerId = homePlayerId,
                IncomeValue = energyValue,
                SupplyValue = supplyValue,
                ControlOwner = homePlayerId,
            };

            systems.Add(system);
        }

        return systems;
    }
}

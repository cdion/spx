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
    /// Each of the 8 profiles appears exactly twice across the 16 income systems.
    /// Home systems are (2,2); Nexus is (0,0).
    /// </summary>
    private static readonly Dictionary<HexCoord, (int Energy, int Supply)> SystemStats =
        new Dictionary<HexCoord, (int, int)>
        {
            // Ring 1 — clockwise from NE
            [new(1, -1)] = (2, 1), // Alpha — Core World
            [new(1, 0)] = (1, 1), // Beta — Colony
            [new(0, 1)] = (2, 2), // Gamma — Capital
            [new(-1, 1)] = (2, 0), // Delta — Trade Port
            [new(-1, 0)] = (1, 0), // Epsilon — Refinery
            [new(0, -1)] = (0, 1), // Zeta — Outpost
            // Ring 2 — clockwise from NE
            [new(2, -1)] = (1, 1), // Eta — Colony
            [new(2, 0)] = (0, 2), // Theta — Depot
            [new(1, 1)] = (1, 0), // Iota — Refinery
            [new(0, 2)] = (1, 2), // Kappa — Garrison
            [new(-1, 2)] = (2, 2), // Lambda — Capital
            [new(-2, 1)] = (0, 1), // Mu — Outpost
            [new(-2, 0)] = (2, 1), // Nu — Core World
            [new(-1, -1)] = (0, 2), // Xi — Depot
            [new(0, -2)] = (2, 0), // Omicron — Trade Port
            [new(1, -2)] = (1, 2), // Pi — Garrison
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

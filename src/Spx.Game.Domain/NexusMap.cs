namespace Spx.Game.Domain;

/// <summary>
/// Generates and queries the fixed 19-system hex grid used in Nexus Protocol (2-player).
/// All coordinates use axial representation (Q, R) on a radius-2 hex disk.
/// </summary>
public static class NexusMap
{
    /// <summary>Axial coordinate of the central Nexus system.</summary>
    public static readonly HexCoord NexusCoord = new(0, 0);

    /// <summary>Home system for player 1 (assigned by <see cref="GenerateMap"/>).</summary>
    public static readonly HexCoord Player1HomeCoord = new(2, -2);

    /// <summary>Home system for player 2 (assigned by <see cref="GenerateMap"/>).</summary>
    public static readonly HexCoord Player2HomeCoord = new(-2, 2);

    private static readonly HashSet<HexCoord> AllCoords =
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

    /// <summary>Returns <c>true</c> when <paramref name="coord"/> is a valid system on the map.</summary>
    public static bool IsValidCoord(HexCoord coord) => AllCoords.Contains(coord);

    /// <summary>Returns <c>true</c> when <paramref name="a"/> and <paramref name="b"/> are adjacent systems.</summary>
    public static bool AreAdjacent(HexCoord a, HexCoord b) => a.DistanceTo(b) == 1;

    /// <summary>
    /// Generates the initial <see cref="NexusSystemState"/> list for a new game.
    /// Income values for the 16 non-nexus non-home systems are randomly assigned (2-5 Energy/turn).
    /// </summary>
    public static List<NexusSystemState> GenerateMap(Guid player1Id, Guid player2Id, Random rng)
    {
        var systems = new List<NexusSystemState>(19);

        foreach (var coord in AllCoords)
        {
            var isNexus = coord == NexusCoord;
            var isPlayer1Home = coord == Player1HomeCoord;
            var isPlayer2Home = coord == Player2HomeCoord;

            Guid? homePlayerId =
                isPlayer1Home ? player1Id
                : isPlayer2Home ? player2Id
                : null;

            var incomeValue =
                isNexus ? 0
                : homePlayerId.HasValue ? 3
                : rng.Next(2, 6); // 2-5 inclusive

            var system = new NexusSystemState
            {
                Coord = coord,
                IsNexus = isNexus,
                HomePlayerId = homePlayerId,
                IncomeValue = incomeValue,
                ControlOwner = homePlayerId,
            };

            if (homePlayerId.HasValue)
            {
                // Starting composition: 1 Carrier + 4 Infantry + 2 Fighters per player
                system.Units[homePlayerId.Value] = new Dictionary<NexusUnitType, int>
                {
                    [NexusUnitType.Carrier] = 1,
                    [NexusUnitType.Infantry] = 4,
                    [NexusUnitType.Fighter] = 2,
                };
            }

            systems.Add(system);
        }

        return systems;
    }
}

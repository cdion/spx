using System.Collections.Immutable;

namespace Spx.Nexus.Domain;

/// <summary>
/// Pure query helpers that derive UI-advisory information from a <see cref="NexusGameView"/>.
/// These mirror the engine's private validation rules so that callers can surface valid
/// moves without duplicating domain logic.
/// </summary>
public static class NexusViewQueries
{
    /// <summary>
    /// Returns the set of systems reachable as the next step in a multi-hop move path.
    /// Accounts for remaining fleet movement, the hexes already in the draft path (no cycles),
    /// and enemy-occupied systems that cannot be used as intermediate waypoints.
    /// Returns an empty list when <paramref name="remainingMove"/> is zero or the source has no
    /// movable units.
    /// </summary>
    public static IReadOnlyList<HexCoord> GetValidNextHops(
        NexusGameView view,
        Guid playerId,
        HexCoord fromSystem,
        ImmutableArray<HexCoord> draftPath,
        int remainingMove
    )
    {
        if (remainingMove <= 0)
            return [];

        var system = view.Systems.FirstOrDefault(s => s.Coord == fromSystem);
        if (system is null)
            return [];

        if (system.GetPlayerMovableStacks(playerId).Length == 0)
            return [];

        var currentEndpoint = draftPath.IsDefaultOrEmpty ? fromSystem : draftPath[^1];

        var pathSet = new HashSet<HexCoord> { fromSystem };
        if (!draftPath.IsDefaultOrEmpty)
            foreach (var h in draftPath)
                pathSet.Add(h);

        return currentEndpoint
            .GetNeighbours()
            .Where(h => NexusMap.IsValidCoord(h) && !pathSet.Contains(h))
            .ToList();
    }

    /// <summary>
    /// Returns the set of map systems the given player may legally move units FROM the
    /// specified system in a single hop. Equivalent to <see cref="GetValidNextHops"/> with
    /// an empty draft path and remainingMove = 1.
    /// </summary>
    public static IReadOnlyList<HexCoord> GetValidMoveDestinations(
        NexusGameView view,
        Guid playerId,
        HexCoord fromSystem
    ) => GetValidNextHops(view, playerId, fromSystem, ImmutableArray<HexCoord>.Empty, 1);
}

namespace Spx.Nexus.Domain;

/// <summary>
/// Pure query helpers that derive UI-advisory information from a <see cref="NexusGameView"/>.
/// These mirror the engine's private validation rules so that callers can surface valid
/// moves without duplicating domain logic.
/// </summary>
public static class NexusViewQueries
{
    /// <summary>
    /// Returns the set of map systems the given player may legally move units FROM the
    /// specified system to this turn. Only distance-1 (adjacent) moves are allowed.
    /// Returns an empty list if the player has no units at the source system.
    /// </summary>
    public static IReadOnlyList<HexCoord> GetValidMoveDestinations(
        NexusGameView view,
        Guid playerId,
        HexCoord fromSystem
    )
    {
        var system = view.Systems.FirstOrDefault(s => s.Coord == fromSystem);
        if (system is null)
            return [];

        if (!system.Units.TryGetValue(playerId, out var playerUnits) || playerUnits.Count == 0)
            return [];

        return fromSystem.GetNeighbours().Where(NexusMap.IsValidCoord).ToList();
    }
}

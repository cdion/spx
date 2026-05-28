using Spx.Nexus.Primitives;

namespace Spx.Game.Application.Nexus;

/// <summary>
/// Pure query helpers derived from a <see cref="NexusSessionView"/>.
/// Mirrors the engine's private validation rules so the UI can surface valid moves
/// without depending on <c>Spx.Nexus.Domain</c>.
/// </summary>
public static class NexusSessionViewQueries
{
    /// <summary>
    /// Returns the set of map systems the given player may legally move units FROM the
    /// specified system to this turn. Only distance-1 (adjacent) moves are allowed.
    /// Returns an empty list if the player has no units at the source system.
    /// </summary>
    public static IReadOnlyList<HexCoord> GetValidMoveDestinations(
        NexusSessionView view,
        Guid playerId,
        HexCoord fromSystem
    )
    {
        var system = view.Systems.FirstOrDefault(s => s.Coord == fromSystem);
        if (system is null)
            return [];

        if (!system.Units.TryGetValue(playerId, out var playerUnits) || playerUnits.Count == 0)
            return [];

        return fromSystem.GetNeighbours().Where(NexusMapTopology.IsValidCoord).ToList();
    }
}

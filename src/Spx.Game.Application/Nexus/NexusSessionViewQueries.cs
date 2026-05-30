using Spx.Nexus.Domain;

namespace Spx.Game.Application.Nexus;

/// <summary>
/// Pure query helpers derived from a <see cref="NexusGameView"/>.
/// Mirrors the engine's private validation rules so the UI can surface valid moves.
/// </summary>
public static class NexusSessionViewQueries
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

        if (system.GetPlayerStacks(playerId).Length == 0)
            return [];

        return fromSystem.GetNeighbours().Where(NexusMapTopology.IsValidCoord).ToList();
    }
}

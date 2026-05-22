namespace Spx.Game.Domain;

/// <summary>
/// Pure query helpers that derive UI-advisory information from a <see cref="NexusGameView"/>.
/// These mirror the engine's private validation rules so that callers can surface valid
/// moves and colonize eligibility without duplicating domain logic.
/// </summary>
public static class NexusGameViewQueries
{
    /// <summary>
    /// Returns the set of map hexes the given player may legally move a fleet FROM the
    /// specified hex to this turn. Distance-1 (adjacent) moves are always included.
    /// Distance-2 moves are included when the player has an active trade-route endpoint
    /// at that hex.
    /// </summary>
    public static IReadOnlyList<HexCoord> GetValidMoveDestinations(
        NexusGameView view,
        Guid playerId,
        HexCoord fromHex
    )
    {
        var hex = view.Hexes.FirstOrDefault(h => h.Coord == fromHex);
        if (hex is null)
            return [];

        var faction =
            view.CurrentPlayer.PlayerId == playerId
                ? view.CurrentPlayer.Faction
                : view.OpponentPlayer.Faction;
        var fleetCount = faction == NexusFactionColor.Red ? hex.RedFleetCount : hex.BlueFleetCount;
        if (fleetCount == 0)
            return [];

        var mapCoords = new HashSet<HexCoord>(view.Hexes.Select(h => h.Coord));
        var targets = fromHex.GetNeighbours().Where(n => mapCoords.Contains(n)).ToList();

        var hasSpeedBonus = view.ActiveTradeRoutes.Any(r =>
            (r.Hex1 == fromHex && r.Owner1 == playerId)
            || (r.Hex2 == fromHex && r.Owner2 == playerId)
        );

        if (hasSpeedBonus)
        {
            foreach (var neighbour in targets.ToList())
            {
                foreach (var twoAway in neighbour.GetNeighbours())
                {
                    if (mapCoords.Contains(twoAway) && twoAway != fromHex)
                        targets.Add(twoAway);
                }
            }
        }

        return targets.Distinct().ToList();
    }

    /// <summary>
    /// Returns <see langword="true"/> when the player may issue a colonize order for the
    /// given hex: the hex must not be the Nexus and must not already be owned by the player.
    /// </summary>
    public static bool CanColonize(NexusGameView view, Guid playerId, HexCoord fromHex)
    {
        var hex = view.Hexes.FirstOrDefault(h => h.Coord == fromHex);
        if (hex is null)
            return false;

        return !hex.IsNexus && hex.ColonyOwnerId != playerId;
    }
}

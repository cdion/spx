namespace Spx.Game.Domain;

/// <summary>
/// Pure query helpers that derive UI-advisory information from a <see cref="NexusGameView"/>.
/// These mirror the engine's private validation rules so that callers can surface valid
/// moves and colonize eligibility without duplicating domain logic.
/// </summary>
public static class NexusGameViewQueries
{
    /// <summary>
    /// Returns the set of map hexes the given fleet may legally move to this turn.
    /// Distance-1 (adjacent) moves are always included. Distance-2 moves are included
    /// when the fleet is standing on an active trade-route endpoint it owns.
    /// </summary>
    public static IReadOnlyList<HexCoord> GetValidMoveDestinations(NexusGameView view, Guid fleetId)
    {
        var fleetHex = view.Hexes.FirstOrDefault(h => h.Fleets.Any(f => f.FleetId == fleetId));
        if (fleetHex is null)
            return [];

        var ownerId = fleetHex.Fleets.First(f => f.FleetId == fleetId).OwnerId;
        var mapCoords = new HashSet<HexCoord>(view.Hexes.Select(h => h.Coord));
        var targets = fleetHex.Coord.GetNeighbours().Where(n => mapCoords.Contains(n)).ToList();

        var hasSpeedBonus = view.ActiveTradeRoutes.Any(r =>
            (r.Hex1 == fleetHex.Coord && r.Owner1 == ownerId)
            || (r.Hex2 == fleetHex.Coord && r.Owner2 == ownerId)
        );

        if (hasSpeedBonus)
        {
            foreach (var neighbour in targets.ToList())
            {
                foreach (var twoAway in neighbour.GetNeighbours())
                {
                    if (mapCoords.Contains(twoAway) && twoAway != fleetHex.Coord)
                        targets.Add(twoAway);
                }
            }
        }

        return targets.Distinct().ToList();
    }

    /// <summary>
    /// Returns <see langword="true"/> when the fleet may issue a colonize order for
    /// its current hex: the hex must not be the Nexus and must not already be owned
    /// by the fleet's player.
    /// </summary>
    public static bool CanColonize(NexusGameView view, Guid fleetId)
    {
        var fleetHex = view.Hexes.FirstOrDefault(h => h.Fleets.Any(f => f.FleetId == fleetId));
        if (fleetHex is null)
            return false;

        var ownerId = fleetHex.Fleets.First(f => f.FleetId == fleetId).OwnerId;
        return !fleetHex.IsNexus && fleetHex.ColonyOwnerId != ownerId;
    }
}

namespace Spx.Game.Domain.Tests;

public class NexusGameViewQueriesTests
{
    // -------------------------------------------------------------------------
    // Minimal view builder helpers
    // -------------------------------------------------------------------------

    private static NexusFleetView Fleet(Guid fleetId, Guid ownerId, NexusFactionColor faction) =>
        new(fleetId, ownerId, faction);

    private static NexusHexView Hex(
        HexCoord coord,
        bool isNexus = false,
        Guid? colonyOwnerId = null,
        params NexusFleetView[] fleets
    ) =>
        new(
            coord,
            NexusColonyColor.None,
            isNexus,
            IsHome: false,
            colonyOwnerId,
            ColonyOwnerFaction: null,
            fleets.ToImmutableArray()
        );

    private static NexusPlayerView Player(Guid playerId, NexusFactionColor faction) =>
        new(
            playerId,
            faction,
            RedCredits: 0,
            BlueCredits: 0,
            GoldCredits: 0,
            NexusGateProgress.None,
            HasSubmittedOrders: false,
            IsActive: true,
            PendingFleetOrders: null,
            PendingBuildFleet: false,
            PendingBeginNexusGate: false
        );

    private static NexusGameView View(
        IReadOnlyList<NexusHexView> hexes,
        IReadOnlyList<NexusTradeRouteView>? tradeRoutes = null,
        NexusPlayerView? currentPlayer = null,
        NexusPlayerView? opponentPlayer = null
    )
    {
        var red = Guid.NewGuid();
        var blue = Guid.NewGuid();
        return new NexusGameView(
            Guid.NewGuid(),
            RoundNumber: 1,
            NexusGamePhase.Planning,
            hexes.ToImmutableArray(),
            (tradeRoutes ?? []).ToImmutableArray(),
            currentPlayer ?? Player(red, NexusFactionColor.Red),
            opponentPlayer ?? Player(blue, NexusFactionColor.Blue),
            ResolveEvents: [],
            Completion: null
        );
    }

    // -------------------------------------------------------------------------
    // GetValidMoveDestinations
    // -------------------------------------------------------------------------

    [Fact]
    public void GetValidMoveDestinations_FleetNotFound_ReturnsEmpty()
    {
        var view = View([Hex(new HexCoord(0, 0))]);

        var result = NexusGameViewQueries.GetValidMoveDestinations(view, Guid.NewGuid());

        Assert.Empty(result);
    }

    [Fact]
    public void GetValidMoveDestinations_NoSpeedBonus_ReturnsOnlyAdjacentMapHexes()
    {
        // Center at (0,0) with three neighbours on the map
        var ownerId = Guid.NewGuid();
        var fleetId = Guid.NewGuid();
        var center = new HexCoord(0, 0);
        var n1 = new HexCoord(1, 0);
        var n2 = new HexCoord(0, 1);
        var n3 = new HexCoord(-1, 1);
        // (1,-1), (-1,0), (0,-1) deliberately omitted from the map

        var view = View([
            Hex(center, fleets: [Fleet(fleetId, ownerId, NexusFactionColor.Red)]),
            Hex(n1),
            Hex(n2),
            Hex(n3),
        ]);

        var result = NexusGameViewQueries.GetValidMoveDestinations(view, fleetId);

        Assert.Equal(3, result.Count);
        Assert.Contains(n1, result);
        Assert.Contains(n2, result);
        Assert.Contains(n3, result);
        Assert.DoesNotContain(center, result);
    }

    [Fact]
    public void GetValidMoveDestinations_WithSpeedBonus_IncludesDistanceTwoHexes()
    {
        var ownerId = Guid.NewGuid();
        var fleetId = Guid.NewGuid();
        var src = new HexCoord(0, 0);
        var mid = new HexCoord(1, 0); // distance-1 neighbour
        var far = new HexCoord(2, 0); // distance-2 target (neighbour of mid)

        var tradeRoute = new NexusTradeRouteView(
            src,
            ownerId,
            NexusFactionColor.Red,
            new HexCoord(-1, 0),
            Guid.NewGuid(),
            NexusFactionColor.Blue
        );

        var view = View(
            [
                Hex(src, fleets: [Fleet(fleetId, ownerId, NexusFactionColor.Red)]),
                Hex(mid),
                Hex(far),
            ],
            tradeRoutes: [tradeRoute]
        );

        var result = NexusGameViewQueries.GetValidMoveDestinations(view, fleetId);

        Assert.Contains(mid, result);
        Assert.Contains(far, result);
    }

    [Fact]
    public void GetValidMoveDestinations_WithSpeedBonus_NeverIncludesSourceHex()
    {
        var ownerId = Guid.NewGuid();
        var fleetId = Guid.NewGuid();
        var src = new HexCoord(0, 0);
        var mid = new HexCoord(1, 0);
        // (1, 0)'s neighbours include (0, 0) — the source — which must be excluded

        var tradeRoute = new NexusTradeRouteView(
            src,
            ownerId,
            NexusFactionColor.Red,
            new HexCoord(-1, 0),
            Guid.NewGuid(),
            NexusFactionColor.Blue
        );

        // Put all six distance-2 neighbours in the map to trigger the loop
        var distanceTwo = mid.GetNeighbours();
        var hexes = new List<NexusHexView>
        {
            Hex(src, fleets: [Fleet(fleetId, ownerId, NexusFactionColor.Red)]),
            Hex(mid),
        };
        foreach (var h in distanceTwo.Where(c => c != src))
            hexes.Add(Hex(h));

        var view = View(hexes, tradeRoutes: [tradeRoute]);

        var result = NexusGameViewQueries.GetValidMoveDestinations(view, fleetId);

        Assert.DoesNotContain(src, result);
    }

    [Fact]
    public void GetValidMoveDestinations_SpeedBonusOwner2_IncludesDistanceTwoHexes()
    {
        // Fleet is on Hex2 of the trade route (not Hex1)
        var ownerId = Guid.NewGuid();
        var fleetId = Guid.NewGuid();
        var src = new HexCoord(0, 0);
        var mid = new HexCoord(1, 0);
        var far = new HexCoord(2, 0);

        var tradeRoute = new NexusTradeRouteView(
            new HexCoord(-5, 0), // Hex1, different player
            Guid.NewGuid(),
            NexusFactionColor.Blue,
            src, // Hex2
            ownerId,
            NexusFactionColor.Red
        );

        var view = View(
            [
                Hex(src, fleets: [Fleet(fleetId, ownerId, NexusFactionColor.Red)]),
                Hex(mid),
                Hex(far),
            ],
            tradeRoutes: [tradeRoute]
        );

        var result = NexusGameViewQueries.GetValidMoveDestinations(view, fleetId);

        Assert.Contains(far, result);
    }

    [Fact]
    public void GetValidMoveDestinations_SpeedBonus_NoDuplicates()
    {
        // A distance-1 hex also appears as a distance-2 from another neighbour
        var ownerId = Guid.NewGuid();
        var fleetId = Guid.NewGuid();
        var src = new HexCoord(0, 0);

        // All six neighbours on the map; many are distance-2 from each other
        var neighbours = src.GetNeighbours();
        var hexes = new List<NexusHexView>
        {
            Hex(src, fleets: [Fleet(fleetId, ownerId, NexusFactionColor.Red)]),
        };
        foreach (var n in neighbours)
            hexes.Add(Hex(n));

        var tradeRoute = new NexusTradeRouteView(
            src,
            ownerId,
            NexusFactionColor.Red,
            new HexCoord(99, 0),
            Guid.NewGuid(),
            NexusFactionColor.Blue
        );

        var view = View(hexes, tradeRoutes: [tradeRoute]);

        var result = NexusGameViewQueries.GetValidMoveDestinations(view, fleetId);

        Assert.Equal(result.Count, result.Distinct().Count());
    }

    // -------------------------------------------------------------------------
    // CanColonize
    // -------------------------------------------------------------------------

    [Fact]
    public void CanColonize_FleetNotFound_ReturnsFalse()
    {
        var view = View([Hex(new HexCoord(0, 0))]);

        Assert.False(NexusGameViewQueries.CanColonize(view, Guid.NewGuid()));
    }

    [Fact]
    public void CanColonize_FleetOnNexusHex_ReturnsFalse()
    {
        var ownerId = Guid.NewGuid();
        var fleetId = Guid.NewGuid();

        var view = View([
            Hex(
                new HexCoord(0, 0),
                isNexus: true,
                fleets: [Fleet(fleetId, ownerId, NexusFactionColor.Red)]
            ),
        ]);

        Assert.False(NexusGameViewQueries.CanColonize(view, fleetId));
    }

    [Fact]
    public void CanColonize_FleetOnOwnColony_ReturnsFalse()
    {
        var ownerId = Guid.NewGuid();
        var fleetId = Guid.NewGuid();

        var view = View([
            Hex(
                new HexCoord(1, 0),
                colonyOwnerId: ownerId,
                fleets: [Fleet(fleetId, ownerId, NexusFactionColor.Red)]
            ),
        ]);

        Assert.False(NexusGameViewQueries.CanColonize(view, fleetId));
    }

    [Fact]
    public void CanColonize_FleetOnUnownedHex_ReturnsTrue()
    {
        var ownerId = Guid.NewGuid();
        var fleetId = Guid.NewGuid();

        var view = View([
            Hex(
                new HexCoord(1, 0),
                colonyOwnerId: null,
                fleets: [Fleet(fleetId, ownerId, NexusFactionColor.Red)]
            ),
        ]);

        Assert.True(NexusGameViewQueries.CanColonize(view, fleetId));
    }

    [Fact]
    public void CanColonize_FleetOnOpponentColony_ReturnsTrue()
    {
        var ownerId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var fleetId = Guid.NewGuid();

        var view = View([
            Hex(
                new HexCoord(1, 0),
                colonyOwnerId: opponentId,
                fleets: [Fleet(fleetId, ownerId, NexusFactionColor.Red)]
            ),
        ]);

        Assert.True(NexusGameViewQueries.CanColonize(view, fleetId));
    }
}

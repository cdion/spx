namespace Spx.Game.Domain.Tests;

public class NexusGameViewQueriesTests
{
    // -------------------------------------------------------------------------
    // Minimal view builder helpers
    // -------------------------------------------------------------------------

    private static NexusHexView Hex(
        HexCoord coord,
        bool isNexus = false,
        Guid? colonyOwnerId = null,
        int redFleets = 0,
        int blueFleets = 0
    ) =>
        new(
            coord,
            NexusColonyColor.None,
            isNexus,
            IsHome: false,
            colonyOwnerId,
            ColonyOwnerFaction: null,
            redFleets,
            blueFleets
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
    public void GetValidMoveDestinations_NoFleetOnHex_ReturnsEmpty()
    {
        var playerId = Guid.NewGuid();
        var view = View([Hex(new HexCoord(0, 0))]);

        var result = NexusGameViewQueries.GetValidMoveDestinations(
            view,
            playerId,
            new HexCoord(0, 0)
        );

        Assert.Empty(result);
    }

    [Fact]
    public void GetValidMoveDestinations_NoSpeedBonus_ReturnsOnlyAdjacentMapHexes()
    {
        var playerId = Guid.NewGuid();
        var player = Player(playerId, NexusFactionColor.Red);
        var center = new HexCoord(0, 0);
        var n1 = new HexCoord(1, 0);
        var n2 = new HexCoord(0, 1);
        var n3 = new HexCoord(-1, 1);

        var view = View(
            [Hex(center, redFleets: 1), Hex(n1), Hex(n2), Hex(n3)],
            currentPlayer: player
        );

        var result = NexusGameViewQueries.GetValidMoveDestinations(view, playerId, center);

        Assert.Equal(3, result.Count);
        Assert.Contains(n1, result);
        Assert.Contains(n2, result);
        Assert.Contains(n3, result);
        Assert.DoesNotContain(center, result);
    }

    [Fact]
    public void GetValidMoveDestinations_WithSpeedBonus_IncludesDistanceTwoHexes()
    {
        var playerId = Guid.NewGuid();
        var player = Player(playerId, NexusFactionColor.Red);
        var src = new HexCoord(0, 0);
        var mid = new HexCoord(1, 0);
        var far = new HexCoord(2, 0);

        var tradeRoute = new NexusTradeRouteView(
            src,
            playerId,
            NexusFactionColor.Red,
            new HexCoord(-1, 0),
            Guid.NewGuid(),
            NexusFactionColor.Blue
        );

        var view = View(
            [Hex(src, redFleets: 1), Hex(mid), Hex(far)],
            tradeRoutes: [tradeRoute],
            currentPlayer: player
        );

        var result = NexusGameViewQueries.GetValidMoveDestinations(view, playerId, src);

        Assert.Contains(mid, result);
        Assert.Contains(far, result);
    }

    [Fact]
    public void GetValidMoveDestinations_WithSpeedBonus_NeverIncludesSourceHex()
    {
        var playerId = Guid.NewGuid();
        var player = Player(playerId, NexusFactionColor.Red);
        var src = new HexCoord(0, 0);
        var mid = new HexCoord(1, 0);

        var tradeRoute = new NexusTradeRouteView(
            src,
            playerId,
            NexusFactionColor.Red,
            new HexCoord(-1, 0),
            Guid.NewGuid(),
            NexusFactionColor.Blue
        );

        var distanceTwo = mid.GetNeighbours();
        var hexes = new List<NexusHexView> { Hex(src, redFleets: 1), Hex(mid) };
        foreach (var h in distanceTwo.Where(c => c != src))
            hexes.Add(Hex(h));

        var view = View(hexes, tradeRoutes: [tradeRoute], currentPlayer: player);

        var result = NexusGameViewQueries.GetValidMoveDestinations(view, playerId, src);

        Assert.DoesNotContain(src, result);
    }

    [Fact]
    public void GetValidMoveDestinations_SpeedBonusOwner2_IncludesDistanceTwoHexes()
    {
        var playerId = Guid.NewGuid();
        var player = Player(playerId, NexusFactionColor.Red);
        var src = new HexCoord(0, 0);
        var mid = new HexCoord(1, 0);
        var far = new HexCoord(2, 0);

        var tradeRoute = new NexusTradeRouteView(
            new HexCoord(-5, 0),
            Guid.NewGuid(),
            NexusFactionColor.Blue,
            src,
            playerId,
            NexusFactionColor.Red
        );

        var view = View(
            [Hex(src, redFleets: 1), Hex(mid), Hex(far)],
            tradeRoutes: [tradeRoute],
            currentPlayer: player
        );

        var result = NexusGameViewQueries.GetValidMoveDestinations(view, playerId, src);

        Assert.Contains(far, result);
    }

    [Fact]
    public void GetValidMoveDestinations_SpeedBonus_NoDuplicates()
    {
        var playerId = Guid.NewGuid();
        var player = Player(playerId, NexusFactionColor.Red);
        var src = new HexCoord(0, 0);

        var neighbours = src.GetNeighbours();
        var hexes = new List<NexusHexView> { Hex(src, redFleets: 1) };
        foreach (var n in neighbours)
            hexes.Add(Hex(n));

        var tradeRoute = new NexusTradeRouteView(
            src,
            playerId,
            NexusFactionColor.Red,
            new HexCoord(99, 0),
            Guid.NewGuid(),
            NexusFactionColor.Blue
        );

        var view = View(hexes, tradeRoutes: [tradeRoute], currentPlayer: player);

        var result = NexusGameViewQueries.GetValidMoveDestinations(view, playerId, src);

        Assert.Equal(result.Count, result.Distinct().Count());
    }

    // -------------------------------------------------------------------------
    // CanColonize
    // -------------------------------------------------------------------------

    [Fact]
    public void CanColonize_HexNotFound_ReturnsFalse()
    {
        var playerId = Guid.NewGuid();
        var view = View([Hex(new HexCoord(0, 0))]);

        Assert.False(NexusGameViewQueries.CanColonize(view, playerId, new HexCoord(9, 9)));
    }

    [Fact]
    public void CanColonize_NexusHex_ReturnsFalse()
    {
        var playerId = Guid.NewGuid();

        var view = View([Hex(new HexCoord(0, 0), isNexus: true)]);

        Assert.False(NexusGameViewQueries.CanColonize(view, playerId, new HexCoord(0, 0)));
    }

    [Fact]
    public void CanColonize_OwnColony_ReturnsFalse()
    {
        var playerId = Guid.NewGuid();

        var view = View([Hex(new HexCoord(1, 0), colonyOwnerId: playerId)]);

        Assert.False(NexusGameViewQueries.CanColonize(view, playerId, new HexCoord(1, 0)));
    }

    [Fact]
    public void CanColonize_UnownedHex_ReturnsTrue()
    {
        var playerId = Guid.NewGuid();

        var view = View([Hex(new HexCoord(1, 0), colonyOwnerId: null)]);

        Assert.True(NexusGameViewQueries.CanColonize(view, playerId, new HexCoord(1, 0)));
    }

    [Fact]
    public void CanColonize_OpponentColony_ReturnsTrue()
    {
        var playerId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();

        var view = View([Hex(new HexCoord(1, 0), colonyOwnerId: opponentId)]);

        Assert.True(NexusGameViewQueries.CanColonize(view, playerId, new HexCoord(1, 0)));
    }
}

namespace Spx.Nexus.Domain.Tests;

public class NexusGameViewQueriesTests
{
    // ─── helpers ────────────────────────────────────────────────────────────

    private static NexusSystemView MakeSystem(
        HexCoord coord,
        params (Guid PlayerId, NexusUnitType Unit, int Count)[] units
    )
    {
        var dict = units
            .GroupBy(u => u.PlayerId)
            .ToImmutableDictionary(
                g => g.Key,
                g =>
                    g.Select(u => new NexusUnitStackGroup(u.Unit, u.Unit.Hull(), u.Count))
                        .ToImmutableArray()
            );
        return new NexusSystemView(coord, false, 2, null, null, dict);
    }

    private static NexusPlayerView MakePlayer(Guid id) =>
        new(
            id,
            NexusFactionColor.Red,
            0,
            NexusGateProgress.None,
            false,
            true,
            null,
            null,
            false,
            0,
            0
        );

    private static NexusGameView MakeView(Guid playerId, params NexusSystemView[] systems) =>
        new(
            Guid.NewGuid(),
            1,
            systems.ToImmutableArray(),
            MakePlayer(playerId),
            MakePlayer(Guid.NewGuid()),
            ImmutableArray<NexusResolveEvent>.Empty,
            null
        );

    // ─── GetValidMoveDestinations ────────────────────────────────────────────

    [Fact]
    public void GetValidMoveDestinations_SourceNotInView_ReturnsEmpty()
    {
        var playerId = Guid.NewGuid();
        var view = MakeView(playerId); // no systems at all

        var result = NexusViewQueries.GetValidMoveDestinations(view, playerId, new HexCoord(0, 0));

        Assert.Empty(result);
    }

    [Fact]
    public void GetValidMoveDestinations_NoPlayerUnitsAtSource_ReturnsEmpty()
    {
        var playerId = Guid.NewGuid();
        var source = new HexCoord(0, 0);
        // System exists but belongs to another player
        var otherId = Guid.NewGuid();
        var view = MakeView(playerId, MakeSystem(source, (otherId, NexusUnitType.Carrier, 1)));

        var result = NexusViewQueries.GetValidMoveDestinations(view, playerId, source);

        Assert.Empty(result);
    }

    [Fact]
    public void GetValidMoveDestinations_PlayerHasUnits_ReturnsValidAdjacentCoords()
    {
        var playerId = Guid.NewGuid();
        // Use center (0,0) — all 6 neighbours are within the 19-hex map radius
        var source = new HexCoord(0, 0);
        var view = MakeView(playerId, MakeSystem(source, (playerId, NexusUnitType.Carrier, 1)));

        var result = NexusViewQueries.GetValidMoveDestinations(view, playerId, source);

        Assert.Equal(6, result.Count);
        Assert.All(result, coord => Assert.True(NexusMap.IsValidCoord(coord)));
    }

    [Fact]
    public void GetValidMoveDestinations_CornerSystem_FiltersOffMapCoords()
    {
        var playerId = Guid.NewGuid();
        // P1 home (2,-2) has only 3 valid adjacent coords within the map
        var source = NexusMap.Player1HomeCoord;
        var view = MakeView(playerId, MakeSystem(source, (playerId, NexusUnitType.Infantry, 1)));

        var result = NexusViewQueries.GetValidMoveDestinations(view, playerId, source);

        Assert.Equal(3, result.Count);
        Assert.All(result, coord => Assert.True(NexusMap.IsValidCoord(coord)));
    }

    [Fact]
    public void GetValidMoveDestinations_NeverIncludesSource()
    {
        var playerId = Guid.NewGuid();
        var source = new HexCoord(0, 0);
        var view = MakeView(playerId, MakeSystem(source, (playerId, NexusUnitType.Carrier, 1)));

        var result = NexusViewQueries.GetValidMoveDestinations(view, playerId, source);

        Assert.DoesNotContain(source, result);
    }
}

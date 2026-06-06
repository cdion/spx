using System.Collections.Immutable;
using Spx.Nexus.Domain;

namespace Spx.Nexus.Domain.Tests;

public class NexusGameViewQueriesTests
{
    // ─── helpers ────────────────────────────────────────────────────────────

    private static readonly NexusUnitDesign CapitalDesign = new()
    {
        DesignId = Guid.Parse("11111111-0000-0000-0000-000000000001"),
        Name = "Carrier",
        Hull = NexusUnitCategory.Capital,
        Modules =
        [
            new Battery(NexusUnitCategory.Strike),
            new Battery(NexusUnitCategory.Capital),
            new Hangar(8),
        ],
    };

    private static readonly NexusUnitDesign PlanetaryDesign = new()
    {
        DesignId = Guid.Parse("22222222-0000-0000-0000-000000000002"),
        Name = "Infantry",
        Hull = NexusUnitCategory.Planetary,
        Modules = [new Battery(NexusUnitCategory.Planetary), new Dock()],
    };

    private static NexusSystemView MakeSystem(
        HexCoord coord,
        params (Guid PlayerId, NexusUnitDesign Design, int Count)[] units
    )
    {
        var dict = units
            .GroupBy(u => u.PlayerId)
            .ToImmutableDictionary(
                g => g.Key,
                g =>
                    g.Select(u => new NexusUnitStackGroup(
                            u.Design.DesignId,
                            u.Design.Hull,
                            1,
                            u.Count
                        ))
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
        var otherId = Guid.NewGuid();
        var view = MakeView(playerId, MakeSystem(source, (otherId, CapitalDesign, 1)));

        var result = NexusViewQueries.GetValidMoveDestinations(view, playerId, source);

        Assert.Empty(result);
    }

    [Fact]
    public void GetValidMoveDestinations_PlayerHasUnits_ReturnsValidAdjacentCoords()
    {
        var playerId = Guid.NewGuid();
        var source = new HexCoord(0, 0);
        var view = MakeView(playerId, MakeSystem(source, (playerId, CapitalDesign, 1)));

        var result = NexusViewQueries.GetValidMoveDestinations(view, playerId, source);

        Assert.Equal(6, result.Count);
        Assert.All(result, coord => Assert.True(NexusMap.IsValidCoord(coord)));
    }

    [Fact]
    public void GetValidMoveDestinations_CornerSystem_FiltersOffMapCoords()
    {
        var playerId = Guid.NewGuid();
        var source = NexusMap.Player1HomeCoord;
        var view = MakeView(playerId, MakeSystem(source, (playerId, PlanetaryDesign, 1)));

        var result = NexusViewQueries.GetValidMoveDestinations(view, playerId, source);

        Assert.Equal(3, result.Count);
        Assert.All(result, coord => Assert.True(NexusMap.IsValidCoord(coord)));
    }

    [Fact]
    public void GetValidMoveDestinations_NeverIncludesSource()
    {
        var playerId = Guid.NewGuid();
        var source = new HexCoord(0, 0);
        var view = MakeView(playerId, MakeSystem(source, (playerId, CapitalDesign, 1)));

        var result = NexusViewQueries.GetValidMoveDestinations(view, playerId, source);

        Assert.DoesNotContain(source, result);
    }

    [Fact]
    public void GetValidMoveDestinations_PlanetaryOnlyInContestedSystem_ReturnsEmpty()
    {
        var playerId = Guid.NewGuid();
        var source = new HexCoord(0, 0);
        var unitStacks = ImmutableDictionary<Guid, ImmutableArray<NexusUnitStackGroup>>.Empty.Add(
            playerId,
            [new NexusUnitStackGroup(PlanetaryDesign.DesignId, PlanetaryDesign.Hull, 1, 1)]
        );
        // MovableUnitStacks is empty — planetary units are pinned in a contested system
        var movableStacks = ImmutableDictionary<Guid, ImmutableArray<NexusUnitStackGroup>>.Empty;
        var view = MakeView(
            playerId,
            new NexusSystemView(source, false, 2, null, null, unitStacks, movableStacks)
        );

        var result = NexusViewQueries.GetValidMoveDestinations(view, playerId, source);

        Assert.Empty(result);
    }
}

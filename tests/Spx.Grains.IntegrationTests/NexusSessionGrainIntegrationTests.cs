using System.Collections.Immutable;
using Orleans;
using Spx.Contracts;
using Spx.Nexus.Domain;
using Xunit;

namespace Spx.Grains.IntegrationTests;

[Collection(OrleansClusterCollection.Name)]
public sealed class GameSessionGrainIntegrationTests(OrleansClusterFixture fixture)
{
    private static readonly Guid GameId = Guid.Parse("a4ad04ba-7717-4684-bb8c-7827b2c01fb9");
    private static readonly Guid FirstPlayerId = Guid.Parse("85b56bc8-bd95-48f5-8374-f53714734717");
    private static readonly Guid SecondPlayerId = Guid.Parse(
        "6421fe5a-5585-4db9-b48b-e6caf8323b8f"
    );

    private static InitializeNexusGameCommand MakeInitCommand() =>
        new(
            ImmutableArray.Create(
                new NexusSessionPlayer(FirstPlayerId),
                new NexusSessionPlayer(SecondPlayerId)
            )
        );

    private static NexusTurnOrdersCommand EmptyOrders(Guid playerId, int round) =>
        new(playerId, round, [], [], false);

    [Fact]
    public async Task GetViewAsync_returns_null_before_initialize()
    {
        var grain = fixture.Cluster.Client.GetGrain<INexusSessionGrain>(
            Guid.Parse("00000000-0000-0000-0000-000000000001")
        );

        var view = await grain.GetViewAsync(FirstPlayerId);

        Assert.Null(view);
    }

    [Fact]
    public async Task InitializeAsync_then_GetViewAsync_returns_planning_view()
    {
        var grain = fixture.Cluster.Client.GetGrain<INexusSessionGrain>(
            Guid.Parse("00000000-0000-0000-0000-000000000002")
        );
        await grain.InitializeAsync(MakeInitCommand());

        var view = await grain.GetViewAsync(FirstPlayerId);

        Assert.NotNull(view);
        Assert.Equal(1, view!.RoundNumber);
        Assert.Null(view.Completion);
    }

    [Fact]
    public async Task SubmitOrdersAsync_accepts_valid_empty_orders()
    {
        var grain = fixture.Cluster.Client.GetGrain<INexusSessionGrain>(
            Guid.Parse("00000000-0000-0000-0000-000000000003")
        );
        await grain.InitializeAsync(MakeInitCommand());

        var view = await grain.GetViewAsync(FirstPlayerId);
        Assert.NotNull(view);

        var firstTurnPlayerId = view!.CurrentPlayer.PlayerId;
        var result = await grain.SubmitOrdersAsync(EmptyOrders(firstTurnPlayerId, 1));

        Assert.IsType<NexusTurnOrdersAccepted>(result);
    }

    [Fact]
    public async Task SessionState_survives_deactivation_and_reactivation()
    {
        var grain = fixture.Cluster.Client.GetGrain<INexusSessionGrain>(GameId);
        await grain.InitializeAsync(MakeInitCommand());

        var view = await grain.GetViewAsync(FirstPlayerId);
        Assert.NotNull(view);
        var firstTurnPlayerId = view!.CurrentPlayer.PlayerId;
        var secondTurnPlayerId = view.Opponent.PlayerId;

        // Both players submit empty orders to advance the round
        await grain.SubmitOrdersAsync(EmptyOrders(firstTurnPlayerId, 1));
        await grain.SubmitOrdersAsync(EmptyOrders(secondTurnPlayerId, 1));

        var round2View = await grain.GetViewAsync(FirstPlayerId);
        Assert.Equal(2, round2View!.RoundNumber);

        // Wait for grain to deactivate
        await Task.Delay(TimeSpan.FromSeconds(4));

        var rejoinedView = await grain.GetViewAsync(FirstPlayerId);
        Assert.NotNull(rejoinedView);
        Assert.Equal(2, rejoinedView!.RoundNumber);
    }

    [Fact]
    public async Task SubmitOrdersAsync_accepts_move_order_and_round_trips_serialization()
    {
        var grain = fixture.Cluster.Client.GetGrain<INexusSessionGrain>(
            Guid.Parse("00000000-0000-0000-0000-000000000005")
        );
        await grain.InitializeAsync(MakeInitCommand());

        var view = await grain.GetViewAsync(FirstPlayerId);
        Assert.NotNull(view);

        var currentPlayerId = view!.CurrentPlayer.PlayerId;
        var systemWithFleet = view.Systems.First(s =>
            s.GetPlayerUnitCounts(currentPlayerId).Values.Sum() > 0
        );
        var destination = NexusViewQueries.GetValidMoveDestinations(
            view,
            currentPlayerId,
            systemWithFleet.Coord
        )[0];
        var moveUnit = systemWithFleet
            .GetPlayerUnitCounts(currentPlayerId)
            .First(kv => kv.Value > 0)
            .Key;

        var result = await grain.SubmitOrdersAsync(
            new NexusTurnOrdersCommand(
                currentPlayerId,
                1,
                [
                    new NexusMoveOrder(
                        systemWithFleet.Coord,
                        destination,
                        ImmutableArray.Create(new NexusUnitStackGroup(moveUnit, moveUnit.Hull(), 1))
                    ),
                ],
                [],
                false
            )
        );

        Assert.IsType<NexusTurnOrdersAccepted>(result);
    }

    [Fact]
    public async Task AbandonAsync_ends_game_with_opponent_as_winner()
    {
        var grain = fixture.Cluster.Client.GetGrain<INexusSessionGrain>(
            Guid.Parse("00000000-0000-0000-0000-000000000004")
        );
        await grain.InitializeAsync(MakeInitCommand());

        var view = await grain.GetViewAsync(FirstPlayerId);
        Assert.NotNull(view);
        var abandoningPlayerId = view!.CurrentPlayer.PlayerId;
        var expectedWinnerId = view.Opponent.PlayerId;

        await grain.AbandonAsync(abandoningPlayerId);

        var finalView = await grain.GetViewAsync(FirstPlayerId);
        Assert.NotNull(finalView);
        Assert.NotNull(finalView!.Completion);
        Assert.Equal(NexusGameOutcome.Victory, finalView.Completion!.Outcome);
        Assert.Equal(expectedWinnerId, finalView.Completion.WinnerId);
    }
}

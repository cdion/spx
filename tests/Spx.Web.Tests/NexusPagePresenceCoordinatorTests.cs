using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Orleans;
using Spx.Contracts;
using Spx.Game.Application;
using Spx.Game.Application.Features.GetGamePresence;
using Spx.Web.Components.Pages.Nexus;
using Spx.Web.Hubs;
using Spx.Web.Presence;
using Xunit;

namespace Spx.Web.Tests;

public sealed class NexusPagePresenceCoordinatorTests
{
    [Fact]
    public async Task ConnectAsync_subscribes_and_registers_presence_for_active_player()
    {
        var gameId = Guid.NewGuid();
        var expectedPresence = new GamePresenceView([GamePageCoordinatorTestData.CurrentPlayerId]);
        var coordinator = CreateCoordinator(
            out var data,
            out var presenceState,
            out var invalidationNotifier,
            out var grain,
            getGamePresenceHandler: new StubGetGamePresenceHandler { Result = expectedPresence }
        );
        var subscriber = Substitute.For<IGameInvalidationSubscriber>();
        data.ApplyPage(GamePageCoordinatorTestData.CreatePage(gameId));

        await coordinator.ConnectAsync(gameId, subscriber);

        await invalidationNotifier.Received(1).SubscribeAsync(gameId, subscriber);
        await grain
            .Received(1)
            .RenewLeaseAsync(
                GamePageCoordinatorTestData.CurrentPlayerId,
                Arg.Any<Guid>(),
                TimeSpan.FromSeconds(45)
            );
        Assert.Equal(expectedPresence, data.Presence);
        Assert.True(presenceState.IsConnectedTo(gameId));
        Assert.True(presenceState.IsPresenceRegistered);
    }

    [Fact]
    public async Task ConnectAsync_does_not_register_presence_for_former_player()
    {
        var gameId = Guid.NewGuid();
        var coordinator = CreateCoordinator(
            out var data,
            out var presenceState,
            out var invalidationNotifier,
            out var grain
        );
        var subscriber = Substitute.For<IGameInvalidationSubscriber>();
        data.ApplyPage(
            new GamePageView(
                GamePageCoordinatorTestData.CreateLobby(gameId, isCurrentUserActive: false),
                null,
                GamePresenceView.Empty
            )
        );

        await coordinator.ConnectAsync(gameId, subscriber);

        await invalidationNotifier.Received(1).SubscribeAsync(gameId, subscriber);
        await grain.DidNotReceiveWithAnyArgs().RenewLeaseAsync(default, default, default);
        Assert.True(presenceState.IsConnectedTo(gameId));
        Assert.False(presenceState.IsPresenceRegistered);
    }

    [Fact]
    public async Task SyncAsync_replaces_presence_lease_when_current_player_changes()
    {
        var gameId = Guid.NewGuid();
        var nextPlayerId = Guid.NewGuid();
        var coordinator = CreateCoordinator(out var data, out _, out _, out var grain);
        var subscriber = Substitute.For<IGameInvalidationSubscriber>();
        data.ApplyPage(GamePageCoordinatorTestData.CreatePage(gameId));

        await coordinator.ConnectAsync(gameId, subscriber);

        var firstRenewCall = Assert.Single(
            grain.ReceivedCalls(),
            call => call.GetMethodInfo().Name == nameof(IGamePresenceGrain.RenewLeaseAsync)
        );
        var firstLeaseId = Assert.IsType<Guid>(firstRenewCall.GetArguments()[1]);

        data.ApplyPage(
            new GamePageView(
                GamePageCoordinatorTestData.CreateLobby(gameId) with
                {
                    CurrentPlayerId = nextPlayerId,
                    CurrentPlayerName = "Captain Green",
                },
                null,
                data.Presence
            )
        );

        await coordinator.SyncAsync();

        await grain.Received(1).RevokeLeaseAsync(firstLeaseId);
        var renewCalls = grain
            .ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IGamePresenceGrain.RenewLeaseAsync))
            .ToArray();
        Assert.Equal(2, renewCalls.Length);
        Assert.Equal(nextPlayerId, Assert.IsType<Guid>(renewCalls[1].GetArguments()[0]));
        Assert.NotEqual(firstLeaseId, Assert.IsType<Guid>(renewCalls[1].GetArguments()[1]));
    }

    [Fact]
    public async Task ChangeGameAsync_unsubscribes_old_game_and_connects_new_game()
    {
        var firstGameId = Guid.NewGuid();
        var secondGameId = Guid.NewGuid();
        var invalidationNotifier = Substitute.For<IGameInvalidationNotifier>();
        var clusterClient = Substitute.For<IClusterClient>();
        var firstGrain = Substitute.For<IGamePresenceGrain>();
        var secondGrain = Substitute.For<IGamePresenceGrain>();
        clusterClient.GetGrain<IGamePresenceGrain>(firstGameId).Returns(firstGrain);
        clusterClient.GetGrain<IGamePresenceGrain>(secondGameId).Returns(secondGrain);
        var data = new NexusPageDataState();
        var presenceState = new NexusPagePresenceState();
        var coordinator = new NexusPagePresenceCoordinator(
            new StubGetGamePresenceHandler(),
            invalidationNotifier,
            new NexusPresenceLeaseCoordinator(clusterClient),
            NullLogger<NexusPagePresenceCoordinator>.Instance,
            data,
            presenceState
        );
        var subscriber = Substitute.For<IGameInvalidationSubscriber>();
        data.ApplyPage(GamePageCoordinatorTestData.CreatePage(firstGameId));

        await coordinator.ConnectAsync(firstGameId, subscriber);

        var firstRenewCall = Assert.Single(
            firstGrain.ReceivedCalls(),
            call => call.GetMethodInfo().Name == nameof(IGamePresenceGrain.RenewLeaseAsync)
        );
        var firstLeaseId = Assert.IsType<Guid>(firstRenewCall.GetArguments()[1]);

        data.ApplyPage(GamePageCoordinatorTestData.CreatePage(secondGameId));

        await coordinator.ChangeGameAsync(secondGameId, subscriber);

        await invalidationNotifier.Received(1).UnsubscribeAsync(firstGameId, subscriber);
        await invalidationNotifier.Received(1).SubscribeAsync(secondGameId, subscriber);
        await firstGrain.Received(1).RevokeLeaseAsync(firstLeaseId);
        await secondGrain
            .Received(1)
            .RenewLeaseAsync(
                GamePageCoordinatorTestData.CurrentPlayerId,
                Arg.Any<Guid>(),
                TimeSpan.FromSeconds(45)
            );
        Assert.True(presenceState.IsConnectedTo(secondGameId));
    }

    [Fact]
    public async Task SyncAsync_revokes_presence_when_current_user_becomes_inactive()
    {
        var gameId = Guid.NewGuid();
        var coordinator = CreateCoordinator(
            out var data,
            out var presenceState,
            out _,
            out var grain
        );
        var subscriber = Substitute.For<IGameInvalidationSubscriber>();
        data.ApplyPage(GamePageCoordinatorTestData.CreatePage(gameId));

        await coordinator.ConnectAsync(gameId, subscriber);

        var firstRenewCall = Assert.Single(
            grain.ReceivedCalls(),
            call => call.GetMethodInfo().Name == nameof(IGamePresenceGrain.RenewLeaseAsync)
        );
        var firstLeaseId = Assert.IsType<Guid>(firstRenewCall.GetArguments()[1]);

        data.ApplyPage(
            new GamePageView(
                GamePageCoordinatorTestData.CreateLobby(gameId, isCurrentUserActive: false),
                null,
                data.Presence
            )
        );

        await coordinator.SyncAsync();

        await grain.Received(1).RevokeLeaseAsync(firstLeaseId);
        Assert.False(presenceState.IsPresenceRegistered);
    }

    [Fact]
    public async Task LoadPresenceAsync_clears_presence_when_lobby_is_missing()
    {
        var coordinator = CreateCoordinator(out var data, out _, out _, out _);
        data.ApplyPresence(new GamePresenceView([GamePageCoordinatorTestData.CurrentPlayerId]));

        await coordinator.LoadPresenceAsync();

        Assert.Equal(GamePresenceView.Empty, data.Presence);
    }

    private static NexusPagePresenceCoordinator CreateCoordinator(
        out NexusPageDataState data,
        out NexusPagePresenceState presenceState,
        out IGameInvalidationNotifier invalidationNotifier,
        out IGamePresenceGrain grain,
        StubGetGamePresenceHandler? getGamePresenceHandler = null
    )
    {
        data = new NexusPageDataState();
        presenceState = new NexusPagePresenceState();
        invalidationNotifier = Substitute.For<IGameInvalidationNotifier>();
        var clusterClient = Substitute.For<IClusterClient>();
        grain = Substitute.For<IGamePresenceGrain>();
        clusterClient.GetGrain<IGamePresenceGrain>(Arg.Any<Guid>()).Returns(grain);

        return new NexusPagePresenceCoordinator(
            getGamePresenceHandler ?? new StubGetGamePresenceHandler(),
            invalidationNotifier,
            new NexusPresenceLeaseCoordinator(clusterClient),
            NullLogger<NexusPagePresenceCoordinator>.Instance,
            data,
            presenceState
        );
    }

    private sealed class StubGetGamePresenceHandler : IGetGamePresenceHandler
    {
        public GamePresenceView Result { get; init; } = GamePresenceView.Empty;

        public Exception? Exception { get; init; }

        public Task<GamePresenceView> HandleAsync(
            Guid gameId,
            CancellationToken cancellationToken = default
        ) =>
            Exception is null
                ? Task.FromResult(Result)
                : Task.FromException<GamePresenceView>(Exception);
    }
}

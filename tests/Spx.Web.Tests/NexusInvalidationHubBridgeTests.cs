using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Orleans;
using Spx.Contracts;
using Spx.Web.Hubs;
using Xunit;

namespace Spx.Web.Tests;

public sealed class NexusInvalidationHubBridgeTests
{
    [Fact]
    public async Task SubscribeAsync_tracks_local_subscribers_with_a_single_orleans_subscription()
    {
        var gameId = Guid.NewGuid();
        var hubContext = CreateHubContext();
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<ILobbyInvalidationGrain>();
        var observer = Substitute.For<ILobbyInvalidationObserver>();
        clusterClient.GetGrain<ILobbyInvalidationGrain>(gameId).Returns(grain);
        clusterClient
            .CreateObjectReference<ILobbyInvalidationObserver>(
                Arg.Any<ILobbyInvalidationObserver>()
            )
            .Returns(observer);
        var bridge = new NexusInvalidationHubBridge(hubContext, clusterClient);
        var firstSubscriber = Substitute.For<IGameInvalidationSubscriber>();
        var secondSubscriber = Substitute.For<IGameInvalidationSubscriber>();

        await bridge.StartAsync(CancellationToken.None);
        await bridge.SubscribeAsync(gameId, firstSubscriber);
        await bridge.SubscribeAsync(gameId, secondSubscriber);
        await bridge.UnsubscribeAsync(gameId, firstSubscriber);

        await grain.Received(1).Subscribe(observer);
        await grain.DidNotReceive().Unsubscribe(observer);

        await bridge.UnsubscribeAsync(gameId, secondSubscriber);

        await grain.Received(1).Unsubscribe(observer);
    }

    [Fact]
    public async Task Hub_disconnect_does_not_unsubscribe_while_local_subscriber_remains()
    {
        var gameId = Guid.NewGuid();
        var hubContext = CreateHubContext();
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<ILobbyInvalidationGrain>();
        var observer = Substitute.For<ILobbyInvalidationObserver>();
        clusterClient.GetGrain<ILobbyInvalidationGrain>(gameId).Returns(grain);
        clusterClient
            .CreateObjectReference<ILobbyInvalidationObserver>(
                Arg.Any<ILobbyInvalidationObserver>()
            )
            .Returns(observer);
        var bridge = new NexusInvalidationHubBridge(hubContext, clusterClient);
        var subscriber = Substitute.For<IGameInvalidationSubscriber>();

        await bridge.StartAsync(CancellationToken.None);
        await bridge.SubscribeAsync(gameId, subscriber);
        await bridge.OnGameConnectedAsync(gameId);
        await bridge.OnGameDisconnectedAsync(gameId);

        await grain.Received(1).Subscribe(observer);
        await grain.DidNotReceive().Unsubscribe(observer);

        await bridge.UnsubscribeAsync(gameId, subscriber);

        await grain.Received(1).Unsubscribe(observer);
    }

    private static IHubContext<NexusHub> CreateHubContext()
    {
        var hubContext = Substitute.For<IHubContext<NexusHub>>();
        var clients = Substitute.For<IHubClients>();
        var clientProxy = Substitute.For<IClientProxy>();

        hubContext.Clients.Returns(clients);
        clients.Group(Arg.Any<string>()).Returns(clientProxy);

        return hubContext;
    }
}

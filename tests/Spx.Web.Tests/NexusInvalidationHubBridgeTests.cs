using Microsoft.Extensions.Logging.Abstractions;
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
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<ILobbyInvalidationGrain>();
        var observer = Substitute.For<ILobbyInvalidationObserver>();
        clusterClient.GetGrain<ILobbyInvalidationGrain>(gameId).Returns(grain);
        clusterClient
            .CreateObjectReference<ILobbyInvalidationObserver>(
                Arg.Any<ILobbyInvalidationObserver>()
            )
            .Returns(observer);
        var bridge = new NexusInvalidationHubBridge(
            clusterClient,
            NullLogger<NexusInvalidationHubBridge>.Instance
        );
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
    public async Task SubscribeAsync_does_not_duplicate_orleans_subscription_for_same_subscriber()
    {
        var gameId = Guid.NewGuid();
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<ILobbyInvalidationGrain>();
        var observer = Substitute.For<ILobbyInvalidationObserver>();
        clusterClient.GetGrain<ILobbyInvalidationGrain>(gameId).Returns(grain);
        clusterClient
            .CreateObjectReference<ILobbyInvalidationObserver>(
                Arg.Any<ILobbyInvalidationObserver>()
            )
            .Returns(observer);
        var bridge = new NexusInvalidationHubBridge(
            clusterClient,
            NullLogger<NexusInvalidationHubBridge>.Instance
        );
        var subscriber = Substitute.For<IGameInvalidationSubscriber>();

        await bridge.StartAsync(CancellationToken.None);
        await bridge.SubscribeAsync(gameId, subscriber);
        await bridge.SubscribeAsync(gameId, subscriber);

        await grain.Received(1).Subscribe(observer);
        await grain.DidNotReceive().Unsubscribe(observer);

        await bridge.UnsubscribeAsync(gameId, subscriber);

        await grain.Received(1).Unsubscribe(observer);
    }

    [Fact]
    public async Task PresenceInvalidation_notifies_local_subscribers()
    {
        var gameId = Guid.NewGuid();
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<ILobbyInvalidationGrain>();
        var observer = Substitute.For<ILobbyInvalidationObserver>();
        clusterClient.GetGrain<ILobbyInvalidationGrain>(gameId).Returns(grain);
        clusterClient
            .CreateObjectReference<ILobbyInvalidationObserver>(
                Arg.Any<ILobbyInvalidationObserver>()
            )
            .Returns(observer);
        var bridge = new NexusInvalidationHubBridge(
            clusterClient,
            NullLogger<NexusInvalidationHubBridge>.Instance
        );
        var subscriber = Substitute.For<IGameInvalidationSubscriber>();

        await bridge.StartAsync(CancellationToken.None);
        await bridge.SubscribeAsync(gameId, subscriber);

        bridge.OnPresenceInvalidated(gameId);

        await subscriber.Received(1).OnPresenceChangedAsync(gameId);
    }

    [Fact]
    public async Task PresenceInvalidation_drops_failed_subscriber_and_continues_notifying_live_subscribers()
    {
        var gameId = Guid.NewGuid();
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<ILobbyInvalidationGrain>();
        var observer = Substitute.For<ILobbyInvalidationObserver>();
        clusterClient.GetGrain<ILobbyInvalidationGrain>(gameId).Returns(grain);
        clusterClient
            .CreateObjectReference<ILobbyInvalidationObserver>(
                Arg.Any<ILobbyInvalidationObserver>()
            )
            .Returns(observer);
        var bridge = new NexusInvalidationHubBridge(
            clusterClient,
            NullLogger<NexusInvalidationHubBridge>.Instance
        );
        var failedSubscriber = new TrackingSubscriber(throwOnPresenceChanged: true);
        var healthySubscriber = new TrackingSubscriber();

        await bridge.StartAsync(CancellationToken.None);
        await bridge.SubscribeAsync(gameId, failedSubscriber);
        await bridge.SubscribeAsync(gameId, healthySubscriber);

        bridge.OnPresenceInvalidated(gameId);
        await Task.Delay(10);

        Assert.Equal(1, failedSubscriber.PresenceChangedCount);
        Assert.Equal(1, healthySubscriber.PresenceChangedCount);

        bridge.OnPresenceInvalidated(gameId);
        await Task.Delay(10);

        Assert.Equal(1, failedSubscriber.PresenceChangedCount);
        Assert.Equal(2, healthySubscriber.PresenceChangedCount);
        await grain.DidNotReceive().Unsubscribe(observer);
    }

    private sealed class TrackingSubscriber(bool throwOnPresenceChanged = false)
        : IGameInvalidationSubscriber
    {
        public int PresenceChangedCount { get; private set; }

        public Task OnGameStateChangedAsync(Guid gameId) => Task.CompletedTask;

        public Task OnMessagesChangedAsync(Guid gameId) => Task.CompletedTask;

        public Task OnPresenceChangedAsync(Guid gameId)
        {
            PresenceChangedCount++;
            if (throwOnPresenceChanged)
            {
                throw new InvalidOperationException("subscriber disposed");
            }

            return Task.CompletedTask;
        }
    }
}

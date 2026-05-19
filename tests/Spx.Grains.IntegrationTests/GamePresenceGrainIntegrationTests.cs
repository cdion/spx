using Orleans;
using Spx.Contracts;
using Xunit;

namespace Spx.Grains.IntegrationTests;

[Collection(OrleansClusterCollection.Name)]
public sealed class GamePresenceGrainIntegrationTests(OrleansClusterFixture fixture)
{
    [Fact]
    public async Task PresenceExpiry_publishes_invalidation_without_a_read()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var observer = new PresenceObserver(gameId);
        var observerReference =
            fixture.Cluster.Client.CreateObjectReference<IGameInvalidationObserver>(observer);
        var invalidationGrain = fixture.Cluster.Client.GetGrain<IGameInvalidationGrain>(gameId);
        var presenceGrain = fixture.Cluster.Client.GetGrain<IGamePresenceGrain>(gameId);

        try
        {
            await presenceGrain.UpsertLeaseAsync(
                new UpsertGamePresenceLeaseCommand(
                    playerId,
                    connectionId,
                    DateTime.UtcNow.AddSeconds(2)
                )
            );

            await invalidationGrain.Subscribe(observerReference);

            var invalidated = await observer.TryWaitForPresenceInvalidationAsync(
                TimeSpan.FromSeconds(8)
            );
            var snapshot = await presenceGrain.GetSnapshotAsync();

            Assert.True(
                invalidated,
                $"Expected background expiry invalidation before timeout, but final online players were [{string.Join(", ", snapshot.OnlinePlayerIds)}]."
            );
            Assert.Empty(snapshot.OnlinePlayerIds);
        }
        finally
        {
            await invalidationGrain.Unsubscribe(observerReference);
            fixture.Cluster.Client.DeleteObjectReference<IGameInvalidationObserver>(
                observerReference
            );
        }
    }

    [Fact]
    public async Task RemoveLease_publishes_invalidation_through_orleans()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var observer = new PresenceObserver(gameId);
        var observerReference =
            fixture.Cluster.Client.CreateObjectReference<IGameInvalidationObserver>(observer);
        var invalidationGrain = fixture.Cluster.Client.GetGrain<IGameInvalidationGrain>(gameId);
        var presenceGrain = fixture.Cluster.Client.GetGrain<IGamePresenceGrain>(gameId);

        try
        {
            await presenceGrain.UpsertLeaseAsync(
                new UpsertGamePresenceLeaseCommand(
                    playerId,
                    connectionId,
                    DateTime.UtcNow.AddMinutes(1)
                )
            );

            await invalidationGrain.Subscribe(observerReference);

            await presenceGrain.RemoveLeaseAsync(
                new RemoveGamePresenceLeaseCommand(playerId, connectionId)
            );

            var invalidated = await observer.TryWaitForPresenceInvalidationAsync(
                TimeSpan.FromSeconds(2)
            );
            var snapshot = await presenceGrain.GetSnapshotAsync();

            Assert.True(
                invalidated,
                "Expected presence invalidation after removing the last lease."
            );
            Assert.Empty(snapshot.OnlinePlayerIds);
        }
        finally
        {
            await invalidationGrain.Unsubscribe(observerReference);
            fixture.Cluster.Client.DeleteObjectReference<IGameInvalidationObserver>(
                observerReference
            );
        }
    }

    [Fact]
    public async Task PresenceExpiry_does_not_publish_invalidation_when_another_connection_keeps_player_online()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var expiringConnectionId = Guid.NewGuid();
        var retainedConnectionId = Guid.NewGuid();
        var observer = new PresenceObserver(gameId);
        var observerReference =
            fixture.Cluster.Client.CreateObjectReference<IGameInvalidationObserver>(observer);
        var invalidationGrain = fixture.Cluster.Client.GetGrain<IGameInvalidationGrain>(gameId);
        var presenceGrain = fixture.Cluster.Client.GetGrain<IGamePresenceGrain>(gameId);

        try
        {
            await presenceGrain.UpsertLeaseAsync(
                new UpsertGamePresenceLeaseCommand(
                    playerId,
                    expiringConnectionId,
                    DateTime.UtcNow.AddSeconds(2)
                )
            );
            await presenceGrain.UpsertLeaseAsync(
                new UpsertGamePresenceLeaseCommand(
                    playerId,
                    retainedConnectionId,
                    DateTime.UtcNow.AddMinutes(1)
                )
            );

            await invalidationGrain.Subscribe(observerReference);

            var invalidated = await observer.TryWaitForPresenceInvalidationAsync(
                TimeSpan.FromSeconds(8)
            );
            var snapshot = await presenceGrain.GetSnapshotAsync();

            Assert.False(
                invalidated,
                "Did not expect presence invalidation while another connection still keeps the player online."
            );
            Assert.Equal([playerId], snapshot.OnlinePlayerIds);
        }
        finally
        {
            await invalidationGrain.Unsubscribe(observerReference);
            fixture.Cluster.Client.DeleteObjectReference<IGameInvalidationObserver>(
                observerReference
            );
        }
    }

    [Fact]
    public async Task RemoveLease_does_not_publish_invalidation_when_another_connection_keeps_player_online()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var removedConnectionId = Guid.NewGuid();
        var retainedConnectionId = Guid.NewGuid();
        var observer = new PresenceObserver(gameId);
        var observerReference =
            fixture.Cluster.Client.CreateObjectReference<IGameInvalidationObserver>(observer);
        var invalidationGrain = fixture.Cluster.Client.GetGrain<IGameInvalidationGrain>(gameId);
        var presenceGrain = fixture.Cluster.Client.GetGrain<IGamePresenceGrain>(gameId);

        try
        {
            await presenceGrain.UpsertLeaseAsync(
                new UpsertGamePresenceLeaseCommand(
                    playerId,
                    removedConnectionId,
                    DateTime.UtcNow.AddMinutes(1)
                )
            );
            await presenceGrain.UpsertLeaseAsync(
                new UpsertGamePresenceLeaseCommand(
                    playerId,
                    retainedConnectionId,
                    DateTime.UtcNow.AddMinutes(1)
                )
            );

            await invalidationGrain.Subscribe(observerReference);

            await presenceGrain.RemoveLeaseAsync(
                new RemoveGamePresenceLeaseCommand(playerId, removedConnectionId)
            );

            var invalidated = await observer.TryWaitForPresenceInvalidationAsync(
                TimeSpan.FromSeconds(2)
            );
            var snapshot = await presenceGrain.GetSnapshotAsync();

            Assert.False(
                invalidated,
                "Did not expect presence invalidation while another connection still keeps the player online."
            );
            Assert.Equal([playerId], snapshot.OnlinePlayerIds);
        }
        finally
        {
            await invalidationGrain.Unsubscribe(observerReference);
            fixture.Cluster.Client.DeleteObjectReference<IGameInvalidationObserver>(
                observerReference
            );
        }
    }

    private sealed class PresenceObserver(Guid expectedGameId) : IGameInvalidationObserver
    {
        private readonly TaskCompletionSource<Guid> completionSource = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        public void OnLobbyInvalidated(Guid gameId) { }

        public void OnSessionInvalidated(Guid gameId) { }

        public void OnMessagesInvalidated(Guid gameId) { }

        public void OnPresenceInvalidated(Guid gameId)
        {
            if (gameId != expectedGameId)
            {
                return;
            }

            completionSource.TrySetResult(gameId);
        }

        public async Task<bool> TryWaitForPresenceInvalidationAsync(TimeSpan timeout)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            try
            {
                await completionSource.Task.WaitAsync(timeoutCts.Token);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }
    }
}

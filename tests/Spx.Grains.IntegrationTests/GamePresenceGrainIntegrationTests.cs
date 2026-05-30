using Spx.Contracts;
using Xunit;

namespace Spx.Grains.IntegrationTests;

[Collection(OrleansClusterCollection.Name)]
public sealed class GamePresenceGrainIntegrationTests(OrleansClusterFixture fixture)
{
    [Fact]
    public async Task RenewLeaseAsync_adds_player_to_snapshot()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var leaseId = Guid.NewGuid();
        var grain = fixture.Cluster.Client.GetGrain<IGamePresenceGrain>(gameId);

        await grain.RenewLeaseAsync(playerId, leaseId, TimeSpan.FromMinutes(1));

        var snapshot = await grain.GetSnapshotAsync();
        Assert.Equal([playerId], snapshot.OnlinePlayerIds.ToArray());
    }

    [Fact]
    public async Task RenewLeaseAsync_keeps_player_online_when_multiple_leases_exist()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var firstLeaseId = Guid.NewGuid();
        var secondLeaseId = Guid.NewGuid();
        var grain = fixture.Cluster.Client.GetGrain<IGamePresenceGrain>(gameId);

        await grain.RenewLeaseAsync(playerId, firstLeaseId, TimeSpan.FromMinutes(1));
        await grain.RenewLeaseAsync(playerId, secondLeaseId, TimeSpan.FromMinutes(1));

        var snapshot = await grain.GetSnapshotAsync();
        Assert.Equal([playerId], snapshot.OnlinePlayerIds.ToArray());
    }

    [Fact]
    public async Task RevokeLeaseAsync_removes_player_when_last_lease_is_revoked()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var leaseId = Guid.NewGuid();
        var grain = fixture.Cluster.Client.GetGrain<IGamePresenceGrain>(gameId);

        await grain.RenewLeaseAsync(playerId, leaseId, TimeSpan.FromMinutes(1));
        await grain.RevokeLeaseAsync(leaseId);

        var snapshot = await grain.GetSnapshotAsync();
        Assert.Empty(snapshot.OnlinePlayerIds);
    }

    [Fact]
    public async Task RevokeLeaseAsync_publishes_presence_invalidation_when_online_players_change()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var leaseId = Guid.NewGuid();
        var grain = fixture.Cluster.Client.GetGrain<IGamePresenceGrain>(gameId);
        var invalidationGrain = fixture.Cluster.Client.GetGrain<ILobbyInvalidationGrain>(gameId);
        var observer = new RecordingInvalidationObserver();
        var observerReference =
            fixture.Cluster.Client.CreateObjectReference<ILobbyInvalidationObserver>(observer);

        await invalidationGrain.Subscribe(observerReference);
        await grain.RenewLeaseAsync(playerId, leaseId, TimeSpan.FromMinutes(1));
        await observer.WaitForPresenceInvalidatedAsync(TimeSpan.FromSeconds(5));

        observer.ResetPresenceInvalidated();

        await grain.RevokeLeaseAsync(leaseId);

        await observer.WaitForPresenceInvalidatedAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, observer.PresenceInvalidationCount);

        await invalidationGrain.Unsubscribe(observerReference);
    }

    [Fact]
    public async Task RevokeLeaseAsync_only_removes_the_specified_lease()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var firstLeaseId = Guid.NewGuid();
        var secondLeaseId = Guid.NewGuid();
        var grain = fixture.Cluster.Client.GetGrain<IGamePresenceGrain>(gameId);

        await grain.RenewLeaseAsync(playerId, firstLeaseId, TimeSpan.FromMinutes(1));
        await grain.RenewLeaseAsync(playerId, secondLeaseId, TimeSpan.FromMinutes(1));
        await grain.RevokeLeaseAsync(firstLeaseId);

        var snapshot = await grain.GetSnapshotAsync();
        Assert.Equal([playerId], snapshot.OnlinePlayerIds.ToArray());
    }

    [Fact]
    public async Task GetSnapshotAsync_ignores_expired_leases()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var leaseId = Guid.NewGuid();
        var grain = fixture.Cluster.Client.GetGrain<IGamePresenceGrain>(gameId);

        await grain.RenewLeaseAsync(playerId, leaseId, TimeSpan.FromMilliseconds(100));
        await Task.Delay(200);

        var snapshot = await grain.GetSnapshotAsync();
        Assert.Empty(snapshot.OnlinePlayerIds);
    }

    [Fact]
    public async Task ExpiredLease_publishes_presence_invalidation_when_online_players_change()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var leaseId = Guid.NewGuid();
        var grain = fixture.Cluster.Client.GetGrain<IGamePresenceGrain>(gameId);
        var invalidationGrain = fixture.Cluster.Client.GetGrain<ILobbyInvalidationGrain>(gameId);
        var observer = new RecordingInvalidationObserver();
        var observerReference =
            fixture.Cluster.Client.CreateObjectReference<ILobbyInvalidationObserver>(observer);

        await invalidationGrain.Subscribe(observerReference);
        await grain.RenewLeaseAsync(playerId, leaseId, TimeSpan.FromMilliseconds(100));
        await observer.WaitForPresenceInvalidatedAsync(TimeSpan.FromSeconds(5));

        observer.ResetPresenceInvalidated();

        await observer.WaitForPresenceInvalidatedAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(1, observer.PresenceInvalidationCount);

        await invalidationGrain.Unsubscribe(observerReference);
    }

    private sealed class RecordingInvalidationObserver : ILobbyInvalidationObserver
    {
        private TaskCompletionSource presenceInvalidatedSource = CreateSource();

        public int PresenceInvalidationCount { get; private set; }

        public void OnLobbyInvalidated(Guid gameId) { }

        public void OnSessionInvalidated(Guid gameId) { }

        public void OnMessagesInvalidated(Guid gameId) { }

        public void OnPresenceInvalidated(Guid gameId)
        {
            PresenceInvalidationCount++;
            presenceInvalidatedSource.TrySetResult();
        }

        public void ResetPresenceInvalidated()
        {
            PresenceInvalidationCount = 0;
            presenceInvalidatedSource = CreateSource();
        }

        public async Task WaitForPresenceInvalidatedAsync(TimeSpan timeout)
        {
            using var cancellationTokenSource = new CancellationTokenSource(timeout);
            await presenceInvalidatedSource.Task.WaitAsync(cancellationTokenSource.Token);
        }

        private static TaskCompletionSource CreateSource() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}

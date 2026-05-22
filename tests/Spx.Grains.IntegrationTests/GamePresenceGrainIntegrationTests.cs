using Spx.Contracts;
using Xunit;

namespace Spx.Grains.IntegrationTests;

[Collection(OrleansClusterCollection.Name)]
public sealed class GamePresenceGrainIntegrationTests(OrleansClusterFixture fixture)
{
    [Fact]
    public async Task SetOnlineAsync_adds_player_to_snapshot()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var grain = fixture.Cluster.Client.GetGrain<IGamePresenceGrain>(gameId);

        await grain.SetOnlineAsync(playerId);

        var snapshot = await grain.GetSnapshotAsync();
        Assert.Equal([playerId], snapshot.OnlinePlayerIds.ToArray());
    }

    [Fact]
    public async Task SetOnlineAsync_is_idempotent_for_same_player()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var grain = fixture.Cluster.Client.GetGrain<IGamePresenceGrain>(gameId);

        await grain.SetOnlineAsync(playerId);
        await grain.SetOnlineAsync(playerId);

        var snapshot = await grain.GetSnapshotAsync();
        Assert.Equal([playerId], snapshot.OnlinePlayerIds.ToArray());
    }

    [Fact]
    public async Task SetOfflineAsync_removes_player_from_snapshot()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var grain = fixture.Cluster.Client.GetGrain<IGamePresenceGrain>(gameId);

        await grain.SetOnlineAsync(playerId);
        await grain.SetOfflineAsync(playerId);

        var snapshot = await grain.GetSnapshotAsync();
        Assert.Empty(snapshot.OnlinePlayerIds);
    }

    [Fact]
    public async Task SetOfflineAsync_is_safe_when_player_was_not_online()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var grain = fixture.Cluster.Client.GetGrain<IGamePresenceGrain>(gameId);

        await grain.SetOfflineAsync(playerId);

        var snapshot = await grain.GetSnapshotAsync();
        Assert.Empty(snapshot.OnlinePlayerIds);
    }

    [Fact]
    public async Task SetOfflineAsync_only_removes_the_specified_player()
    {
        var gameId = Guid.NewGuid();
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();
        var grain = fixture.Cluster.Client.GetGrain<IGamePresenceGrain>(gameId);

        await grain.SetOnlineAsync(playerA);
        await grain.SetOnlineAsync(playerB);
        await grain.SetOfflineAsync(playerA);

        var snapshot = await grain.GetSnapshotAsync();
        Assert.Equal([playerB], snapshot.OnlinePlayerIds.ToArray());
    }
}

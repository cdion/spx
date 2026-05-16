using Orleans.Runtime;
using Spx.Grains;
using Xunit;

namespace Spx.Grains.Tests;

public sealed class GamePresenceGrainTests
{
    [Fact]
    public void GamePresenceGrain_does_not_use_persistent_state()
    {
        Assert.Equal(typeof(Grain), typeof(GamePresenceGrain).BaseType);

        var constructorParameters = typeof(GamePresenceGrain)
            .GetConstructors()
            .SelectMany(constructor => constructor.GetParameters());

        Assert.DoesNotContain(
            constructorParameters,
            parameter => parameter.ParameterType.IsGenericType
                && parameter.ParameterType.GetGenericTypeDefinition() == typeof(IPersistentState<>));
    }

    [Fact]
    public void UpsertLease_marks_player_online_when_first_connection_is_added()
    {
        var leasesByPlayerId = new Dictionary<Guid, Dictionary<Guid, DateTime>>();
        var playerId = Guid.NewGuid();
        var changed = GamePresenceTracker.UpsertLease(leasesByPlayerId, playerId, Guid.NewGuid(), DateTime.UtcNow.AddSeconds(10), DateTime.UtcNow);

        Assert.True(changed);
        Assert.Equal([playerId], GamePresenceTracker.CreateSnapshot(leasesByPlayerId).OnlinePlayerIds);
    }

    [Fact]
    public void UpsertLease_does_not_change_online_set_when_same_player_adds_second_connection()
    {
        var leasesByPlayerId = new Dictionary<Guid, Dictionary<Guid, DateTime>>();
        var playerId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        GamePresenceTracker.UpsertLease(leasesByPlayerId, playerId, Guid.NewGuid(), nowUtc.AddSeconds(10), nowUtc);
        var changed = GamePresenceTracker.UpsertLease(leasesByPlayerId, playerId, Guid.NewGuid(), nowUtc.AddSeconds(10), nowUtc);

        Assert.False(changed);
    }

    [Fact]
    public void UpsertLease_marks_player_online_when_replacing_only_expired_connections()
    {
        var leasesByPlayerId = new Dictionary<Guid, Dictionary<Guid, DateTime>>();
        var playerId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        GamePresenceTracker.UpsertLease(leasesByPlayerId, playerId, Guid.NewGuid(), nowUtc.AddSeconds(-1), nowUtc);

        var changed = GamePresenceTracker.UpsertLease(leasesByPlayerId, playerId, Guid.NewGuid(), nowUtc.AddSeconds(10), nowUtc);

        Assert.True(changed);
        Assert.Equal([playerId], GamePresenceTracker.CreateSnapshot(leasesByPlayerId).OnlinePlayerIds);
    }

    [Fact]
    public void RemoveLease_keeps_player_online_until_last_connection_is_removed()
    {
        var leasesByPlayerId = new Dictionary<Guid, Dictionary<Guid, DateTime>>();
        var playerId = Guid.NewGuid();
        var firstConnectionId = Guid.NewGuid();
        var secondConnectionId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        GamePresenceTracker.UpsertLease(leasesByPlayerId, playerId, firstConnectionId, nowUtc.AddSeconds(10), nowUtc);
        GamePresenceTracker.UpsertLease(leasesByPlayerId, playerId, secondConnectionId, nowUtc.AddSeconds(10), nowUtc);

        Assert.False(GamePresenceTracker.RemoveLease(leasesByPlayerId, playerId, firstConnectionId, nowUtc));
        Assert.True(GamePresenceTracker.RemoveLease(leasesByPlayerId, playerId, secondConnectionId, nowUtc));
    }

    [Fact]
    public void PruneExpiredLeases_marks_player_offline_when_last_connection_expires()
    {
        var leasesByPlayerId = new Dictionary<Guid, Dictionary<Guid, DateTime>>();
        var playerId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        GamePresenceTracker.UpsertLease(leasesByPlayerId, playerId, Guid.NewGuid(), nowUtc.AddSeconds(1), nowUtc);
        var changed = GamePresenceTracker.PruneExpiredLeases(leasesByPlayerId, nowUtc.AddSeconds(2));

        Assert.True(changed);
        Assert.Empty(GamePresenceTracker.CreateSnapshot(leasesByPlayerId).OnlinePlayerIds);
    }
}
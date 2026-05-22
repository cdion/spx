using System.Collections.Immutable;
using Orleans;
using Spx.Contracts;

namespace Spx.Grains;

public sealed class GamePresenceGrain : Grain, IGamePresenceGrain
{
    private readonly HashSet<Guid> onlinePlayers = [];

    public Task SetOnlineAsync(Guid playerId)
    {
        onlinePlayers.Add(playerId);
        return Task.CompletedTask;
    }

    public Task SetOfflineAsync(Guid playerId)
    {
        onlinePlayers.Remove(playerId);
        return Task.CompletedTask;
    }

    public Task<GamePresenceSnapshot> GetSnapshotAsync() =>
        Task.FromResult(
            new GamePresenceSnapshot(onlinePlayers.OrderBy(id => id).ToImmutableArray())
        );
}

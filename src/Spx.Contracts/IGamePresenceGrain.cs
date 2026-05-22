using System.Collections.Immutable;
using Orleans;

namespace Spx.Contracts;

public interface IGamePresenceGrain : IGrainWithGuidKey
{
    Task SetOnlineAsync(Guid playerId);

    Task SetOfflineAsync(Guid playerId);

    Task<GamePresenceSnapshot> GetSnapshotAsync();
}

[GenerateSerializer]
public sealed record GamePresenceSnapshot([property: Id(0)] ImmutableArray<Guid> OnlinePlayerIds);

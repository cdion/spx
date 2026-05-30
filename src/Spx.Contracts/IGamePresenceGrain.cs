using System.Collections.Immutable;
using Orleans;

namespace Spx.Contracts;

public interface IGamePresenceGrain : IGrainWithGuidKey
{
    Task RenewLeaseAsync(Guid playerId, Guid leaseId, TimeSpan ttl);

    Task RevokeLeaseAsync(Guid leaseId);

    Task<GamePresenceSnapshot> GetSnapshotAsync();
}

[GenerateSerializer]
public sealed record GamePresenceSnapshot([property: Id(0)] ImmutableArray<Guid> OnlinePlayerIds);

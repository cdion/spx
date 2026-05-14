using Orleans;

namespace Spx.Contracts;

public interface IGamePresenceGrain : IGrainWithGuidKey
{
    Task UpsertLeaseAsync(UpsertGamePresenceLeaseCommand command);

    Task RemoveLeaseAsync(RemoveGamePresenceLeaseCommand command);

    Task<GamePresenceSnapshot> GetSnapshotAsync();
}

[GenerateSerializer]
public sealed record UpsertGamePresenceLeaseCommand(
    [property: Id(0)] Guid PlayerId,
    [property: Id(1)] Guid ConnectionId,
    [property: Id(2)] DateTime ExpiresAtUtc);

[GenerateSerializer]
public sealed record RemoveGamePresenceLeaseCommand(
    [property: Id(0)] Guid PlayerId,
    [property: Id(1)] Guid ConnectionId);

[GenerateSerializer]
public sealed record GamePresenceSnapshot(
    [property: Id(0)] IReadOnlyList<Guid> OnlinePlayerIds);
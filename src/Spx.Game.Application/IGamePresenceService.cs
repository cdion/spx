namespace Spx.Game.Application;

public interface IGamePresenceService
{
    Task<GamePresenceView> GetPresenceAsync(Guid gameId, CancellationToken cancellationToken = default);

    Task UpsertPresenceLeaseAsync(Guid gameId, Guid playerId, Guid connectionId, DateTime expiresAtUtc, CancellationToken cancellationToken = default);

    Task RemovePresenceLeaseAsync(Guid gameId, Guid playerId, Guid connectionId, CancellationToken cancellationToken = default);
}
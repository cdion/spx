namespace Spx.Game.Application;

public interface IGamePresenceService
{
    Task<GamePresenceView> GetPresenceAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    );
}

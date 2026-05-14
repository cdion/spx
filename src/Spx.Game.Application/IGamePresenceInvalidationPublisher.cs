namespace Spx.Game.Application;

public interface IGamePresenceInvalidationPublisher
{
    Task PublishPresenceInvalidatedAsync(Guid gameId, CancellationToken cancellationToken = default);
}
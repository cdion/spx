namespace Spx.Game.Application;

public interface IGameSessionInvalidationPublisher
{
    Task PublishSessionInvalidatedAsync(Guid gameId, CancellationToken cancellationToken = default);
}
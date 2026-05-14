namespace Spx.Game.Application;

public interface IGameMessageInvalidationPublisher
{
    Task PublishMessagesInvalidatedAsync(Guid gameId, CancellationToken cancellationToken = default);
}
namespace Spx.Game.Application;

public interface IGameMessageEventsPublisher
{
    Task PublishMessagesChangedAsync(Guid gameId, CancellationToken cancellationToken = default);
}
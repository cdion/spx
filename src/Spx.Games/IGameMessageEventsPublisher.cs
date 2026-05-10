namespace Spx.Games;

public interface IGameMessageEventsPublisher
{
    Task PublishMessagesChangedAsync(Guid gameId, CancellationToken cancellationToken = default);
}
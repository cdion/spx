namespace Spx.Games;

public interface IGameLobbyEventsPublisher
{
    Task PublishLobbyChangedAsync(Guid gameId, CancellationToken cancellationToken = default);
}
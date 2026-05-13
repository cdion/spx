namespace Spx.Game.Application;

public interface IGameLobbyEventsPublisher
{
    Task PublishLobbyChangedAsync(Guid gameId, CancellationToken cancellationToken = default);
}
namespace Spx.Game.Application;

public interface IGameLobbyInvalidationPublisher
{
    Task PublishLobbyInvalidatedAsync(Guid gameId, CancellationToken cancellationToken = default);
}
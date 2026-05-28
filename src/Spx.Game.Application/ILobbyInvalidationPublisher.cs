namespace Spx.Game.Application;

public interface ILobbyInvalidationPublisher
{
    Task PublishLobbyInvalidatedAsync(Guid gameId, CancellationToken cancellationToken = default);
}

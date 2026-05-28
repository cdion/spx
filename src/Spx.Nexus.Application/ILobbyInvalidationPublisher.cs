namespace Spx.Nexus.Application;

public interface ILobbyInvalidationPublisher
{
    Task PublishLobbyInvalidatedAsync(Guid gameId, CancellationToken cancellationToken = default);
}

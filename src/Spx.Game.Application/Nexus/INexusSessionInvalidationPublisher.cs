namespace Spx.Game.Application.Nexus;

public interface INexusSessionInvalidationPublisher
{
    Task PublishSessionInvalidatedAsync(Guid gameId, CancellationToken cancellationToken = default);
}

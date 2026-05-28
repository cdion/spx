namespace Spx.Nexus.Application;

public interface INexusSessionInvalidationPublisher
{
    Task PublishSessionInvalidatedAsync(Guid gameId, CancellationToken cancellationToken = default);
}

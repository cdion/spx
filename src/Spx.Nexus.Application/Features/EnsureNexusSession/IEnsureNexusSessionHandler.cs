namespace Spx.Nexus.Application.Features.EnsureNexusSession;

public interface IEnsureNexusSessionHandler
{
    Task<bool> HandleAsync(Guid gameId, CancellationToken cancellationToken = default);
}

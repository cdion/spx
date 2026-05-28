namespace Spx.Game.Application.Nexus.Features.EnsureNexusSession;

public interface IEnsureNexusSessionHandler
{
    Task<bool> HandleAsync(Guid gameId, CancellationToken cancellationToken = default);
}

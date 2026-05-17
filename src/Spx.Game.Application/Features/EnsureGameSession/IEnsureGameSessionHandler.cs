namespace Spx.Game.Application.Features.EnsureGameSession;

public interface IEnsureGameSessionHandler
{
    Task<bool> HandleAsync(Guid gameId, CancellationToken cancellationToken = default);
}
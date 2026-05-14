using Spx.Contracts;

namespace Spx.Game.Application.Features.GetGameSession;

public interface IGetGameSessionHandler
{
    Task<GameSessionView?> HandleAsync(Guid gameId, string userId, CancellationToken cancellationToken = default);
}
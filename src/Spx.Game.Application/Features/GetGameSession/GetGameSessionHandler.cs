using Spx.Contracts;

namespace Spx.Game.Application.Features.GetGameSession;

internal sealed class GetGameSessionHandler(IGameSessionService gameSessionService) : IGetGameSessionHandler
{
    public async Task<GameSessionView?> HandleAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
        => await gameSessionService.GetSessionViewAsync(gameId, userId, cancellationToken);
}
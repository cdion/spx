namespace Spx.Game.Application.Features.GetGameSession;

internal sealed class GetGameSessionHandler(IGameSessionService gameSessionService)
    : IGetGameSessionHandler
{
    public async Task<GameSessionView?> HandleAsync(
        Guid gameId,
        Guid playerId,
        CancellationToken cancellationToken = default
    ) => await gameSessionService.GetSessionAsync(gameId, playerId, cancellationToken);
}

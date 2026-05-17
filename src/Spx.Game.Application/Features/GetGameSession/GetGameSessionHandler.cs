namespace Spx.Game.Application.Features.GetGameSession;

internal sealed class GetGameSessionHandler(IGameSessionService gameSessionService) : IGetGameSessionHandler
{
    public async Task<GameSessionSnapshot?> HandleAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
        => await gameSessionService.GetSessionAsync(gameId, userId, cancellationToken);
}
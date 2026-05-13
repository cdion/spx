namespace Spx.Games.Features.GetGamePage;

internal sealed class GetGamePageHandler(
    IGamePersistence gamePersistence,
    IGameSessionService gameSessionService) : IGetGamePageHandler
{
    public async Task<GamePageView?> HandleAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
    {
        var lobby = await gamePersistence.GetLobbyAsync(gameId, userId, cancellationToken);
        if (lobby is null)
        {
            return null;
        }

        var session = await gameSessionService.GetSessionViewAsync(gameId, userId, cancellationToken);
        return new GamePageView(lobby, session);
    }
}
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

        if (session is null && lobby.IsCurrentUserActive && lobby.Players.Count == lobby.MaxPlayers)
        {
            var activePlayers = await gamePersistence.GetActiveSessionPlayersAsync(gameId, cancellationToken);
            if (activePlayers is { Count: 2 }
                && await gameSessionService.TryInitializeAsync(gameId, activePlayers, cancellationToken))
            {
                session = await gameSessionService.GetSessionViewAsync(gameId, userId, cancellationToken);
            }
        }

        return new GamePageView(lobby, session);
    }
}
namespace Spx.Game.Application.Features.GetGamePage;

internal sealed class GetGamePageHandler(
    IGamePersistence gamePersistence,
    IGameSessionService gameSessionService,
    IGamePresenceService gamePresenceService) : IGetGamePageHandler
{
    public async Task<GamePageView?> HandleAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
    {
        var lobby = await gamePersistence.GetLobbyAsync(gameId, userId, cancellationToken);
        if (lobby is null)
        {
            return null;
        }

        var session = await gameSessionService.GetSessionAsync(gameId, lobby.CurrentPlayerId, cancellationToken);

        var presence = await gamePresenceService.GetPresenceAsync(gameId, cancellationToken);

        return new GamePageView(lobby, session, presence);
    }
}
namespace Spx.Nexus.Application.Features.GetNexusPage;

internal sealed class GetNexusPageHandler(
    IGamePersistence gamePersistence,
    INexusSessionService gameSessionService,
    IGamePresenceService gamePresenceService
) : IGetNexusPageHandler
{
    public async Task<GamePageView?> HandleAsync(
        Guid gameId,
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        var lobby = await gamePersistence.GetLobbyAsync(gameId, userId, cancellationToken);
        if (lobby is null)
        {
            return null;
        }

        var sessionOutcome = await gameSessionService.GetSessionAsync(
            gameId,
            lobby.CurrentPlayerId,
            cancellationToken
        );

        var session = sessionOutcome is GameSessionFound found ? found.Session : null;
        var presence = await gamePresenceService.GetPresenceAsync(gameId, cancellationToken);

        return new GamePageView(lobby, session, presence);
    }
}

namespace Spx.Game.Application.Features.EnsureGameSession;

internal sealed class EnsureGameSessionHandler(
    IGamePersistence gamePersistence,
    IGameSessionService gameSessionService
) : IEnsureGameSessionHandler
{
    public async Task<bool> HandleAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var activePlayers = await gamePersistence.GetActiveSessionPlayersAsync(
            gameId,
            cancellationToken
        );
        if (activePlayers is not { Count: 2 })
        {
            return false;
        }

        return await gameSessionService.EnsureSessionAsync(
            gameId,
            activePlayers,
            cancellationToken
        );
    }
}

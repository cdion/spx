namespace Spx.Nexus.Application.Features.EnsureNexusSession;

internal sealed class EnsureNexusSessionHandler(
    IGamePersistence gamePersistence,
    INexusSessionService gameSessionService
) : IEnsureNexusSessionHandler
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

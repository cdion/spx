namespace Spx.Game.Application.Nexus.Features.EnsureNexusSession;

internal sealed class EnsureNexusSessionHandler(
    INexusSessionRosterProvider sessionRosterProvider,
    INexusSessionService gameSessionService
) : IEnsureNexusSessionHandler
{
    public async Task<bool> HandleAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var activePlayers = await sessionRosterProvider.GetActiveSessionPlayersAsync(
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

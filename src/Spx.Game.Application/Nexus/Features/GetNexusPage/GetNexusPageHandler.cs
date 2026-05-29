using Microsoft.Extensions.Logging;
using Spx.Nexus.Domain;

namespace Spx.Game.Application.Nexus.Features.GetNexusPage;

internal sealed partial class GetNexusPageHandler(
    IGamePersistence gamePersistence,
    INexusSessionRosterProvider sessionRosterProvider,
    INexusSessionService gameSessionService,
    IGamePresenceService gamePresenceService,
    ILogger<GetNexusPageHandler> logger
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

        var sessionOutcome = await GetSessionOutcomeAsync(lobby, gameId, cancellationToken);

        var session = sessionOutcome is GameSessionFound found ? found.Session : null;
        var presence = await gamePresenceService.GetPresenceAsync(gameId, cancellationToken);

        return new GamePageView(lobby, session, presence);
    }

    private async Task<GameSessionOutcome> GetSessionOutcomeAsync(
        GameLobbyView lobby,
        Guid gameId,
        CancellationToken cancellationToken
    )
    {
        var sessionOutcome = await gameSessionService.GetSessionAsync(
            gameId,
            lobby.CurrentPlayerId,
            cancellationToken
        );

        if (!lobby.IsCurrentUserActive)
        {
            return sessionOutcome;
        }

        var activePlayers = await sessionRosterProvider.GetActiveSessionPlayersAsync(
            gameId,
            cancellationToken
        );

        if (sessionOutcome is GameSessionUnavailable)
        {
            return await TryRepairMissingSessionAsync(
                gameId,
                lobby.CurrentPlayerId,
                activePlayers,
                cancellationToken
            );
        }

        if (sessionOutcome is GameSessionFound found)
        {
            return await TryReconcileStaleSessionAsync(
                gameId,
                lobby,
                found.Session,
                activePlayers,
                cancellationToken
            );
        }

        return sessionOutcome;
    }

    private async Task<GameSessionOutcome> TryRepairMissingSessionAsync(
        Guid gameId,
        Guid currentPlayerId,
        IReadOnlyList<Guid>? activePlayers,
        CancellationToken cancellationToken
    )
    {
        if (activePlayers is not { Count: 2 })
        {
            return new GameSessionUnavailable();
        }

        var ensured = await gameSessionService.EnsureSessionAsync(
            gameId,
            activePlayers,
            cancellationToken
        );

        if (!ensured)
        {
            LogMissingSessionRepairFailed(logger, gameId, currentPlayerId);
        }

        return ensured
            ? await gameSessionService.GetSessionAsync(gameId, currentPlayerId, cancellationToken)
            : new GameSessionUnavailable();
    }

    private async Task<GameSessionOutcome> TryReconcileStaleSessionAsync(
        Guid gameId,
        GameLobbyView lobby,
        NexusGameView session,
        IReadOnlyList<Guid>? activePlayers,
        CancellationToken cancellationToken
    )
    {
        if (activePlayers is not { Count: 1 } || activePlayers[0] != lobby.CurrentPlayerId)
        {
            return new GameSessionFound(session);
        }

        if (!session.Opponent.IsActive)
        {
            return new GameSessionFound(session);
        }

        await gameSessionService.AbandonAsync(gameId, session.Opponent.PlayerId, cancellationToken);

        return await gameSessionService.GetSessionAsync(
            gameId,
            lobby.CurrentPlayerId,
            cancellationToken
        );
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to repair missing Nexus session during page load for game {GameId} player {PlayerId}."
    )]
    private static partial void LogMissingSessionRepairFailed(
        ILogger logger,
        Guid gameId,
        Guid playerId
    );
}

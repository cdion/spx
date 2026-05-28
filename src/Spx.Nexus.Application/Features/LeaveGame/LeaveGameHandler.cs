using Microsoft.Extensions.Logging;

namespace Spx.Nexus.Application.Features.LeaveGame;

internal sealed partial class LeaveGameHandler(
    IGamePersistence gamePersistence,
    INexusSessionService gameSessionService,
    ILobbyInvalidationPublisher gameLobbyInvalidationPublisher,
    IGameMessageInvalidationPublisher gameMessageInvalidationPublisher,
    ILogger<LeaveGameHandler> logger
) : ILeaveGameHandler
{
    public async Task<GameCommandOutcome> HandleAsync(
        Guid gameId,
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        // Execute SQL leave first to ensure persistent state changes atomically
        var leaveResult = await gamePersistence.LeaveGameAsync(gameId, userId, cancellationToken);

        // Only abandon in Orleans after SQL commit succeeds
        if (leaveResult.PlayerId is Guid playerId)
        {
            try
            {
                await gameSessionService.AbandonAsync(gameId, playerId, cancellationToken);
            }
            catch (Exception exception)
            {
                LogAbandonSessionFailed(logger, exception, gameId, userId);
            }

            await gameLobbyInvalidationPublisher.PublishLobbyInvalidatedAsync(
                gameId,
                cancellationToken
            );
            await gameMessageInvalidationPublisher.PublishMessagesInvalidatedAsync(
                gameId,
                cancellationToken
            );
        }

        return leaveResult.Result;
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to abandon Orleans session after leave for game {GameId} user {UserId}."
    )]
    private static partial void LogAbandonSessionFailed(
        ILogger logger,
        Exception exception,
        Guid gameId,
        string userId
    );
}

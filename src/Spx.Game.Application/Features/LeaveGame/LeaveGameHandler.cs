using Microsoft.Extensions.Logging;

namespace Spx.Game.Application.Features.LeaveGame;

internal sealed class LeaveGameHandler(
    IGamePersistence gamePersistence,
    IGameSessionService gameSessionService,
    IGameLobbyInvalidationPublisher gameLobbyInvalidationPublisher,
    IGameMessageInvalidationPublisher gameMessageInvalidationPublisher,
    ILogger<LeaveGameHandler> logger)
    : ILeaveGameHandler
{
    public async Task<GameCommandOutcome> HandleAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
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
                logger.LogWarning(exception, "Failed to abandon Orleans session after leave for game {GameId} user {UserId}.", gameId, userId);
            }

            await gameLobbyInvalidationPublisher.PublishLobbyInvalidatedAsync(gameId, cancellationToken);
            await gameMessageInvalidationPublisher.PublishMessagesInvalidatedAsync(gameId, cancellationToken);
        }

        return leaveResult.Result;
    }
}
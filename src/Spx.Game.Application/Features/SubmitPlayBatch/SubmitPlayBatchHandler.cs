using Microsoft.Extensions.Logging;
using Spx.Contracts;

namespace Spx.Game.Application.Features.SubmitPlayBatch;

internal sealed class SubmitPlayBatchHandler(
    IGameSessionService gameSessionService,
    IGameplayEventMessageWriter gameplayEventMessageWriter,
    IGameSessionInvalidationPublisher gameSessionInvalidationPublisher,
    IGameMessageInvalidationPublisher gameMessageInvalidationPublisher,
    ILogger<SubmitPlayBatchHandler> logger) : ISubmitPlayBatchHandler
{
    public async Task<GameSessionCommandOutcome> HandleAsync(
        Guid gameId,
        string userId,
        int expectedRoundNumber,
        IReadOnlyList<GameBatchCardCommand> cards,
        CancellationToken cancellationToken = default)
    {
        var result = await gameSessionService.SubmitPlayBatchAsync(
            gameId,
            new SubmitPlayBatchCommand(userId, expectedRoundNumber, cards),
            cancellationToken);

        if (result is GameSessionCommandSucceeded succeeded)
        {
            var persistedGameplayMessageCount = 0;

            try
            {
                persistedGameplayMessageCount = await gameplayEventMessageWriter.PersistResolvedBatchAsync(succeeded.Session, succeeded.GameplayEvents, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to persist gameplay event messages after play batch submission for game {GameId} user {UserId}.", gameId, userId);
            }

            try
            {
                await gameSessionInvalidationPublisher.PublishSessionInvalidatedAsync(gameId, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to publish session invalidation after play batch submission for game {GameId} user {UserId}.", gameId, userId);
            }

            if (persistedGameplayMessageCount > 0)
            {
                try
                {
                    await gameMessageInvalidationPublisher.PublishMessagesInvalidatedAsync(gameId, cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Failed to publish message invalidation after play batch submission for game {GameId} user {UserId}.", gameId, userId);
                }
            }
        }

        return result;
    }
}
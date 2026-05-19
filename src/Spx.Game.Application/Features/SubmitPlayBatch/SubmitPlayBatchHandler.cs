using Microsoft.Extensions.Logging;

namespace Spx.Game.Application.Features.SubmitPlayBatch;

internal sealed partial class SubmitPlayBatchHandler(
    IGameSessionService gameSessionService,
    IGameplayEventMessageWriter gameplayEventMessageWriter,
    IGameSessionInvalidationPublisher gameSessionInvalidationPublisher,
    IGameMessageInvalidationPublisher gameMessageInvalidationPublisher,
    ILogger<SubmitPlayBatchHandler> logger
) : ISubmitPlayBatchHandler
{
    public async Task<GameSessionCommandOutcome> HandleAsync(
        Guid gameId,
        Guid playerId,
        int expectedRoundNumber,
        IReadOnlyList<GameBatchCardCommand> cards,
        CancellationToken cancellationToken = default
    )
    {
        var result = await gameSessionService.SubmitPlayBatchAsync(
            gameId,
            new SubmitPlayBatchCommand(playerId, expectedRoundNumber, cards),
            cancellationToken
        );

        if (result is GameSessionCommandSucceeded succeeded)
        {
            var persistedGameplayMessageCount = 0;
            var persistedGameplayEvents = false;

            try
            {
                persistedGameplayMessageCount =
                    await gameplayEventMessageWriter.PersistResolvedBatchAsync(
                        gameId,
                        succeeded.Session.LastResolvedBatch,
                        succeeded.Session.Completion,
                        succeeded.GameplayEvents,
                        cancellationToken
                    );
                persistedGameplayEvents = true;
            }
            catch (Exception exception)
            {
                LogPersistGameplayEventsFailed(logger, exception, gameId, playerId);
            }

            if (
                persistedGameplayEvents
                && succeeded.PendingGameplayEventBatchId is Guid pendingGameplayEventBatchId
            )
            {
                try
                {
                    await gameSessionService.AcknowledgeGameplayEventBatchAsync(
                        gameId,
                        pendingGameplayEventBatchId,
                        cancellationToken
                    );
                }
                catch (Exception exception)
                {
                    LogAcknowledgeGameplayEventBatchFailed(
                        logger,
                        exception,
                        pendingGameplayEventBatchId,
                        gameId,
                        playerId
                    );
                }
            }

            try
            {
                await gameSessionInvalidationPublisher.PublishSessionInvalidatedAsync(
                    gameId,
                    cancellationToken
                );
            }
            catch (Exception exception)
            {
                LogPublishSessionInvalidationFailed(logger, exception, gameId, playerId);
            }

            if (persistedGameplayMessageCount > 0)
            {
                try
                {
                    await gameMessageInvalidationPublisher.PublishMessagesInvalidatedAsync(
                        gameId,
                        cancellationToken
                    );
                }
                catch (Exception exception)
                {
                    LogPublishMessageInvalidationFailed(logger, exception, gameId, playerId);
                }
            }
        }

        return result;
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to persist gameplay event messages after play batch submission for game {GameId} player {PlayerId}."
    )]
    private static partial void LogPersistGameplayEventsFailed(
        ILogger logger,
        Exception exception,
        Guid gameId,
        Guid playerId
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to acknowledge persisted gameplay event batch {BatchId} after play batch submission for game {GameId} player {PlayerId}."
    )]
    private static partial void LogAcknowledgeGameplayEventBatchFailed(
        ILogger logger,
        Exception exception,
        Guid batchId,
        Guid gameId,
        Guid playerId
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to publish session invalidation after play batch submission for game {GameId} player {PlayerId}."
    )]
    private static partial void LogPublishSessionInvalidationFailed(
        ILogger logger,
        Exception exception,
        Guid gameId,
        Guid playerId
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to publish message invalidation after play batch submission for game {GameId} player {PlayerId}."
    )]
    private static partial void LogPublishMessageInvalidationFailed(
        ILogger logger,
        Exception exception,
        Guid gameId,
        Guid playerId
    );
}

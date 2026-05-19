using Microsoft.Extensions.Logging;

namespace Spx.Game.Application.Features.SubmitAcquireCard;

internal sealed partial class SubmitAcquireCardHandler(
    IGameSessionService gameSessionService,
    IGameSessionInvalidationPublisher gameSessionInvalidationPublisher,
    ILogger<SubmitAcquireCardHandler> logger
) : ISubmitAcquireCardHandler
{
    public async Task<GameSessionCommandOutcome> HandleAsync(
        Guid gameId,
        Guid playerId,
        int expectedRoundNumber,
        Guid marketCardInstanceId,
        CancellationToken cancellationToken = default
    )
    {
        var result = await gameSessionService.SubmitAcquireAsync(
            gameId,
            new SubmitAcquireRequest(playerId, expectedRoundNumber, marketCardInstanceId),
            cancellationToken
        );

        if (result is GameSessionCommandSucceeded)
        {
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
        }

        return result;
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to publish session invalidation after acquire submission for game {GameId} user {PlayerId}."
    )]
    private static partial void LogPublishSessionInvalidationFailed(
        ILogger logger,
        Exception exception,
        Guid gameId,
        Guid playerId
    );
}

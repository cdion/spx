using Microsoft.Extensions.Logging;

namespace Spx.Game.Application.Features.SubmitAcquireCard;

internal sealed class SubmitAcquireCardHandler(
    IGameSessionService gameSessionService,
    IGameSessionInvalidationPublisher gameSessionInvalidationPublisher,
    ILogger<SubmitAcquireCardHandler> logger) : ISubmitAcquireCardHandler
{
    public async Task<GameSessionCommandOutcome> HandleAsync(
        Guid gameId,
        string userId,
        int expectedRoundNumber,
        Guid marketCardInstanceId,
        CancellationToken cancellationToken = default)
    {
        var result = await gameSessionService.SubmitAcquireAsync(
            gameId,
            new SubmitAcquireRequest(userId, expectedRoundNumber, marketCardInstanceId),
            cancellationToken);

        if (result is GameSessionCommandSucceeded)
        {
            try
            {
                await gameSessionInvalidationPublisher.PublishSessionInvalidatedAsync(gameId, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to publish session invalidation after acquire submission for game {GameId} user {UserId}.", gameId, userId);
            }
        }

        return result;
    }
}

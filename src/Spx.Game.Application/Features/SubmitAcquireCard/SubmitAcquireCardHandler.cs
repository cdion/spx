using Microsoft.Extensions.Logging;

namespace Spx.Game.Application.Features.SubmitAcquireCard;

internal sealed class SubmitAcquireCardHandler(
    IGameSessionService gameSessionService,
    IGameSessionInvalidationPublisher gameSessionInvalidationPublisher,
    ILogger<SubmitAcquireCardHandler> logger) : ISubmitAcquireCardHandler
{
    public async Task<GameSessionCommandOutcome> HandleAsync(
        Guid gameId,
        Guid playerId,
        int expectedRoundNumber,
        Guid marketCardInstanceId,
        CancellationToken cancellationToken = default)
    {
        var result = await gameSessionService.SubmitAcquireAsync(
            gameId,
            new SubmitAcquireRequest(playerId, expectedRoundNumber, marketCardInstanceId),
            cancellationToken);

        if (result is GameSessionCommandSucceeded)
        {
            try
            {
                await gameSessionInvalidationPublisher.PublishSessionInvalidatedAsync(gameId, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to publish session invalidation after acquire submission for game {GameId} user {PlayerId}.", gameId, playerId);
            }
        }

        return result;
    }
}

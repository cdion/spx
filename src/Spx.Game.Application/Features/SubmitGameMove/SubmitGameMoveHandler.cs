using Spx.Contracts;

namespace Spx.Game.Application.Features.SubmitGameMove;

internal sealed class SubmitGameMoveHandler(
    IGameSessionService gameSessionService,
    IGameLobbyEventsPublisher gameLobbyEventsPublisher) : ISubmitGameMoveHandler
{
    public async Task<SubmitGameMoveOutcome> HandleAsync(
        Guid gameId,
        string userId,
        int expectedRoundNumber,
        GameMove move,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await gameSessionService.SubmitMoveAsync(
                gameId,
                new SubmitGameMoveCommand(userId, expectedRoundNumber, move),
                cancellationToken);

            // Publish state change so opponent client sees progression promptly
            try
            {
                await gameLobbyEventsPublisher.PublishLobbyChangedAsync(gameId, cancellationToken);
            }
            catch
            {
                // Log but don't fail—publish is best-effort for UI updates
            }

            return new SubmitGameMoveSucceeded(session);
        }
        catch (InvalidOperationException exception)
        {
            return new SubmitGameMoveFailed(exception.Message);
        }
    }
}
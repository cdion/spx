namespace Spx.Games.Features.LeaveGame;

internal sealed class LeaveGameHandler(
    IGamePersistence gamePersistence,
    IGameLobbyEventsPublisher gameLobbyEventsPublisher,
    IGameMessageEventsPublisher gameMessageEventsPublisher)
    : ILeaveGameHandler
{
    public async Task<GameCommandResult> HandleAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
    {
        var leaveResult = await gamePersistence.LeaveGameAsync(gameId, userId, cancellationToken);

        if (leaveResult.Changed)
        {
            await gameLobbyEventsPublisher.PublishLobbyChangedAsync(gameId, cancellationToken);
            await gameMessageEventsPublisher.PublishMessagesChangedAsync(gameId, cancellationToken);
        }

        return leaveResult.Result;
    }
}
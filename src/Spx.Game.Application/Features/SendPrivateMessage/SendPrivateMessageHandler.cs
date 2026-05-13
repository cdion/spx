namespace Spx.Game.Application.Features.SendPrivateMessage;

internal sealed class SendPrivateMessageHandler(
    IGameMessageEventsPublisher gameMessageEventsPublisher,
    IGameMessagePersistence gameMessagePersistence) : ISendPrivateMessageHandler
{
    public async Task<GameMessageCommandOutcome> HandleAsync(Guid gameId, string userId, Guid recipientPlayerId, SendGameMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (!GameMessageSupport.TryNormalizeMessageBody(request.Body, out var body, out var errorMessage))
        {
            return new GameMessageCommandFailed(errorMessage);
        }

        var result = await gameMessagePersistence.SendPrivateMessageAsync(gameId, userId, recipientPlayerId, body, cancellationToken);
        if (result is GameMessageCommandSucceeded)
        {
            await gameMessageEventsPublisher.PublishMessagesChangedAsync(gameId, cancellationToken);
        }

        return result;
    }
}
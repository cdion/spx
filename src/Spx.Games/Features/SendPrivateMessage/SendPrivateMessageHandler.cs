namespace Spx.Games.Features.SendPrivateMessage;

internal sealed class SendPrivateMessageHandler(
    IGameMessageEventsPublisher gameMessageEventsPublisher,
    IGameMessagePersistence gameMessagePersistence) : ISendPrivateMessageHandler
{
    public async Task<GameMessageCommandResult> HandleAsync(Guid gameId, string userId, Guid recipientPlayerId, SendGameMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (!GameMessageSupport.TryNormalizeMessageBody(request.Body, out var body, out var errorMessage))
        {
            return GameMessageCommandResult.Failure(errorMessage);
        }

        var result = await gameMessagePersistence.SendPrivateMessageAsync(gameId, userId, recipientPlayerId, body, cancellationToken);
        if (result.Succeeded)
        {
            await gameMessageEventsPublisher.PublishMessagesChangedAsync(gameId, cancellationToken);
        }

        return result;
    }
}
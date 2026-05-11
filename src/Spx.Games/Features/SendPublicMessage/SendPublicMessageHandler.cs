namespace Spx.Games.Features.SendPublicMessage;

internal sealed class SendPublicMessageHandler(
    IGameMessageEventsPublisher gameMessageEventsPublisher,
    IGameMessagePersistence gameMessagePersistence) : ISendPublicMessageHandler
{
    public async Task<GameMessageCommandResult> HandleAsync(Guid gameId, string userId, SendGameMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (!GameMessageSupport.TryNormalizeMessageBody(request.Body, out var body, out var errorMessage))
        {
            return GameMessageCommandResult.Failure(errorMessage);
        }

        var result = await gameMessagePersistence.SendPublicMessageAsync(gameId, userId, body, cancellationToken);
        if (result.Succeeded)
        {
            await gameMessageEventsPublisher.PublishMessagesChangedAsync(gameId, cancellationToken);
        }

        return result;
    }
}
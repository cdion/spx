namespace Spx.Games.Features.EditMessage;

internal sealed class EditMessageHandler(
    IGameMessageEventsPublisher gameMessageEventsPublisher,
    IGameMessagePersistence gameMessagePersistence) : IEditMessageHandler
{
    public async Task<GameMessageCommandResult> HandleAsync(Guid gameId, string userId, Guid messageId, UpdateGameMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (!GameMessageSupport.TryNormalizeMessageBody(request.Body, out var body, out var errorMessage))
        {
            return GameMessageCommandResult.Failure(errorMessage);
        }

        var result = await gameMessagePersistence.EditMessageAsync(gameId, userId, messageId, body, cancellationToken);
        if (result.Succeeded)
        {
            await gameMessageEventsPublisher.PublishMessagesChangedAsync(gameId, cancellationToken);
        }

        return result;
    }
}
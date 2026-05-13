namespace Spx.Game.Application.Features.EditMessage;

internal sealed class EditMessageHandler(
    IGameMessageEventsPublisher gameMessageEventsPublisher,
    IGameMessagePersistence gameMessagePersistence) : IEditMessageHandler
{
    public async Task<GameMessageCommandOutcome> HandleAsync(Guid gameId, string userId, Guid messageId, UpdateGameMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (!GameMessageSupport.TryNormalizeMessageBody(request.Body, out var body, out var errorMessage))
        {
            return new GameMessageCommandFailed(errorMessage);
        }

        var result = await gameMessagePersistence.EditMessageAsync(gameId, userId, messageId, body, cancellationToken);
        if (result is GameMessageCommandSucceeded)
        {
            await gameMessageEventsPublisher.PublishMessagesChangedAsync(gameId, cancellationToken);
        }

        return result;
    }
}
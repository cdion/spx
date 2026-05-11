namespace Spx.Games.Features.DeleteMessage;

internal sealed class DeleteMessageHandler(
    IGameMessageEventsPublisher gameMessageEventsPublisher,
    IGameMessagePersistence gameMessagePersistence) : IDeleteMessageHandler
{
    public async Task<GameMessageCommandResult> HandleAsync(Guid gameId, string userId, Guid messageId, CancellationToken cancellationToken = default)
    {
        var result = await gameMessagePersistence.DeleteMessageAsync(gameId, userId, messageId, cancellationToken);
        if (result.Succeeded)
        {
            await gameMessageEventsPublisher.PublishMessagesChangedAsync(gameId, cancellationToken);
        }

        return result;
    }
}
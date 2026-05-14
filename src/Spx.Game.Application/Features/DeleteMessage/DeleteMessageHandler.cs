namespace Spx.Game.Application.Features.DeleteMessage;

internal sealed class DeleteMessageHandler(
    IGameMessageInvalidationPublisher gameMessageInvalidationPublisher,
    IGameMessagePersistence gameMessagePersistence) : IDeleteMessageHandler
{
    public async Task<GameMessageCommandOutcome> HandleAsync(Guid gameId, string userId, Guid messageId, CancellationToken cancellationToken = default)
    {
        var result = await gameMessagePersistence.DeleteMessageAsync(gameId, userId, messageId, cancellationToken);
        if (result is GameMessageCommandSucceeded)
        {
            await gameMessageInvalidationPublisher.PublishMessagesInvalidatedAsync(gameId, cancellationToken);
        }

        return result;
    }
}
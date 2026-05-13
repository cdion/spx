namespace Spx.Game.Application.Features.SendPublicMessage;

internal sealed class SendPublicMessageHandler(
    IGameMessageEventsPublisher gameMessageEventsPublisher,
    IGameMessagePersistence gameMessagePersistence) : ISendPublicMessageHandler
{
    public async Task<GameMessageCommandOutcome> HandleAsync(Guid gameId, string userId, SendGameMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (!GameMessageSupport.TryNormalizeMessageBody(request.Body, out var body, out var errorMessage))
        {
            return new GameMessageCommandFailed(errorMessage);
        }

        var result = await gameMessagePersistence.SendPublicMessageAsync(gameId, userId, body, cancellationToken);
        if (result is GameMessageCommandSucceeded)
        {
            await gameMessageEventsPublisher.PublishMessagesChangedAsync(gameId, cancellationToken);
        }

        return result;
    }
}
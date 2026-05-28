namespace Spx.Nexus.Application.Features.SendPublicMessage;

internal sealed class SendPublicMessageHandler(
    IGameMessageInvalidationPublisher gameMessageInvalidationPublisher,
    IGameMessagePersistence gameMessagePersistence
) : ISendPublicMessageHandler
{
    public async Task<GameMessageCommandOutcome> HandleAsync(
        Guid gameId,
        Guid playerId,
        SendGameMessageRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (
            !GameMessageSupport.TryNormalizeMessageBody(
                request.Body,
                out var body,
                out var errorMessage
            )
        )
        {
            return new GameMessageCommandFailed(errorMessage);
        }

        var result = await gameMessagePersistence.SendPublicMessageAsync(
            gameId,
            playerId,
            body,
            cancellationToken
        );
        if (result is GameMessageCommandSucceeded)
        {
            await gameMessageInvalidationPublisher.PublishMessagesInvalidatedAsync(
                gameId,
                cancellationToken
            );
        }

        return result;
    }
}

namespace Spx.Nexus.Application.Features.EditMessage;

internal sealed class EditMessageHandler(
    IGameMessageInvalidationPublisher gameMessageInvalidationPublisher,
    IGameMessagePersistence gameMessagePersistence
) : IEditMessageHandler
{
    public async Task<GameMessageCommandOutcome> HandleAsync(
        Guid gameId,
        Guid playerId,
        Guid messageId,
        UpdateGameMessageRequest request,
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

        var result = await gameMessagePersistence.EditMessageAsync(
            gameId,
            playerId,
            messageId,
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

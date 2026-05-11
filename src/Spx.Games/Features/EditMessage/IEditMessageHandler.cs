namespace Spx.Games.Features.EditMessage;

public interface IEditMessageHandler
{
    Task<GameMessageCommandResult> HandleAsync(Guid gameId, string userId, Guid messageId, UpdateGameMessageRequest request, CancellationToken cancellationToken = default);
}
namespace Spx.Game.Application.Features.EditMessage;

public interface IEditMessageHandler
{
    Task<GameMessageCommandOutcome> HandleAsync(Guid gameId, string userId, Guid messageId, UpdateGameMessageRequest request, CancellationToken cancellationToken = default);
}
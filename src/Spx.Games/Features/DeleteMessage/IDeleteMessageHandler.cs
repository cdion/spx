namespace Spx.Games.Features.DeleteMessage;

public interface IDeleteMessageHandler
{
    Task<GameMessageCommandResult> HandleAsync(Guid gameId, string userId, Guid messageId, CancellationToken cancellationToken = default);
}
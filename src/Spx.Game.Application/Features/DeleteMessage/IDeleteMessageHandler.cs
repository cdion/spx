namespace Spx.Game.Application.Features.DeleteMessage;

public interface IDeleteMessageHandler
{
    Task<GameMessageCommandOutcome> HandleAsync(Guid gameId, string userId, Guid messageId, CancellationToken cancellationToken = default);
}
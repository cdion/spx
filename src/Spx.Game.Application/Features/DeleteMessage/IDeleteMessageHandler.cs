namespace Spx.Game.Application.Features.DeleteMessage;

public interface IDeleteMessageHandler
{
    Task<GameMessageCommandOutcome> HandleAsync(
        Guid gameId,
        Guid playerId,
        Guid messageId,
        CancellationToken cancellationToken = default
    );
}

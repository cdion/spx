namespace Spx.Nexus.Application.Features.DeleteMessage;

public interface IDeleteMessageHandler
{
    Task<GameMessageCommandOutcome> HandleAsync(
        Guid gameId,
        Guid playerId,
        Guid messageId,
        CancellationToken cancellationToken = default
    );
}

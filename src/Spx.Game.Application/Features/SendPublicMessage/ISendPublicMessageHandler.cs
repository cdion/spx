namespace Spx.Game.Application.Features.SendPublicMessage;

public interface ISendPublicMessageHandler
{
    Task<GameMessageCommandOutcome> HandleAsync(
        Guid gameId,
        Guid playerId,
        SendGameMessageRequest request,
        CancellationToken cancellationToken = default
    );
}

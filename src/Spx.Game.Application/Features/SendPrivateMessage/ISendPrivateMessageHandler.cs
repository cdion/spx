namespace Spx.Game.Application.Features.SendPrivateMessage;

public interface ISendPrivateMessageHandler
{
    Task<GameMessageCommandOutcome> HandleAsync(
        Guid gameId,
        Guid playerId,
        Guid recipientPlayerId,
        SendGameMessageRequest request,
        CancellationToken cancellationToken = default
    );
}

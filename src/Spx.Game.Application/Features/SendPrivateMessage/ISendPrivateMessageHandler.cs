namespace Spx.Game.Application.Features.SendPrivateMessage;

public interface ISendPrivateMessageHandler
{
    Task<GameMessageCommandOutcome> HandleAsync(Guid gameId, string userId, Guid recipientPlayerId, SendGameMessageRequest request, CancellationToken cancellationToken = default);
}
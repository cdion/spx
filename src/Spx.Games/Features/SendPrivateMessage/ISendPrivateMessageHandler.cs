namespace Spx.Games.Features.SendPrivateMessage;

public interface ISendPrivateMessageHandler
{
    Task<GameMessageCommandResult> HandleAsync(Guid gameId, string userId, Guid recipientPlayerId, SendGameMessageRequest request, CancellationToken cancellationToken = default);
}
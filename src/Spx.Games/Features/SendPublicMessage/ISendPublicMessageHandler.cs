namespace Spx.Games.Features.SendPublicMessage;

public interface ISendPublicMessageHandler
{
    Task<GameMessageCommandResult> HandleAsync(Guid gameId, string userId, SendGameMessageRequest request, CancellationToken cancellationToken = default);
}
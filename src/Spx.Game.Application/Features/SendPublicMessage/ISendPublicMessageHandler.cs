namespace Spx.Game.Application.Features.SendPublicMessage;

public interface ISendPublicMessageHandler
{
    Task<GameMessageCommandOutcome> HandleAsync(Guid gameId, string userId, SendGameMessageRequest request, CancellationToken cancellationToken = default);
}
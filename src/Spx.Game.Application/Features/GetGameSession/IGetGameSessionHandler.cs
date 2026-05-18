namespace Spx.Game.Application.Features.GetGameSession;

public interface IGetGameSessionHandler
{
    Task<GameSessionView?> HandleAsync(Guid gameId, Guid playerId, CancellationToken cancellationToken = default);
}
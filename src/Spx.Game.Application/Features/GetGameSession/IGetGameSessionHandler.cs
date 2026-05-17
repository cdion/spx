namespace Spx.Game.Application.Features.GetGameSession;

public interface IGetGameSessionHandler
{
    Task<GameSessionSnapshot?> HandleAsync(Guid gameId, string userId, CancellationToken cancellationToken = default);
}
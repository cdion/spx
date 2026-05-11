namespace Spx.Games.Features.GetLobby;

public interface IGetLobbyHandler
{
    Task<GameLobbyView?> HandleAsync(Guid gameId, string userId, CancellationToken cancellationToken = default);
}
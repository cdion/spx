namespace Spx.Games.Features.GetGameLobby;

public interface IGetGameLobbyHandler
{
    Task<GameLobbyView?> HandleAsync(Guid gameId, string userId, CancellationToken cancellationToken = default);
}
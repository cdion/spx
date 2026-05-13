namespace Spx.Game.Application.Features.GetGameLobby;

public interface IGetGameLobbyHandler
{
    Task<GameLobbyView?> HandleAsync(Guid gameId, string userId, CancellationToken cancellationToken = default);
}
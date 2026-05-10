namespace Spx.Games;

public interface IGameService
{
    Task<GameCommandResult> CreateGameAsync(string userId, CreateGameRequest request, CancellationToken cancellationToken = default);

    Task<GameCommandResult> JoinGameAsync(string userId, JoinGameRequest request, CancellationToken cancellationToken = default);

    Task<GameCommandResult> LeaveGameAsync(Guid gameId, string userId, CancellationToken cancellationToken = default);

    Task<GameLobbyView?> GetLobbyAsync(Guid gameId, string userId, CancellationToken cancellationToken = default);

    Task<UserGamesView> GetUserGamesAsync(string userId, CancellationToken cancellationToken = default);
}
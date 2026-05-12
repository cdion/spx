using Spx.Contracts;

namespace Spx.Games;

public interface IGamePersistence
{
    Task<Guid?> TryCreateGameAsync(CreateGamePersistenceRequest request, CancellationToken cancellationToken);

    Task<JoinGamePersistenceResult> JoinGameAsync(JoinGamePersistenceRequest request, CancellationToken cancellationToken);

    Task<LeaveGamePersistenceResult> LeaveGameAsync(Guid gameId, string userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<GameSessionPlayer>?> GetActiveSessionPlayersAsync(Guid gameId, CancellationToken cancellationToken);

    Task<GameLobbyView?> GetLobbyAsync(Guid gameId, string userId, CancellationToken cancellationToken);

    Task<UserGamesView> GetUserGamesAsync(string userId, CancellationToken cancellationToken);
}

public sealed record CreateGamePersistenceRequest(
    string UserId,
    string GameName,
    string PlayerName,
    string PlayerNameLookup,
    string InviteCode);

public sealed record JoinGamePersistenceRequest(
    string UserId,
    string InviteCode,
    string PlayerName,
    string PlayerNameLookup);

public sealed record JoinGamePersistenceResult(
    GameCommandResult Result,
    Guid? GameIdToPublish,
    bool PublishMessagesChanged);

public sealed record LeaveGamePersistenceResult(
    GameCommandResult Result,
    bool Changed);
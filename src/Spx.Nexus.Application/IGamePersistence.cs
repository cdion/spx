namespace Spx.Nexus.Application;

public interface IGamePersistence
{
    Task<Guid?> TryCreateGameAsync(
        CreateGamePersistenceRequest request,
        CancellationToken cancellationToken
    );

    Task<JoinGamePersistenceResult> JoinGameAsync(
        JoinGamePersistenceRequest request,
        CancellationToken cancellationToken
    );

    Task<LeaveGamePersistenceResult> LeaveGameAsync(
        Guid gameId,
        string userId,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<GameSessionParticipant>?> GetActiveSessionPlayersAsync(
        Guid gameId,
        CancellationToken cancellationToken
    );

    Task<GameLobbyView?> GetLobbyAsync(
        Guid gameId,
        string userId,
        CancellationToken cancellationToken
    );

    Task<UserGamesView> GetUserGamesAsync(string userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<GamePlayerView>> GetActivePlayersAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    );
}

public sealed record CreateGamePersistenceRequest(
    string UserId,
    string GameName,
    string PlayerName,
    string PlayerNameLookup,
    string InviteCode
);

public sealed record JoinGamePersistenceRequest(
    string UserId,
    string InviteCode,
    string PlayerName,
    string PlayerNameLookup
);

public sealed record JoinGamePersistenceResult(
    GameCommandOutcome Result,
    Guid? LobbyGameId = null,
    Guid? MessagesGameId = null
);

public sealed record LeaveGamePersistenceResult(GameCommandOutcome Result, Guid? PlayerId = null);

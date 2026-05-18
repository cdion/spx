namespace Spx.Game.Application;

public sealed record CreateGameRequest(string GameName, string PlayerName);

public sealed record JoinGameRequest(string InviteCode, string PlayerName);

public abstract record GameCommandOutcome;

public sealed record GameCommandSucceeded(Guid GameId) : GameCommandOutcome;

public sealed record GameCommandFailed(string ErrorMessage) : GameCommandOutcome;

public sealed record GamePlayerView(Guid PlayerId, string Name, DateTime JoinedAtUtc);

public sealed record GameLobbyView(
    Guid GameId,
    string Name,
    string InviteCode,
    GameStatus Status,
    int MaxPlayers,
    DateTime CreatedAtUtc,
    DateTime? EndedAtUtc,
    string CurrentPlayerName,
    Guid CurrentPlayerId,
    IReadOnlyList<GamePlayerView> Players,
    bool IsCurrentUserActive);

public sealed record GameSummaryView(
    Guid GameId,
    string Name,
    string InviteCode,
    GameStatus Status,
    int ActivePlayerCount,
    int MaxPlayers,
    DateTime CreatedAtUtc,
    DateTime? EndedAtUtc,
    string? CurrentPlayerName);

public sealed record UserGamesView(
    IReadOnlyList<GameSummaryView> OpenGames,
    IReadOnlyList<GameSummaryView> EndedGames);
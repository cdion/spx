using Spx.Web.Data;

namespace Spx.Web.Services;

public sealed record CreateGameRequest(string GameName, string PlayerName);

public sealed record JoinGameRequest(string InviteCode, string PlayerName);

public sealed record GameCommandResult(bool Succeeded, Guid? GameId, string? ErrorMessage)
{
    public static GameCommandResult Success(Guid gameId) => new(true, gameId, null);

    public static GameCommandResult Failure(string errorMessage) => new(false, null, errorMessage);
}

public sealed record GamePlayerView(Guid PlayerId, string Name, DateTime JoinedAtUtc, bool IsCurrentUser);

public sealed record GameLobbyView(
    Guid GameId,
    string Name,
    string InviteCode,
    GameStatus Status,
    int MaxPlayers,
    DateTime CreatedAtUtc,
    DateTime? EndedAtUtc,
    string CurrentPlayerName,
    IReadOnlyList<GamePlayerView> Players);

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
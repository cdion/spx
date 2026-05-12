using Spx.Contracts;

namespace Spx.Games;

public sealed record GamePageView(
    GameLobbyView Lobby,
    GameSessionPlayerView? Session);

public sealed record SubmitGameMoveResult(
    bool Succeeded,
    GameSessionPlayerView? Session,
    string? ErrorMessage)
{
    public static SubmitGameMoveResult Success(GameSessionPlayerView session)
        => new(true, session, null);

    public static SubmitGameMoveResult Failure(string errorMessage)
        => new(false, null, errorMessage);
}
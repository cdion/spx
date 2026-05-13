using Spx.Contracts;

namespace Spx.Games;

public sealed record GamePageView(
    GameLobbyView Lobby,
    GameSessionView? Session);

public sealed record SubmitGameMoveResult(
    bool Succeeded,
    GameSessionView? Session,
    string? ErrorMessage)
{
    public static SubmitGameMoveResult Success(GameSessionView session)
        => new(true, session, null);

    public static SubmitGameMoveResult Failure(string errorMessage)
        => new(false, null, errorMessage);
}
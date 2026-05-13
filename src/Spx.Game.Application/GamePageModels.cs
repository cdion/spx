using Spx.Contracts;

namespace Spx.Game.Application;

public sealed record GamePageView(
    GameLobbyView Lobby,
    GameSessionView? Session);

public abstract record SubmitGameMoveOutcome;

public sealed record SubmitGameMoveSucceeded(GameSessionView Session) : SubmitGameMoveOutcome;

public sealed record SubmitGameMoveFailed(string ErrorMessage) : SubmitGameMoveOutcome;
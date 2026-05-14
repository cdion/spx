using Spx.Contracts;

namespace Spx.Game.Application;

public sealed record GamePageView(
    GameLobbyView Lobby,
    GameSessionView? Session,
    GamePresenceView Presence);

public sealed record GamePresenceView(IReadOnlyList<Guid> OnlinePlayerIds)
{
    public static GamePresenceView Empty { get; } = new(Array.Empty<Guid>());
}

public abstract record SubmitGameMoveOutcome;

public sealed record SubmitGameMoveSucceeded(GameSessionView Session) : SubmitGameMoveOutcome;

public sealed record SubmitGameMoveFailed(string ErrorMessage) : SubmitGameMoveOutcome;
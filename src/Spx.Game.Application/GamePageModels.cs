using Spx.Contracts;

namespace Spx.Game.Application;

public sealed record GamePageView(
    GameLobbyView Lobby,
    GameSessionSnapshot? Session,
    GamePresenceView Presence);

public sealed record GamePresenceView(IReadOnlyList<Guid> OnlinePlayerIds)
{
    public static GamePresenceView Empty { get; } = new(Array.Empty<Guid>());
}

public abstract record GameSessionCommandOutcome;

public sealed record GameSessionCommandSucceeded(
    GameSessionSnapshot Session,
    IReadOnlyList<GameplayEvent> GameplayEvents,
    Guid? PendingGameplayEventBatchId = null) : GameSessionCommandOutcome
{
    public GameSessionCommandSucceeded(GameSessionSnapshot Session) : this(Session, [])
    {
    }
}

public sealed record GameSessionCommandFailed(string ErrorMessage) : GameSessionCommandOutcome;
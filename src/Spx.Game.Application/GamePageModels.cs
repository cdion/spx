namespace Spx.Game.Application;

public abstract record GameSessionOutcome;

public sealed record GameSessionFound(NexusGameView Session) : GameSessionOutcome;

public sealed record GameSessionUnavailable : GameSessionOutcome;

public sealed record GamePageView(
    GameLobbyView Lobby,
    NexusGameView? Session,
    GamePresenceView Presence
);

public sealed record GamePresenceView(IReadOnlyList<Guid> OnlinePlayerIds)
{
    public static GamePresenceView Empty { get; } = new(Array.Empty<Guid>());
}

public abstract record GameSessionCommandOutcome;

public sealed record GameSessionCommandSucceeded(NexusGameView Session) : GameSessionCommandOutcome;

public sealed record GameSessionCommandFailed(string ErrorMessage) : GameSessionCommandOutcome;

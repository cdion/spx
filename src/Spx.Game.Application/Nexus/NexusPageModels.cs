namespace Spx.Game.Application.Nexus;

public abstract record GameSessionOutcome;

public sealed record GameSessionFound(NexusSessionView Session) : GameSessionOutcome;

public sealed record GameSessionUnavailable : GameSessionOutcome;

public sealed record GamePageView(
    GameLobbyView Lobby,
    NexusSessionView? Session,
    GamePresenceView Presence
);

public abstract record GameSessionCommandOutcome;

public sealed record GameSessionCommandSucceeded(NexusSessionView Session)
    : GameSessionCommandOutcome;

public sealed record GameSessionCommandFailed(string ErrorMessage) : GameSessionCommandOutcome;

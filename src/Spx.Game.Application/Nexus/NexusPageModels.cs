using Spx.Nexus.Domain;

namespace Spx.Game.Application.Nexus;

public abstract record GameSessionOutcome;

public sealed record GameSessionFound(NexusGameView Session) : GameSessionOutcome;

public sealed record GameSessionUnavailable : GameSessionOutcome;

public sealed record GamePageView(
    GameLobbyView Lobby,
    NexusGameView? Session,
    GamePresenceView Presence
);

public abstract record GameSessionCommandOutcome;

public sealed record GameSessionCommandSucceeded(NexusGameView Session) : GameSessionCommandOutcome;

public sealed record GameSessionCommandFailed(string ErrorMessage) : GameSessionCommandOutcome;

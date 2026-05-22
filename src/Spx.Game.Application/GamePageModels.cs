namespace Spx.Game.Application;

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

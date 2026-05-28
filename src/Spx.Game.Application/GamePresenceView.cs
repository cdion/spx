namespace Spx.Game.Application;

public sealed record GamePresenceView(IReadOnlyList<Guid> OnlinePlayerIds)
{
    public static GamePresenceView Empty { get; } = new(Array.Empty<Guid>());
}

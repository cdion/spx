using Spx.Nexus.Application;

namespace Spx.Web.Components.Lobby;

public sealed class TimelineEntryState
{
    public Guid Key { get; init; }

    public GameTimelineEntryView? Message { get; set; }

    public LocalTimelineMessageState? Local { get; set; }

    public PendingMessageState? Pending { get; set; }
}

public sealed record LocalTimelineMessageState(
    string Title,
    string Body,
    DateTime CreatedAtUtc,
    GameMessageKind Kind
);

public sealed record PendingMessageState(
    string Body,
    Guid? RecipientPlayerId,
    string RecipientDisplayName,
    DateTime CreatedAtUtc,
    bool IsPrivate,
    bool Failed
)
{
    public bool Failed { get; set; } = Failed;
}

public sealed class TimelineScrollMetrics
{
    public double ScrollTop { get; set; }

    public double ScrollHeight { get; set; }

    public double ClientHeight { get; set; }
}

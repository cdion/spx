namespace Spx.Nexus.Application;

public sealed record SendGameMessageRequest(string Body);

public sealed record UpdateGameMessageRequest(string Body);

public abstract record GameMessageCommandOutcome;

public sealed record GameMessageCommandSucceeded(GameTimelineEntryView Message)
    : GameMessageCommandOutcome;

public sealed record GameMessageCommandFailed(string ErrorMessage) : GameMessageCommandOutcome;

public sealed record GameTimelinePageView(IReadOnlyList<GameTimelineEntryView> Items, bool HasMore);

public sealed record GameTimelineEntryView(
    Guid Id,
    GameMessageKind Kind,
    GameMessageSenderKind SenderKind,
    Guid? SenderPlayerId,
    string SenderDisplayName,
    Guid? RecipientPlayerId,
    string RecipientDisplayName,
    string Body,
    DateTime CreatedAtUtc,
    DateTime? EditedAtUtc,
    DateTime? DeletedAtUtc,
    bool IsCurrentUserSender,
    bool IsPrivate,
    bool CanEdit,
    bool CanDelete
);

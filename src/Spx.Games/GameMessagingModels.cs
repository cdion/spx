namespace Spx.Games;

public sealed record SendGameMessageRequest(string Body);

public sealed record UpdateGameMessageRequest(string Body);

public sealed record GameMessageCommandResult(bool Succeeded, GameMessageView? Message, string? ErrorMessage)
{
    public static GameMessageCommandResult Success(GameMessageView message) => new(true, message, null);

    public static GameMessageCommandResult Failure(string errorMessage) => new(false, null, errorMessage);
}

public sealed record GameMessagePageView(
    IReadOnlyList<GameMessageView> Items,
    bool HasMore);

public sealed record GameMessageView(
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
    bool CanDelete);
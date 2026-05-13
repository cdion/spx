namespace Spx.Game.Application;

public static class GameMessageSupport
{
    public static readonly TimeSpan MessageMutationWindow = TimeSpan.FromMinutes(2);
    public const int DefaultPageSize = 20;
    private const int MaxPageSize = 20;

    public static int NormalizeTake(int take)
        => Math.Clamp(take <= 0 ? DefaultPageSize : take, 1, MaxPageSize);

    public static GameTimelineEntryView MapMessage(GameMessageSnapshot message, string userId, bool canMutate, DateTime now)
    {
        var isCurrentUserSender = string.Equals(message.SenderUserId, userId, StringComparison.Ordinal);
        var isPlayerMessage = message.Kind == GameMessageKind.PlayerPublic || message.Kind == GameMessageKind.PlayerPrivate;
        var isWithinMutationWindow = now - message.CreatedAtUtc <= MessageMutationWindow;
        var canEdit = canMutate && isCurrentUserSender && isPlayerMessage && message.DeletedAtUtc is null && isWithinMutationWindow;
        var canDelete = canMutate && isCurrentUserSender && isPlayerMessage && message.DeletedAtUtc is null && isWithinMutationWindow;

        return new GameTimelineEntryView(
            message.Id,
            message.Kind,
            message.SenderKind,
            message.SenderPlayerId,
            message.SenderDisplayName,
            message.RecipientPlayerId,
            message.RecipientDisplayName,
            message.DeletedAtUtc is null ? message.Body : string.Empty,
            message.CreatedAtUtc,
            message.EditedAtUtc,
            message.DeletedAtUtc,
            isCurrentUserSender,
            message.Kind == GameMessageKind.PlayerPrivate,
            canEdit,
            canDelete);
    }

    public static bool TryNormalizeMessageBody(string value, out string normalizedValue, out string errorMessage)
    {
        normalizedValue = value.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();

        if (normalizedValue.Length == 0)
        {
            errorMessage = "Messages cannot be empty.";
            return false;
        }

        if (normalizedValue.Length > 1024)
        {
            errorMessage = "Messages must be 1024 characters or fewer.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    public sealed record GameMessageSnapshot(
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
        string? SenderUserId);
}
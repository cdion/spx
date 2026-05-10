using Microsoft.EntityFrameworkCore;
using Spx.Data;

namespace Spx.Games;

public sealed class GameMessagingService(
    ApplicationDbContext dbContext,
    IGameMessageEventsPublisher gameMessageEventsPublisher) : IGameMessagingService
{
    private static readonly TimeSpan MessageMutationWindow = TimeSpan.FromMinutes(2);
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 20;

    public async Task<GameMessagePageView?> GetMessagesAsync(Guid gameId, string userId, Guid? beforeMessageId = default, int take = DefaultPageSize, CancellationToken cancellationToken = default)
    {
        var access = await GetReadAccessAsync(gameId, userId, cancellationToken);
        if (access is null)
        {
            return null;
        }

        var pageSize = NormalizeTake(take);
        var query = BuildVisibleMessagesQuery(gameId, userId, access);

        if (beforeMessageId.HasValue)
        {
            query = query.Where(entry => entry.Id < beforeMessageId.Value);
        }

        var now = DateTime.UtcNow;
        var projections = await query
            .OrderByDescending(entry => entry.Id)
            .Take(pageSize + 1)
            .Select(entry => new GameMessageProjection(
                entry.Id,
                entry.Kind,
                entry.SenderKind,
                entry.SenderPlayerId,
                entry.SenderDisplayName,
                entry.RecipientPlayerId,
                entry.RecipientDisplayName,
                entry.Body,
                entry.CreatedAtUtc,
                entry.EditedAtUtc,
                entry.DeletedAtUtc,
                entry.SenderPlayer != null ? entry.SenderPlayer.UserId : null))
            .ToListAsync(cancellationToken);

        var hasMore = projections.Count > pageSize;
        if (hasMore)
        {
            projections.RemoveAt(projections.Count - 1);
        }

        var items = projections
            .Select(entry => MapMessage(entry, userId, access.IsActive, now))
            .ToList();

        return new GameMessagePageView(items, hasMore);
    }

    public async Task<IReadOnlyList<GameMessageView>?> GetMessageUpdatesAsync(Guid gameId, string userId, Guid? afterMessageId, int take = DefaultPageSize, CancellationToken cancellationToken = default)
    {
        var access = await GetReadAccessAsync(gameId, userId, cancellationToken);
        if (access is null)
        {
            return null;
        }

        if (!afterMessageId.HasValue)
        {
            return [];
        }

        var now = DateTime.UtcNow;
        return await BuildVisibleMessagesQuery(gameId, userId, access)
            .Where(entry => entry.Id > afterMessageId.Value)
            .OrderBy(entry => entry.Id)
            .Take(NormalizeTake(take))
            .Select(entry => new GameMessageProjection(
                entry.Id,
                entry.Kind,
                entry.SenderKind,
                entry.SenderPlayerId,
                entry.SenderDisplayName,
                entry.RecipientPlayerId,
                entry.RecipientDisplayName,
                entry.Body,
                entry.CreatedAtUtc,
                entry.EditedAtUtc,
                entry.DeletedAtUtc,
                entry.SenderPlayer != null ? entry.SenderPlayer.UserId : null))
            .AsAsyncEnumerable()
            .Select(entry => MapMessage(entry, userId, access.IsActive, now))
            .ToListAsync(cancellationToken);
    }

    public async Task<GameMessageCommandResult> SendPublicMessageAsync(Guid gameId, string userId, SendGameMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeMessageBody(request.Body, out var body, out var errorMessage))
        {
            return GameMessageCommandResult.Failure(errorMessage);
        }

        var sender = await GetActivePlayerAsync(gameId, userId, cancellationToken);
        if (sender is null)
        {
            return GameMessageCommandResult.Failure("You are not an active player in that game.");
        }

        var message = GameMessageFactory.CreatePublicPlayerMessage(gameId, sender, body, DateTime.UtcNow);
        dbContext.GameMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);
        await gameMessageEventsPublisher.PublishMessagesChangedAsync(gameId, cancellationToken);

        return GameMessageCommandResult.Success(MapMessage(message, userId, true, DateTime.UtcNow));
    }

    public async Task<GameMessageCommandResult> SendPrivateMessageAsync(Guid gameId, string userId, Guid recipientPlayerId, SendGameMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeMessageBody(request.Body, out var body, out var errorMessage))
        {
            return GameMessageCommandResult.Failure(errorMessage);
        }

        var sender = await GetActivePlayerAsync(gameId, userId, cancellationToken);
        if (sender is null)
        {
            return GameMessageCommandResult.Failure("You are not an active player in that game.");
        }

        var recipient = await dbContext.GamePlayers
            .SingleOrDefaultAsync(entry => entry.GameId == gameId && entry.Id == recipientPlayerId && entry.LeftAtUtc == null, cancellationToken);

        if (recipient is null)
        {
            return GameMessageCommandResult.Failure("That recipient is not an active player in this game.");
        }

        var message = GameMessageFactory.CreatePrivatePlayerMessage(gameId, sender, recipient, body, DateTime.UtcNow);
        dbContext.GameMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);
        await gameMessageEventsPublisher.PublishMessagesChangedAsync(gameId, cancellationToken);

        return GameMessageCommandResult.Success(MapMessage(message, userId, true, DateTime.UtcNow));
    }

    public async Task<GameMessageCommandResult> EditMessageAsync(Guid gameId, string userId, Guid messageId, UpdateGameMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeMessageBody(request.Body, out var body, out var errorMessage))
        {
            return GameMessageCommandResult.Failure(errorMessage);
        }

        if (!await IsActivePlayerAsync(gameId, userId, cancellationToken))
        {
            return GameMessageCommandResult.Failure("You are not an active player in that game.");
        }

        var message = await dbContext.GameMessages
            .Include(entry => entry.SenderPlayer)
            .SingleOrDefaultAsync(entry => entry.GameId == gameId
                && entry.Id == messageId
                && (entry.Kind == GameMessageKind.PlayerPublic || entry.Kind == GameMessageKind.PlayerPrivate), cancellationToken);

        if (message is null || message.SenderPlayer?.UserId != userId)
        {
            return GameMessageCommandResult.Failure("That message could not be edited.");
        }

        if (message.DeletedAtUtc is not null)
        {
            return GameMessageCommandResult.Failure("Deleted messages cannot be edited.");
        }

        var now = DateTime.UtcNow;
        if (now - message.CreatedAtUtc > MessageMutationWindow)
        {
            return GameMessageCommandResult.Failure("That message can no longer be edited.");
        }

        message.Body = body;
        message.EditedAtUtc = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        await gameMessageEventsPublisher.PublishMessagesChangedAsync(gameId, cancellationToken);

        return GameMessageCommandResult.Success(MapMessage(message, userId, true, now));
    }

    public async Task<GameMessageCommandResult> DeleteMessageAsync(Guid gameId, string userId, Guid messageId, CancellationToken cancellationToken = default)
    {
        if (!await IsActivePlayerAsync(gameId, userId, cancellationToken))
        {
            return GameMessageCommandResult.Failure("You are not an active player in that game.");
        }

        var message = await dbContext.GameMessages
            .Include(entry => entry.SenderPlayer)
            .SingleOrDefaultAsync(entry => entry.GameId == gameId
                && entry.Id == messageId
                && (entry.Kind == GameMessageKind.PlayerPublic || entry.Kind == GameMessageKind.PlayerPrivate), cancellationToken);

        if (message is null || message.SenderPlayer?.UserId != userId)
        {
            return GameMessageCommandResult.Failure("That message could not be deleted.");
        }

        if (message.DeletedAtUtc is not null)
        {
            return GameMessageCommandResult.Failure("That message has already been deleted.");
        }

        var now = DateTime.UtcNow;
        if (now - message.CreatedAtUtc > MessageMutationWindow)
        {
            return GameMessageCommandResult.Failure("That message can no longer be deleted.");
        }

        message.Body = string.Empty;
        message.DeletedAtUtc = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        await gameMessageEventsPublisher.PublishMessagesChangedAsync(gameId, cancellationToken);

        return GameMessageCommandResult.Success(MapMessage(message, userId, true, now));
    }

    private IQueryable<GameMessage> BuildVisibleMessagesQuery(Guid gameId, string userId, MessageReadAccess access)
    {
        var query = dbContext.GameMessages
            .AsNoTracking()
            .Where(entry => entry.GameId == gameId)
            .Where(entry => entry.RecipientPlayerId == null
                || (entry.SenderPlayer != null && entry.SenderPlayer.UserId == userId)
                || (entry.RecipientPlayer != null && entry.RecipientPlayer.UserId == userId));

        if (access.VisibleThroughMessageId.HasValue)
        {
            query = query.Where(entry => entry.Id <= access.VisibleThroughMessageId.Value);
        }
        else if (access.LeftAtUtc.HasValue)
        {
            query = query.Where(entry => entry.CreatedAtUtc <= access.LeftAtUtc.Value);
        }

        return query;
    }

    private async Task<GamePlayer?> GetActivePlayerAsync(Guid gameId, string userId, CancellationToken cancellationToken)
        => await dbContext.GamePlayers
            .SingleOrDefaultAsync(entry => entry.GameId == gameId && entry.UserId == userId && entry.LeftAtUtc == null, cancellationToken);

    private async Task<bool> IsActivePlayerAsync(Guid gameId, string userId, CancellationToken cancellationToken)
        => await dbContext.GamePlayers
            .AnyAsync(entry => entry.GameId == gameId && entry.UserId == userId && entry.LeftAtUtc == null, cancellationToken);

    private async Task<MessageReadAccess?> GetReadAccessAsync(Guid gameId, string userId, CancellationToken cancellationToken)
    {
        var activePlayer = await dbContext.GamePlayers
            .AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.GameId == gameId && entry.UserId == userId && entry.LeftAtUtc == null, cancellationToken);

        if (activePlayer is not null)
        {
            return new MessageReadAccess(true, null, null);
        }

        var formerPlayer = await dbContext.GamePlayers
            .AsNoTracking()
            .Where(entry => entry.GameId == gameId && entry.UserId == userId && entry.LeftAtUtc != null)
            .OrderByDescending(entry => entry.LeftAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (formerPlayer is null)
        {
            return null;
        }

        return new MessageReadAccess(false, formerPlayer.VisibleThroughMessageId, formerPlayer.LeftAtUtc);
    }

    private static int NormalizeTake(int take)
        => Math.Clamp(take <= 0 ? DefaultPageSize : take, 1, MaxPageSize);

    private static GameMessageView MapMessage(GameMessage message, string userId, bool canMutate, DateTime now)
        => MapMessage(
            new GameMessageProjection(
                message.Id,
                message.Kind,
                message.SenderKind,
                message.SenderPlayerId,
                message.SenderDisplayName,
                message.RecipientPlayerId,
                message.RecipientDisplayName,
                message.Body,
                message.CreatedAtUtc,
                message.EditedAtUtc,
                message.DeletedAtUtc,
                message.SenderPlayer?.UserId),
            userId,
            canMutate,
            now);

    private static GameMessageView MapMessage(GameMessageProjection message, string userId, bool canMutate, DateTime now)
    {
        var isCurrentUserSender = string.Equals(message.SenderUserId, userId, StringComparison.Ordinal);
        var isPlayerMessage = message.Kind == GameMessageKind.PlayerPublic || message.Kind == GameMessageKind.PlayerPrivate;
        var isWithinMutationWindow = now - message.CreatedAtUtc <= MessageMutationWindow;
        var canEdit = canMutate && isCurrentUserSender && isPlayerMessage && message.DeletedAtUtc is null && isWithinMutationWindow;
        var canDelete = canMutate && isCurrentUserSender && isPlayerMessage && message.DeletedAtUtc is null && isWithinMutationWindow;

        return new GameMessageView(
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

    private static bool TryNormalizeMessageBody(string value, out string normalizedValue, out string errorMessage)
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

    private sealed record MessageReadAccess(bool IsActive, Guid? VisibleThroughMessageId, DateTime? LeftAtUtc);

    private sealed record GameMessageProjection(
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
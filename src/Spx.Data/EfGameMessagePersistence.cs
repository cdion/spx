using Microsoft.EntityFrameworkCore;
using Spx.Game.Application;

namespace Spx.Data;

internal sealed class EfGameMessagePersistence(
    IDbContextFactory<ApplicationDbContext> contextFactory
) : IGameMessagePersistence
{
    public async Task<GameTimelinePageView?> GetMessagesAsync(
        Guid gameId,
        Guid playerId,
        Guid? beforeMessageId,
        int take,
        CancellationToken cancellationToken
    )
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var support = new GameMessagePersistenceSupport(dbContext);
        var access = await support.GetReadAccessAsync(gameId, playerId, cancellationToken);
        if (access is null)
        {
            return null;
        }

        var pageSize = GameMessageSupport.NormalizeTake(take);
        var query = support.BuildVisibleMessagesQuery(gameId, playerId, access);

        if (beforeMessageId.HasValue)
        {
            query = query.Where(entry => entry.Id < beforeMessageId.Value);
        }

        var now = DateTime.UtcNow;
        var projections = await query
            .OrderByDescending(entry => entry.Id)
            .Take(pageSize + 1)
            .Select(entry => new GameMessageSupport.GameMessageSnapshot(
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
                entry.DeletedAtUtc
            ))
            .ToListAsync(cancellationToken);

        var hasMore = projections.Count > pageSize;
        if (hasMore)
        {
            projections.RemoveAt(projections.Count - 1);
        }

        var items = projections
            .Select(entry => GameMessageSupport.MapMessage(entry, playerId, access.IsActive, now))
            .ToList();

        return new GameTimelinePageView(items, hasMore);
    }

    public async Task<IReadOnlyList<GameTimelineEntryView>?> GetMessageUpdatesAsync(
        Guid gameId,
        Guid playerId,
        Guid? afterMessageId,
        int take,
        CancellationToken cancellationToken
    )
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var support = new GameMessagePersistenceSupport(dbContext);
        var access = await support.GetReadAccessAsync(gameId, playerId, cancellationToken);
        if (access is null)
        {
            return null;
        }

        if (!afterMessageId.HasValue)
        {
            return [];
        }

        var now = DateTime.UtcNow;
        return await support
            .BuildVisibleMessagesQuery(gameId, playerId, access)
            .Where(entry => entry.Id > afterMessageId.Value)
            .OrderBy(entry => entry.Id)
            .Take(GameMessageSupport.NormalizeTake(take))
            .Select(entry => new GameMessageSupport.GameMessageSnapshot(
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
                entry.DeletedAtUtc
            ))
            .AsAsyncEnumerable()
            .Select(entry => GameMessageSupport.MapMessage(entry, playerId, access.IsActive, now))
            .ToListAsync(cancellationToken);
    }

    public async Task<GameMessageCommandOutcome> SendPublicMessageAsync(
        Guid gameId,
        Guid playerId,
        string body,
        CancellationToken cancellationToken
    )
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var support = new GameMessagePersistenceSupport(dbContext);
        var sender = await support.GetActivePlayerAsync(gameId, playerId, cancellationToken);
        if (sender is null)
        {
            return new GameMessageCommandFailed("You are not an active player in that game.");
        }

        var now = DateTime.UtcNow;
        var message = GameMessageFactory.CreatePublicPlayerMessage(gameId, sender, body, now);
        dbContext.GameMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new GameMessageCommandSucceeded(
            GameMessageSupport.MapMessage(
                new GameMessageSupport.GameMessageSnapshot(
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
                    message.DeletedAtUtc
                ),
                playerId,
                true,
                now
            )
        );
    }

    public async Task<GameMessageCommandOutcome> SendPrivateMessageAsync(
        Guid gameId,
        Guid playerId,
        Guid recipientPlayerId,
        string body,
        CancellationToken cancellationToken
    )
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var support = new GameMessagePersistenceSupport(dbContext);
        var sender = await support.GetActivePlayerAsync(gameId, playerId, cancellationToken);
        if (sender is null)
        {
            return new GameMessageCommandFailed("You are not an active player in that game.");
        }

        var recipient = await dbContext.GamePlayers.SingleOrDefaultAsync(
            entry =>
                entry.GameId == gameId && entry.Id == recipientPlayerId && entry.LeftAtUtc == null,
            cancellationToken
        );

        if (recipient is null)
        {
            return new GameMessageCommandFailed(
                "That recipient is not an active player in this game."
            );
        }

        var now = DateTime.UtcNow;
        var message = GameMessageFactory.CreatePrivatePlayerMessage(
            gameId,
            sender,
            recipient,
            body,
            now
        );
        dbContext.GameMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new GameMessageCommandSucceeded(
            GameMessageSupport.MapMessage(
                new GameMessageSupport.GameMessageSnapshot(
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
                    message.DeletedAtUtc
                ),
                playerId,
                true,
                now
            )
        );
    }

    public async Task<GameMessageCommandOutcome> EditMessageAsync(
        Guid gameId,
        Guid playerId,
        Guid messageId,
        string body,
        CancellationToken cancellationToken
    )
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var support = new GameMessagePersistenceSupport(dbContext);
        if (!await support.IsActivePlayerAsync(gameId, playerId, cancellationToken))
        {
            return new GameMessageCommandFailed("You are not an active player in that game.");
        }

        var message = await dbContext
            .GameMessages.Include(entry => entry.SenderPlayer)
            .SingleOrDefaultAsync(
                entry =>
                    entry.GameId == gameId
                    && entry.Id == messageId
                    && (
                        entry.Kind == GameMessageKind.PlayerPublic
                        || entry.Kind == GameMessageKind.PlayerPrivate
                    ),
                cancellationToken
            );

        if (message is null || message.SenderPlayerId != playerId)
        {
            return new GameMessageCommandFailed("That message could not be edited.");
        }

        if (message.DeletedAtUtc is not null)
        {
            return new GameMessageCommandFailed("Deleted messages cannot be edited.");
        }

        var now = DateTime.UtcNow;
        if (now - message.CreatedAtUtc > GameMessageSupport.MessageMutationWindow)
        {
            return new GameMessageCommandFailed("That message can no longer be edited.");
        }

        message.Body = body;
        message.EditedAtUtc = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        return new GameMessageCommandSucceeded(
            GameMessageSupport.MapMessage(
                new GameMessageSupport.GameMessageSnapshot(
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
                    message.DeletedAtUtc
                ),
                playerId,
                true,
                now
            )
        );
    }

    public async Task<GameMessageCommandOutcome> DeleteMessageAsync(
        Guid gameId,
        Guid playerId,
        Guid messageId,
        CancellationToken cancellationToken
    )
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var support = new GameMessagePersistenceSupport(dbContext);
        if (!await support.IsActivePlayerAsync(gameId, playerId, cancellationToken))
        {
            return new GameMessageCommandFailed("You are not an active player in that game.");
        }

        var message = await dbContext
            .GameMessages.Include(entry => entry.SenderPlayer)
            .SingleOrDefaultAsync(
                entry =>
                    entry.GameId == gameId
                    && entry.Id == messageId
                    && (
                        entry.Kind == GameMessageKind.PlayerPublic
                        || entry.Kind == GameMessageKind.PlayerPrivate
                    ),
                cancellationToken
            );

        if (message is null || message.SenderPlayerId != playerId)
        {
            return new GameMessageCommandFailed("That message could not be deleted.");
        }

        if (message.DeletedAtUtc is not null)
        {
            return new GameMessageCommandFailed("That message has already been deleted.");
        }

        var now = DateTime.UtcNow;
        if (now - message.CreatedAtUtc > GameMessageSupport.MessageMutationWindow)
        {
            return new GameMessageCommandFailed("That message can no longer be deleted.");
        }

        message.Body = string.Empty;
        message.DeletedAtUtc = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        return new GameMessageCommandSucceeded(
            GameMessageSupport.MapMessage(
                new GameMessageSupport.GameMessageSnapshot(
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
                    message.DeletedAtUtc
                ),
                playerId,
                true,
                now
            )
        );
    }
}

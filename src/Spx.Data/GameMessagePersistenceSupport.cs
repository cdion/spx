using Microsoft.EntityFrameworkCore;
using Spx.Games;

namespace Spx.Data;

internal sealed class GameMessagePersistenceSupport(ApplicationDbContext dbContext)
{
    public IQueryable<GameMessage> BuildVisibleMessagesQuery(Guid gameId, string userId, MessageReadAccess access)
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

    public async Task<GamePlayer?> GetActivePlayerAsync(Guid gameId, string userId, CancellationToken cancellationToken)
        => await dbContext.GamePlayers
            .SingleOrDefaultAsync(entry => entry.GameId == gameId && entry.UserId == userId && entry.LeftAtUtc == null, cancellationToken);

    public async Task<bool> IsActivePlayerAsync(Guid gameId, string userId, CancellationToken cancellationToken)
        => await dbContext.GamePlayers
            .AnyAsync(entry => entry.GameId == gameId && entry.UserId == userId && entry.LeftAtUtc == null, cancellationToken);

    public async Task<MessageReadAccess?> GetReadAccessAsync(Guid gameId, string userId, CancellationToken cancellationToken)
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

    internal sealed record MessageReadAccess(bool IsActive, Guid? VisibleThroughMessageId, DateTime? LeftAtUtc);
}
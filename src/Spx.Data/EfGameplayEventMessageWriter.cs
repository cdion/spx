using Microsoft.EntityFrameworkCore;
using Spx.Contracts;
using Spx.Game.Application;

namespace Spx.Data;

internal sealed class EfGameplayEventMessageWriter(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IGameplayEventMessageFormatter gameplayEventMessageFormatter) : IGameplayEventMessageWriter
{
    public async Task<int> PersistResolvedBatchAsync(GameSessionSnapshot session, IReadOnlyList<GameplayEvent> gameplayEvents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(gameplayEvents);

        if (session.LastResolvedBatch is null || gameplayEvents.Count == 0)
        {
            return 0;
        }

        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);

        var participantIds = session.LastResolvedBatch.Players
            .Select(player => player.Participant.PlayerId)
            .Append(session.Completion?.Winner?.PlayerId ?? Guid.Empty)
            .Where(playerId => playerId != Guid.Empty)
            .Distinct()
            .ToArray();

        var playerNames = await dbContext.GamePlayers
            .AsNoTracking()
            .Where(entry => entry.GameId == session.GameId && participantIds.Contains(entry.Id))
            .ToDictionaryAsync(entry => entry.Id, entry => entry.Name, cancellationToken);

        var messageBodies = gameplayEventMessageFormatter.CreateMessageBodies(session, gameplayEvents, playerNames);
        if (messageBodies.Count == 0)
        {
            return 0;
        }

        var resolvedAtUtc = session.LastResolvedBatch.ResolvedAtUtc;
        var existingBodies = await dbContext.GameMessages
            .AsNoTracking()
            .Where(entry => entry.GameId == session.GameId
                && entry.Kind == GameMessageKind.GameplayEvent
                && entry.CreatedAtUtc == resolvedAtUtc)
            .Select(entry => entry.Body)
            .ToListAsync(cancellationToken);

        var newBodies = messageBodies
            .Where(body => !existingBodies.Contains(body, StringComparer.Ordinal))
            .ToArray();

        if (newBodies.Length == 0)
        {
            return 0;
        }

        foreach (var body in newBodies)
        {
            dbContext.GameMessages.Add(GameMessageFactory.CreateGameplayEvent(session.GameId, body, resolvedAtUtc));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return newBodies.Length;
    }
}
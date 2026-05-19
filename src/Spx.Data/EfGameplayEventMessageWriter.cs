using Microsoft.EntityFrameworkCore;
using Spx.Game.Application;
using Spx.Game.Domain;

namespace Spx.Data;

internal sealed class EfGameplayEventMessageWriter(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IGameplayEventMessageFormatter gameplayEventMessageFormatter
) : IGameplayEventMessageWriter
{
    public async Task<int> PersistResolvedBatchAsync(
        Guid gameId,
        GameResolvedBatchView? lastResolvedBatch,
        GameCompletionView? completion,
        IReadOnlyList<GameplayEvent> gameplayEvents,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(gameplayEvents);

        if (lastResolvedBatch is null || gameplayEvents.Count == 0)
        {
            return 0;
        }

        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);

        var participantIds = lastResolvedBatch
            .Players.Select(player => player.Participant.PlayerId)
            .Append(completion?.Winner?.PlayerId ?? Guid.Empty)
            .Where(playerId => playerId != Guid.Empty)
            .Distinct()
            .ToArray();

        var playerNames = await dbContext
            .GamePlayers.AsNoTracking()
            .Where(entry => entry.GameId == gameId && participantIds.Contains(entry.Id))
            .ToDictionaryAsync(entry => entry.Id, entry => entry.Name, cancellationToken);

        var messageBodies = gameplayEventMessageFormatter.CreateMessageBodies(
            lastResolvedBatch,
            completion,
            gameplayEvents,
            playerNames
        );
        if (messageBodies.Count == 0)
        {
            return 0;
        }

        var resolvedAtUtc = lastResolvedBatch.ResolvedAtUtc;
        var existingBodies = await dbContext
            .GameMessages.AsNoTracking()
            .Where(entry =>
                entry.GameId == gameId
                && entry.Kind == GameMessageKind.GameplayEvent
                && entry.CreatedAtUtc == resolvedAtUtc
            )
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
            dbContext.GameMessages.Add(
                GameMessageFactory.CreateGameplayEvent(gameId, body, resolvedAtUtc)
            );
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return newBodies.Length;
    }
}

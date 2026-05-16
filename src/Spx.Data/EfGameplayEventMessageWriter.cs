using Microsoft.EntityFrameworkCore;
using Spx.Contracts;
using Spx.Game.Application;

namespace Spx.Data;

internal sealed class EfGameplayEventMessageWriter(
    IDbContextFactory<ApplicationDbContext> contextFactory) : IGameplayEventMessageWriter
{
    public async Task<int> PersistResolvedBatchAsync(GameSessionView session, IReadOnlyList<string> gameplayEvents, CancellationToken cancellationToken = default)
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

        var messageBodies = CreateMessageBodies(session, gameplayEvents, playerNames);
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

    private static IReadOnlyList<string> CreateMessageBodies(GameSessionView session, IReadOnlyList<string> gameplayEvents, IReadOnlyDictionary<Guid, string> playerNames)
    {
        var resolvedBatch = session.LastResolvedBatch;
        if (resolvedBatch is null)
        {
            return [];
        }

        var summaryLines = new List<string>
        {
            $"Round {resolvedBatch.RoundNumber} resolved."
        };

        foreach (var player in resolvedBatch.Players)
        {
            var playerName = ResolvePlayerName(player.Participant, playerNames);
            var playedCards = player.PlayedCards.Select(card => card.Card.DisplayName).ToArray();
            summaryLines.Add(playedCards.Length == 0
                ? $"{playerName} passed."
                : $"{playerName} played {string.Join(", ", playedCards)}.");
        }

        foreach (var entry in gameplayEvents)
        {
            summaryLines.Add(NormalizeEventText(entry, resolvedBatch.Players, playerNames));
        }

        var messages = new List<string>
        {
            string.Join("\n", summaryLines)
        };

        if (session.Completion is not null && session.Completion.CompletedAtUtc == resolvedBatch.ResolvedAtUtc)
        {
            messages.Add(CreateCompletionBody(session.Completion, playerNames));
        }

        return messages;
    }

    private static string NormalizeEventText(
        string text,
        IEnumerable<GameResolvedPlayerBatchView> players,
        IReadOnlyDictionary<Guid, string> playerNames)
    {
        var normalizedText = text;
        foreach (var player in players)
        {
            var playerName = ResolvePlayerName(player.Participant, playerNames);
            normalizedText = normalizedText.Replace(player.Participant.UserId, playerName, StringComparison.Ordinal);
            normalizedText = normalizedText.Replace(player.Participant.PlayerId.ToString(), playerName, StringComparison.OrdinalIgnoreCase);
        }

        return normalizedText;
    }

    private static string CreateCompletionBody(GameCompletionView completion, IReadOnlyDictionary<Guid, string> playerNames)
        => completion.Reason switch
        {
            GameCompletionReason.Victory when completion.Winner is not null
                => $"{ResolvePlayerName(completion.Winner, playerNames)} won by producing Victory.",
            GameCompletionReason.Abandoned when completion.Winner is not null
                => $"{ResolvePlayerName(completion.Winner, playerNames)} won because the opponent abandoned the match.",
            _ => "The match ended in a draw."
        };

    private static string ResolvePlayerName(GameSessionParticipantView participant, IReadOnlyDictionary<Guid, string> playerNames)
        => playerNames.TryGetValue(participant.PlayerId, out var playerName)
            ? playerName
            : participant.UserId;
}
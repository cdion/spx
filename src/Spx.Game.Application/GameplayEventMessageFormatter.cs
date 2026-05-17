using Spx.Contracts;

namespace Spx.Game.Application;

public sealed class GameplayEventMessageFormatter : IGameplayEventMessageFormatter
{
    public IReadOnlyList<string> CreateMessageBodies(
        GameSessionSnapshot session,
        IReadOnlyList<GameplayEvent> gameplayEvents,
        IReadOnlyDictionary<Guid, string> playerNames)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(gameplayEvents);
        ArgumentNullException.ThrowIfNull(playerNames);

        var resolvedBatch = session.LastResolvedBatch;
        if (resolvedBatch is null)
        {
            return [];
        }

        var userNames = resolvedBatch.Players
            .Select(player => player.Participant)
            .DistinctBy(participant => participant.UserId)
            .ToDictionary(
                participant => participant.UserId,
                participant => ResolvePlayerName(participant, playerNames),
                StringComparer.Ordinal);

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

        foreach (var gameplayEvent in gameplayEvents)
        {
            summaryLines.Add(FormatGameplayEvent(gameplayEvent, userNames));
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

    private static string FormatGameplayEvent(GameplayEvent gameplayEvent, IReadOnlyDictionary<string, string> userNames)
        => gameplayEvent.Kind switch
        {
            GameplayEventKind.Fizzled => $"{ResolveUserName(gameplayEvent.ActorUserId, userNames)}'s {gameplayEvent.SourceCardDefinition} fizzled.",
            GameplayEventKind.DiscardedCard => $"{ResolveUserName(gameplayEvent.ActorUserId, userNames)} resolved {gameplayEvent.SourceCardDefinition} and discarded {gameplayEvent.TargetCardDefinition} from {ResolveUserName(gameplayEvent.TargetUserId, userNames)}.",
            GameplayEventKind.CreatedCard => FormatCreatedEvent(gameplayEvent, userNames),
            GameplayEventKind.ConvertedCard => $"{ResolveUserName(gameplayEvent.ActorUserId, userNames)} resolved {gameplayEvent.SourceCardDefinition} and converted {gameplayEvent.TargetCardDefinition} into {gameplayEvent.ProducedCardDefinition}.",
            GameplayEventKind.ScheduledReturnToHand => $"{ResolveUserName(gameplayEvent.ActorUserId, userNames)} resolved {gameplayEvent.SourceCardDefinition} and will return {gameplayEvent.TargetCardDefinition} to hand.",
            GameplayEventKind.ReturnedToHand => $"{ResolveUserName(gameplayEvent.ActorUserId, userNames)} returned {gameplayEvent.SourceCardDefinition} to hand.",
            GameplayEventKind.Resolved => $"{ResolveUserName(gameplayEvent.ActorUserId, userNames)} resolved {gameplayEvent.SourceCardDefinition}.",
            _ => throw new InvalidOperationException("Unknown gameplay event kind.")
        };

    private static string FormatCreatedEvent(GameplayEvent gameplayEvent, IReadOnlyDictionary<string, string> userNames)
    {
        var actorName = ResolveUserName(gameplayEvent.ActorUserId, userNames);
        return gameplayEvent.SourceCardDefinition switch
        {
            GameCardDefinition.Extract => $"{actorName} extracted {gameplayEvent.ProducedCardDefinition}.",
            GameCardDefinition.Refine => $"{actorName} refined {gameplayEvent.ProducedCardDefinition}.",
            GameCardDefinition.Produce => $"{actorName} produced {gameplayEvent.ProducedCardDefinition}.",
            _ => $"{actorName} resolved {gameplayEvent.SourceCardDefinition} and created {gameplayEvent.ProducedCardDefinition}."
        };
    }

    private static string CreateCompletionBody(GameCompletionSnapshot completion, IReadOnlyDictionary<Guid, string> playerNames)
        => completion.Reason switch
        {
            GameCompletionReason.Victory when completion.Winner is not null
                => $"{ResolvePlayerName(completion.Winner, playerNames)} won by producing Victory.",
            GameCompletionReason.Abandoned when completion.Winner is not null
                => $"{ResolvePlayerName(completion.Winner, playerNames)} won because the opponent abandoned the match.",
            _ => "The match ended in a draw."
        };

    private static string ResolvePlayerName(GameSessionParticipant participant, IReadOnlyDictionary<Guid, string> playerNames)
        => playerNames.TryGetValue(participant.PlayerId, out var playerName)
            ? playerName
            : participant.UserId;

    private static string ResolveUserName(string? userId, IReadOnlyDictionary<string, string> userNames)
        => userId is not null && userNames.TryGetValue(userId, out var userName)
            ? userName
            : userId ?? string.Empty;
}
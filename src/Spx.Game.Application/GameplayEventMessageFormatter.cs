namespace Spx.Game.Application;

public sealed class GameplayEventMessageFormatter : IGameplayEventMessageFormatter
{
    public IReadOnlyList<string> CreateMessageBodies(
        GameResolvedBatchView? lastResolvedBatch,
        GameCompletionView? completion,
        IReadOnlyList<GameplayEvent> gameplayEvents,
        IReadOnlyDictionary<Guid, string> playerNames)
    {
        ArgumentNullException.ThrowIfNull(gameplayEvents);
        ArgumentNullException.ThrowIfNull(playerNames);

        if (lastResolvedBatch is null)
        {
            return [];
        }

        var summaryLines = new List<string>
        {
            $"Round {lastResolvedBatch.RoundNumber} resolved."
        };

        foreach (var player in lastResolvedBatch.Players)
        {
            var playerName = ResolvePlayerName(player.Participant, playerNames);
            var playedCards = player.PlayedCards.Select(card => card.Card.DisplayName).ToArray();
            summaryLines.Add(playedCards.Length == 0
                ? $"{playerName} passed."
                : $"{playerName} played {string.Join(", ", playedCards)}.");
        }

        foreach (var gameplayEvent in gameplayEvents)
        {
            summaryLines.Add(FormatGameplayEvent(gameplayEvent, playerNames));
        }

        var messages = new List<string>
        {
            string.Join("\n", summaryLines)
        };

        if (completion is not null && completion.CompletedAtUtc == lastResolvedBatch.ResolvedAtUtc)
        {
            messages.Add(CreateCompletionBody(completion, playerNames));
        }

        return messages;
    }

    private static string FormatGameplayEvent(GameplayEvent gameplayEvent, IReadOnlyDictionary<Guid, string> playerNames)
        => gameplayEvent.Kind switch
        {
            GameplayEventKind.Fizzled => $"{ResolvePlayerName(gameplayEvent.ActorPlayerId, playerNames)}'s {gameplayEvent.SourceCardDefinition} fizzled.",
            GameplayEventKind.DiscardedCard => $"{ResolvePlayerName(gameplayEvent.ActorPlayerId, playerNames)} resolved {gameplayEvent.SourceCardDefinition} and discarded {gameplayEvent.TargetCardDefinition} from {ResolvePlayerName(gameplayEvent.TargetPlayerId, playerNames)}.",
            GameplayEventKind.CreatedCard => FormatCreatedEvent(gameplayEvent, playerNames),
            GameplayEventKind.ConvertedCard => $"{ResolvePlayerName(gameplayEvent.ActorPlayerId, playerNames)} resolved {gameplayEvent.SourceCardDefinition} and converted {gameplayEvent.TargetCardDefinition} into {gameplayEvent.ProducedCardDefinition}.",
            GameplayEventKind.ScheduledReturnToHand => $"{ResolvePlayerName(gameplayEvent.ActorPlayerId, playerNames)} resolved {gameplayEvent.SourceCardDefinition} and will return {gameplayEvent.TargetCardDefinition} to hand.",
            GameplayEventKind.ReturnedToHand => $"{ResolvePlayerName(gameplayEvent.ActorPlayerId, playerNames)} returned {gameplayEvent.SourceCardDefinition} to hand.",
            GameplayEventKind.Resolved => $"{ResolvePlayerName(gameplayEvent.ActorPlayerId, playerNames)} resolved {gameplayEvent.SourceCardDefinition}.",
            _ => throw new InvalidOperationException("Unknown gameplay event kind.")
        };

    private static string FormatCreatedEvent(GameplayEvent gameplayEvent, IReadOnlyDictionary<Guid, string> playerNames)
    {
        var actorName = ResolvePlayerName(gameplayEvent.ActorPlayerId, playerNames);
        return gameplayEvent.SourceCardDefinition switch
        {
            GameCardDefinition.Extract => $"{actorName} extracted {gameplayEvent.ProducedCardDefinition}.",
            GameCardDefinition.Refine => $"{actorName} refined {gameplayEvent.ProducedCardDefinition}.",
            GameCardDefinition.Produce => $"{actorName} produced {gameplayEvent.ProducedCardDefinition}.",
            _ => $"{actorName} resolved {gameplayEvent.SourceCardDefinition} and created {gameplayEvent.ProducedCardDefinition}."
        };
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

    private static string ResolvePlayerName(GameSessionParticipant participant, IReadOnlyDictionary<Guid, string> playerNames)
        => playerNames.TryGetValue(participant.PlayerId, out var playerName)
            ? playerName
            : participant.PlayerId.ToString();

    private static string ResolvePlayerName(Guid playerId, IReadOnlyDictionary<Guid, string> playerNames)
        => playerNames.TryGetValue(playerId, out var playerName) ? playerName : playerId.ToString();

    private static string ResolvePlayerName(Guid? playerId, IReadOnlyDictionary<Guid, string> playerNames)
        => playerId.HasValue ? ResolvePlayerName(playerId.Value, playerNames) : string.Empty;
}
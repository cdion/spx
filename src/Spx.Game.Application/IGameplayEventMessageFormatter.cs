namespace Spx.Game.Application;

public interface IGameplayEventMessageFormatter
{
    IReadOnlyList<string> CreateMessageBodies(
        GameResolvedBatchView? lastResolvedBatch,
        GameCompletionView? completion,
        IReadOnlyList<GameplayEvent> gameplayEvents,
        IReadOnlyDictionary<Guid, string> playerNames);
}
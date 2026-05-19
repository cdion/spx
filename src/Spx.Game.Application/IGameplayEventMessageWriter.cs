namespace Spx.Game.Application;

public interface IGameplayEventMessageWriter
{
    Task<int> PersistResolvedBatchAsync(
        Guid gameId,
        GameResolvedBatchView? lastResolvedBatch,
        GameCompletionView? completion,
        IReadOnlyList<GameplayEvent> gameplayEvents,
        CancellationToken cancellationToken = default
    );
}

using Spx.Contracts;

namespace Spx.Game.Application;

public interface IGameplayEventMessageWriter
{
    Task<int> PersistResolvedBatchAsync(GameSessionSnapshot session, IReadOnlyList<GameplayEvent> gameplayEvents, CancellationToken cancellationToken = default);
}
using Spx.Contracts;

namespace Spx.Game.Application;

public interface IGameplayEventMessageWriter
{
    Task<int> PersistResolvedBatchAsync(GameSessionView session, IReadOnlyList<string> gameplayEvents, CancellationToken cancellationToken = default);
}
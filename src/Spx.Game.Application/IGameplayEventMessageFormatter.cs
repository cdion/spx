using Spx.Contracts;

namespace Spx.Game.Application;

public interface IGameplayEventMessageFormatter
{
    IReadOnlyList<string> CreateMessageBodies(
    GameSessionSnapshot session,
        IReadOnlyList<GameplayEvent> gameplayEvents,
        IReadOnlyDictionary<Guid, string> playerNames);
}
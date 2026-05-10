using Spx.Contracts;
using Spx.Grains;
using Xunit;

namespace Spx.Grains.Tests;

public sealed class GameLobbyEventsGrainTests
{
    [Fact]
    public void NotifyObservers_RemovesObserversThatThrow()
    {
        var notifiedGameIds = new List<Guid>();
        var workingObserver = new DelegateGameLobbyObserver(gameId => notifiedGameIds.Add(gameId));
        var throwingObserver = new DelegateGameLobbyObserver(static _ => throw new InvalidOperationException("boom"));
        var observers = new HashSet<IGameLobbyObserver> { workingObserver, throwingObserver };
        var gameId = Guid.NewGuid();

        GameLobbyEventsGrain.NotifyObservers(gameId, observers);

        Assert.Equal([gameId], notifiedGameIds);
        Assert.Contains(workingObserver, observers);
        Assert.DoesNotContain(throwingObserver, observers);
    }

    private sealed class DelegateGameLobbyObserver(Action<Guid> onLobbyChanged) : IGameLobbyObserver
    {
        public void OnLobbyChanged(Guid gameId)
            => onLobbyChanged(gameId);
    }
}
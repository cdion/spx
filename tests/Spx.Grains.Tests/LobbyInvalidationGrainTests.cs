using Microsoft.Extensions.Logging.Abstractions;
using Spx.Contracts;
using Spx.Grains;
using Xunit;

namespace Spx.Grains.Tests;

public sealed class LobbyInvalidationGrainTests
{
    [Fact]
    public void NotifyLobbyObservers_LeavesObserversWhenNoneThrow()
    {
        var notifiedGameIds = new List<Guid>();
        var firstObserver = new DelegateGameInvalidationObserver(onLobbyInvalidated: gameId =>
            notifiedGameIds.Add(gameId)
        );
        var secondObserver = new DelegateGameInvalidationObserver(onLobbyInvalidated: gameId =>
            notifiedGameIds.Add(gameId)
        );
        var observers = new HashSet<ILobbyInvalidationObserver> { firstObserver, secondObserver };
        var gameId = Guid.NewGuid();

        LobbyInvalidationGrain.NotifyLobbyObservers(
            gameId,
            observers,
            NullLogger<LobbyInvalidationGrain>.Instance
        );

        Assert.Equal([gameId, gameId], notifiedGameIds);
        Assert.Contains(firstObserver, observers);
        Assert.Contains(secondObserver, observers);
    }

    [Fact]
    public void NotifyLobbyObservers_RemovesObserversThatThrow()
    {
        var notifiedGameIds = new List<Guid>();
        var workingObserver = new DelegateGameInvalidationObserver(onLobbyInvalidated: gameId =>
            notifiedGameIds.Add(gameId)
        );
        var throwingObserver = new DelegateGameInvalidationObserver(onLobbyInvalidated: static _ =>
            throw new InvalidOperationException("boom")
        );
        var observers = new HashSet<ILobbyInvalidationObserver>
        {
            workingObserver,
            throwingObserver,
        };
        var gameId = Guid.NewGuid();

        LobbyInvalidationGrain.NotifyLobbyObservers(
            gameId,
            observers,
            NullLogger<LobbyInvalidationGrain>.Instance
        );

        Assert.Equal([gameId], notifiedGameIds);
        Assert.Contains(workingObserver, observers);
        Assert.DoesNotContain(throwingObserver, observers);
    }

    [Fact]
    public void NotifyMessageObservers_LeavesObserversWhenNoneThrow()
    {
        var notifiedGameIds = new List<Guid>();
        var firstObserver = new DelegateGameInvalidationObserver(onMessagesInvalidated: gameId =>
            notifiedGameIds.Add(gameId)
        );
        var secondObserver = new DelegateGameInvalidationObserver(onMessagesInvalidated: gameId =>
            notifiedGameIds.Add(gameId)
        );
        var observers = new HashSet<ILobbyInvalidationObserver> { firstObserver, secondObserver };
        var gameId = Guid.NewGuid();

        LobbyInvalidationGrain.NotifyMessageObservers(
            gameId,
            observers,
            NullLogger<LobbyInvalidationGrain>.Instance
        );

        Assert.Equal([gameId, gameId], notifiedGameIds);
        Assert.Contains(firstObserver, observers);
        Assert.Contains(secondObserver, observers);
    }

    [Fact]
    public void NotifyMessageObservers_RemovesObserversThatThrow()
    {
        var notifiedGameIds = new List<Guid>();
        var workingObserver = new DelegateGameInvalidationObserver(onMessagesInvalidated: gameId =>
            notifiedGameIds.Add(gameId)
        );
        var throwingObserver = new DelegateGameInvalidationObserver(
            onMessagesInvalidated: static _ => throw new InvalidOperationException("boom")
        );
        var observers = new HashSet<ILobbyInvalidationObserver>
        {
            workingObserver,
            throwingObserver,
        };
        var gameId = Guid.NewGuid();

        LobbyInvalidationGrain.NotifyMessageObservers(
            gameId,
            observers,
            NullLogger<LobbyInvalidationGrain>.Instance
        );

        Assert.Equal([gameId], notifiedGameIds);
        Assert.Contains(workingObserver, observers);
        Assert.DoesNotContain(throwingObserver, observers);
    }

    [Fact]
    public void NotifySessionObservers_LeavesObserversWhenNoneThrow()
    {
        var notifiedGameIds = new List<Guid>();
        var firstObserver = new DelegateGameInvalidationObserver(onSessionInvalidated: gameId =>
            notifiedGameIds.Add(gameId)
        );
        var secondObserver = new DelegateGameInvalidationObserver(onSessionInvalidated: gameId =>
            notifiedGameIds.Add(gameId)
        );
        var observers = new HashSet<ILobbyInvalidationObserver> { firstObserver, secondObserver };
        var gameId = Guid.NewGuid();

        LobbyInvalidationGrain.NotifySessionObservers(
            gameId,
            observers,
            NullLogger<LobbyInvalidationGrain>.Instance
        );

        Assert.Equal([gameId, gameId], notifiedGameIds);
        Assert.Contains(firstObserver, observers);
        Assert.Contains(secondObserver, observers);
    }

    [Fact]
    public void NotifyPresenceObservers_LeavesObserversWhenNoneThrow()
    {
        var notifiedGameIds = new List<Guid>();
        var firstObserver = new DelegateGameInvalidationObserver(onPresenceInvalidated: gameId =>
            notifiedGameIds.Add(gameId)
        );
        var secondObserver = new DelegateGameInvalidationObserver(onPresenceInvalidated: gameId =>
            notifiedGameIds.Add(gameId)
        );
        var observers = new HashSet<ILobbyInvalidationObserver> { firstObserver, secondObserver };
        var gameId = Guid.NewGuid();

        LobbyInvalidationGrain.NotifyPresenceObservers(
            gameId,
            observers,
            NullLogger<LobbyInvalidationGrain>.Instance
        );

        Assert.Equal([gameId, gameId], notifiedGameIds);
        Assert.Contains(firstObserver, observers);
        Assert.Contains(secondObserver, observers);
    }

    private sealed class DelegateGameInvalidationObserver(
        Action<Guid>? onLobbyInvalidated = null,
        Action<Guid>? onSessionInvalidated = null,
        Action<Guid>? onMessagesInvalidated = null,
        Action<Guid>? onPresenceInvalidated = null
    ) : ILobbyInvalidationObserver
    {
        public void OnLobbyInvalidated(Guid gameId) => onLobbyInvalidated?.Invoke(gameId);

        public void OnSessionInvalidated(Guid gameId) => onSessionInvalidated?.Invoke(gameId);

        public void OnMessagesInvalidated(Guid gameId) => onMessagesInvalidated?.Invoke(gameId);

        public void OnPresenceInvalidated(Guid gameId) => onPresenceInvalidated?.Invoke(gameId);
    }
}

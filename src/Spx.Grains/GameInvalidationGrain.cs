using Orleans;
using Spx.Contracts;

namespace Spx.Grains;

public sealed class GameInvalidationGrain : Grain, IGameInvalidationGrain
{
    private readonly HashSet<IGameInvalidationObserver> observers = [];

    public Task Subscribe(IGameInvalidationObserver observer)
    {
        observers.Add(observer);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(IGameInvalidationObserver observer)
    {
        observers.Remove(observer);
        return Task.CompletedTask;
    }

    public Task PublishLobbyInvalidated()
    {
        NotifyLobbyObservers(this.GetPrimaryKey(), observers);
        return Task.CompletedTask;
    }

    public Task PublishSessionInvalidated()
    {
        NotifySessionObservers(this.GetPrimaryKey(), observers);
        return Task.CompletedTask;
    }

    public Task PublishMessagesInvalidated()
    {
        NotifyMessageObservers(this.GetPrimaryKey(), observers);
        return Task.CompletedTask;
    }

    public Task PublishPresenceInvalidated()
    {
        NotifyPresenceObservers(this.GetPrimaryKey(), observers);
        return Task.CompletedTask;
    }

    internal static void NotifyLobbyObservers(Guid gameId, ICollection<IGameInvalidationObserver> observers)
        => NotifyObservers(gameId, observers, static (observer, targetGameId) => observer.OnLobbyInvalidated(targetGameId));

    internal static void NotifySessionObservers(Guid gameId, ICollection<IGameInvalidationObserver> observers)
        => NotifyObservers(gameId, observers, static (observer, targetGameId) => observer.OnSessionInvalidated(targetGameId));

    internal static void NotifyMessageObservers(Guid gameId, ICollection<IGameInvalidationObserver> observers)
        => NotifyObservers(gameId, observers, static (observer, targetGameId) => observer.OnMessagesInvalidated(targetGameId));

    internal static void NotifyPresenceObservers(Guid gameId, ICollection<IGameInvalidationObserver> observers)
        => NotifyObservers(gameId, observers, static (observer, targetGameId) => observer.OnPresenceInvalidated(targetGameId));

    private static void NotifyObservers(Guid gameId, ICollection<IGameInvalidationObserver> observers, Action<IGameInvalidationObserver, Guid> notifyObserver)
    {
        List<IGameInvalidationObserver>? disconnectedObservers = null;

        foreach (var observer in observers)
        {
            try
            {
                notifyObserver(observer, gameId);
            }
            catch
            {
                disconnectedObservers ??= [];
                disconnectedObservers.Add(observer);
            }
        }

        if (disconnectedObservers is null)
        {
            return;
        }

        foreach (var observer in disconnectedObservers)
        {
            observers.Remove(observer);
        }
    }
}
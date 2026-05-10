using Orleans;
using Spx.Contracts;

namespace Spx.Grains;

public sealed class GameLobbyEventsGrain : Grain, IGameLobbyEventsGrain
{
    private readonly HashSet<IGameLobbyObserver> observers = [];

    public Task Subscribe(IGameLobbyObserver observer)
    {
        observers.Add(observer);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(IGameLobbyObserver observer)
    {
        observers.Remove(observer);
        return Task.CompletedTask;
    }

    public Task PublishLobbyChanged()
    {
        NotifyObservers(this.GetPrimaryKey(), observers);
        return Task.CompletedTask;
    }

    public Task PublishMessagesChanged()
    {
        NotifyMessageObservers(this.GetPrimaryKey(), observers);
        return Task.CompletedTask;
    }

    internal static void NotifyObservers(Guid gameId, ICollection<IGameLobbyObserver> observers)
        => NotifyObservers(gameId, observers, static (observer, targetGameId) => observer.OnLobbyChanged(targetGameId));

    internal static void NotifyMessageObservers(Guid gameId, ICollection<IGameLobbyObserver> observers)
        => NotifyObservers(gameId, observers, static (observer, targetGameId) => observer.OnMessagesChanged(targetGameId));

    private static void NotifyObservers(Guid gameId, ICollection<IGameLobbyObserver> observers, Action<IGameLobbyObserver, Guid> notifyObserver)
    {
        List<IGameLobbyObserver>? disconnectedObservers = null;

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
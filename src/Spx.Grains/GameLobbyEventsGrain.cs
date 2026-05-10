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

    internal static void NotifyObservers(Guid gameId, ICollection<IGameLobbyObserver> observers)
    {
        List<IGameLobbyObserver>? disconnectedObservers = null;

        foreach (var observer in observers)
        {
            try
            {
                observer.OnLobbyChanged(gameId);
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
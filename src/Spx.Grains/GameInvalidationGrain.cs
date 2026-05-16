using Microsoft.Extensions.Logging;
using Orleans;
using Spx.Contracts;

namespace Spx.Grains;

public sealed class GameInvalidationGrain(ILogger<GameInvalidationGrain> logger) : Grain, IGameInvalidationGrain
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
        NotifyLobbyObservers(this.GetPrimaryKey(), observers, logger);
        return Task.CompletedTask;
    }

    public Task PublishSessionInvalidated()
    {
        NotifySessionObservers(this.GetPrimaryKey(), observers, logger);
        return Task.CompletedTask;
    }

    public Task PublishMessagesInvalidated()
    {
        NotifyMessageObservers(this.GetPrimaryKey(), observers, logger);
        return Task.CompletedTask;
    }

    public Task PublishPresenceInvalidated()
    {
        NotifyPresenceObservers(this.GetPrimaryKey(), observers, logger);
        return Task.CompletedTask;
    }

    internal static void NotifyLobbyObservers(Guid gameId, ICollection<IGameInvalidationObserver> observers, ILogger<GameInvalidationGrain>? logger = null)
        => NotifyObservers(gameId, observers, static (observer, targetGameId) => observer.OnLobbyInvalidated(targetGameId), logger);

    internal static void NotifySessionObservers(Guid gameId, ICollection<IGameInvalidationObserver> observers, ILogger<GameInvalidationGrain>? logger = null)
        => NotifyObservers(gameId, observers, static (observer, targetGameId) => observer.OnSessionInvalidated(targetGameId), logger);

    internal static void NotifyMessageObservers(Guid gameId, ICollection<IGameInvalidationObserver> observers, ILogger<GameInvalidationGrain>? logger = null)
        => NotifyObservers(gameId, observers, static (observer, targetGameId) => observer.OnMessagesInvalidated(targetGameId), logger);

    internal static void NotifyPresenceObservers(Guid gameId, ICollection<IGameInvalidationObserver> observers, ILogger<GameInvalidationGrain>? logger = null)
        => NotifyObservers(gameId, observers, static (observer, targetGameId) => observer.OnPresenceInvalidated(targetGameId), logger);

    private static void NotifyObservers(
        Guid gameId,
        ICollection<IGameInvalidationObserver> observers,
        Action<IGameInvalidationObserver, Guid> notifyObserver,
        ILogger<GameInvalidationGrain>? logger)
    {
        List<IGameInvalidationObserver>? disconnectedObservers = null;

        foreach (var observer in observers)
        {
            try
            {
                notifyObserver(observer, gameId);
            }
            catch (Exception exception)
            {
                logger?.LogDebug(exception, "Removing a disconnected game invalidation observer for game {GameId}.", gameId);
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
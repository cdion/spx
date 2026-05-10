using Orleans;
using Spx.Contracts;

namespace Spx.Web.Services;

public sealed class OrleansGameLobbyNotifier(IClusterClient clusterClient, ILogger<OrleansGameLobbyNotifier> logger)
    : IGameLobbyNotifier, IGameLobbyObserver
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly Dictionary<Guid, Dictionary<Guid, Func<Task>>> subscriptions = [];
    private IGameLobbyObserver? observerReference;

    public async Task PublishLobbyChangedAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        try
        {
            await clusterClient.GetGrain<IGameLobbyEventsGrain>(gameId).PublishLobbyChanged();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to publish lobby update for game {GameId}.", gameId);
        }
    }

    public async ValueTask<IAsyncDisposable> SubscribeAsync(Guid gameId, Func<Task> onLobbyChanged, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onLobbyChanged);

        var subscriptionId = Guid.NewGuid();

        await gate.WaitAsync(cancellationToken);
        try
        {
            var isFirstSubscription = !subscriptions.TryGetValue(gameId, out var callbacks);
            callbacks ??= [];
            callbacks[subscriptionId] = onLobbyChanged;
            subscriptions[gameId] = callbacks;

            if (isFirstSubscription)
            {
                var observer = await GetObserverReferenceAsync(cancellationToken);

                try
                {
                    await clusterClient.GetGrain<IGameLobbyEventsGrain>(gameId).Subscribe(observer);
                }
                catch
                {
                    callbacks.Remove(subscriptionId);

                    if (callbacks.Count == 0)
                    {
                        subscriptions.Remove(gameId);
                    }

                    throw;
                }
            }
        }
        finally
        {
            gate.Release();
        }

        return new LobbySubscription(this, gameId, subscriptionId);
    }

    public void OnLobbyChanged(Guid gameId)
    {
        _ = DispatchAsync(gameId);
    }

    private async Task<IGameLobbyObserver> GetObserverReferenceAsync(CancellationToken cancellationToken)
    {
        if (observerReference is not null)
        {
            return observerReference;
        }

        observerReference = clusterClient.CreateObjectReference<IGameLobbyObserver>(this);
        return observerReference;
    }

    private async Task DispatchAsync(Guid gameId)
    {
        List<Func<Task>> callbacks;

        await gate.WaitAsync();
        try
        {
            if (!subscriptions.TryGetValue(gameId, out var registeredCallbacks))
            {
                return;
            }

            callbacks = [.. registeredCallbacks.Values];
        }
        finally
        {
            gate.Release();
        }

        foreach (var callback in callbacks)
        {
            try
            {
                await callback();
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "A lobby update callback failed for game {GameId}.", gameId);
            }
        }
    }

    private async Task UnsubscribeAsync(Guid gameId, Guid subscriptionId)
    {
        await gate.WaitAsync();
        try
        {
            if (!subscriptions.TryGetValue(gameId, out var callbacks))
            {
                return;
            }

            callbacks.Remove(subscriptionId);

            if (callbacks.Count > 0)
            {
                return;
            }

            subscriptions.Remove(gameId);

            if (observerReference is not null)
            {
                await clusterClient.GetGrain<IGameLobbyEventsGrain>(gameId).Unsubscribe(observerReference);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private sealed class LobbySubscription(OrleansGameLobbyNotifier owner, Guid gameId, Guid subscriptionId) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
            => new(owner.UnsubscribeAsync(gameId, subscriptionId));
    }
}
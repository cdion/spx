using Orleans;
using Spx.Contracts;
using Spx.Games;

namespace Spx.Web.Adapters.Games;

public sealed class OrleansGameLobbyAdapter(
    IClusterClient clusterClient,
    ILogger<OrleansGameLobbyAdapter> logger)
    : IGameLobbySubscriptionService, IGameMessageSubscriptionService, IGameLobbyEventsPublisher, IGameMessageEventsPublisher, IGameLobbyObserver
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly Dictionary<Guid, Dictionary<Guid, Func<Task>>> lobbySubscriptions = [];
    private readonly Dictionary<Guid, Dictionary<Guid, Func<Task>>> messageSubscriptions = [];
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

    public async Task PublishMessagesChangedAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        try
        {
            await clusterClient.GetGrain<IGameLobbyEventsGrain>(gameId).PublishMessagesChanged();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to publish message update for game {GameId}.", gameId);
        }
    }

    public async ValueTask<IAsyncDisposable> SubscribeAsync(Guid gameId, Func<Task> onLobbyChanged, CancellationToken cancellationToken = default)
        => await SubscribeCoreAsync(gameId, onLobbyChanged, SubscriptionKind.Lobby, cancellationToken);

    public async ValueTask<IAsyncDisposable> SubscribeToMessagesAsync(Guid gameId, Func<Task> onMessagesChanged, CancellationToken cancellationToken = default)
        => await SubscribeCoreAsync(gameId, onMessagesChanged, SubscriptionKind.Messages, cancellationToken);

    public void OnLobbyChanged(Guid gameId)
    {
        _ = DispatchAsync(gameId, SubscriptionKind.Lobby);
    }

    public void OnMessagesChanged(Guid gameId)
    {
        _ = DispatchAsync(gameId, SubscriptionKind.Messages);
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

    private async ValueTask<IAsyncDisposable> SubscribeCoreAsync(Guid gameId, Func<Task> callback, SubscriptionKind kind, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(callback);

        var subscriptionId = Guid.NewGuid();

        await gate.WaitAsync(cancellationToken);
        try
        {
            var callbacks = GetSubscriptions(kind);
            var hadAnySubscriptions = HasAnySubscriptions(gameId);

            if (!callbacks.TryGetValue(gameId, out var registeredCallbacks))
            {
                registeredCallbacks = [];
            }

            registeredCallbacks[subscriptionId] = callback;
            callbacks[gameId] = registeredCallbacks;

            if (!hadAnySubscriptions)
            {
                var observer = await GetObserverReferenceAsync(cancellationToken);

                try
                {
                    await clusterClient.GetGrain<IGameLobbyEventsGrain>(gameId).Subscribe(observer);
                }
                catch
                {
                    registeredCallbacks.Remove(subscriptionId);

                    if (registeredCallbacks.Count == 0)
                    {
                        callbacks.Remove(gameId);
                    }

                    throw;
                }
            }
        }
        finally
        {
            gate.Release();
        }

        return new Subscription(this, gameId, subscriptionId, kind);
    }

    private async Task DispatchAsync(Guid gameId, SubscriptionKind kind)
    {
        List<Func<Task>> callbacks;

        await gate.WaitAsync();
        try
        {
            if (!GetSubscriptions(kind).TryGetValue(gameId, out var registeredCallbacks))
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
                logger.LogWarning(exception, "A {SubscriptionKind} update callback failed for game {GameId}.", kind, gameId);
            }
        }
    }

    private async Task UnsubscribeAsync(Guid gameId, Guid subscriptionId, SubscriptionKind kind)
    {
        await gate.WaitAsync();
        try
        {
            var subscriptions = GetSubscriptions(kind);
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

            if (observerReference is not null && !HasAnySubscriptions(gameId))
            {
                await clusterClient.GetGrain<IGameLobbyEventsGrain>(gameId).Unsubscribe(observerReference);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private bool HasAnySubscriptions(Guid gameId)
        => (lobbySubscriptions.TryGetValue(gameId, out var lobbyCallbacks) && lobbyCallbacks.Count > 0)
            || (messageSubscriptions.TryGetValue(gameId, out var messageCallbacks) && messageCallbacks.Count > 0);

    private Dictionary<Guid, Dictionary<Guid, Func<Task>>> GetSubscriptions(SubscriptionKind kind)
        => kind == SubscriptionKind.Lobby ? lobbySubscriptions : messageSubscriptions;

    private enum SubscriptionKind
    {
        Lobby,
        Messages
    }

    private sealed class Subscription(OrleansGameLobbyAdapter owner, Guid gameId, Guid subscriptionId, SubscriptionKind kind) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
            => new(owner.UnsubscribeAsync(gameId, subscriptionId, kind));
    }
}
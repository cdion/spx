using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Orleans;
using Spx.Contracts;

namespace Spx.Web.Hubs;

public interface IGameInvalidationHubBridge
{
    Task OnGameConnectedAsync(Guid gameId);
    Task OnGameDisconnectedAsync(Guid gameId);
}

public interface IGameInvalidationSubscriber
{
    Task OnGameStateChangedAsync(Guid gameId);
    Task OnMessagesChangedAsync(Guid gameId);
    Task OnPresenceChangedAsync(Guid gameId);
}

public interface IGameInvalidationNotifier
{
    Task SubscribeAsync(Guid gameId, IGameInvalidationSubscriber subscriber);
    Task UnsubscribeAsync(Guid gameId, IGameInvalidationSubscriber subscriber);
    Task NotifyPresenceChangedAsync(Guid gameId);
}

public sealed class NexusInvalidationHubBridge(
    IHubContext<NexusHub> hubContext,
    IClusterClient clusterClient
)
    : ILobbyInvalidationObserver,
        IGameInvalidationHubBridge,
        IGameInvalidationNotifier,
        IHostedService
{
    private readonly ConcurrentDictionary<Guid, int> listenerCounts = new();
    private readonly ConcurrentDictionary<
        Guid,
        ConcurrentDictionary<IGameInvalidationSubscriber, byte>
    > subscribers = new();
    private ILobbyInvalidationObserver? observerRef;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        observerRef = clusterClient.CreateObjectReference<ILobbyInvalidationObserver>(this);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task OnGameConnectedAsync(Guid gameId)
    {
        await AddListenerAsync(gameId);
    }

    public async Task OnGameDisconnectedAsync(Guid gameId)
    {
        await RemoveListenerAsync(gameId);
    }

    public async Task SubscribeAsync(Guid gameId, IGameInvalidationSubscriber subscriber)
    {
        var gameSubscribers = subscribers.GetOrAdd(gameId, _ => []);
        if (!gameSubscribers.TryAdd(subscriber, 0))
        {
            return;
        }

        try
        {
            await AddListenerAsync(gameId);
        }
        catch
        {
            gameSubscribers.TryRemove(subscriber, out _);
            if (gameSubscribers.IsEmpty)
            {
                subscribers.TryRemove(gameId, out _);
            }

            throw;
        }
    }

    public async Task UnsubscribeAsync(Guid gameId, IGameInvalidationSubscriber subscriber)
    {
        if (!subscribers.TryGetValue(gameId, out var gameSubscribers))
        {
            return;
        }

        if (!gameSubscribers.TryRemove(subscriber, out _))
        {
            return;
        }

        if (gameSubscribers.IsEmpty)
        {
            subscribers.TryRemove(gameId, out _);
        }

        await RemoveListenerAsync(gameId);
    }

    public Task NotifyPresenceChangedAsync(Guid gameId) => NotifyPresenceChangedCoreAsync(gameId);

    public void OnLobbyInvalidated(Guid gameId) => _ = NotifyGameStateChangedAsync(gameId);

    public void OnSessionInvalidated(Guid gameId) => _ = NotifyGameStateChangedAsync(gameId);

    public void OnMessagesInvalidated(Guid gameId) => _ = NotifyMessagesChangedAsync(gameId);

    private async Task NotifyGameStateChangedAsync(Guid gameId)
    {
        await hubContext.Clients.Group($"game:{gameId}").SendAsync("GameStateChanged", gameId);
        await NotifySubscribersAsync(
            gameId,
            static (subscriber, id) => subscriber.OnGameStateChangedAsync(id)
        );
    }

    private async Task NotifyMessagesChangedAsync(Guid gameId)
    {
        await hubContext.Clients.Group($"game:{gameId}").SendAsync("MessagesChanged", gameId);
        await NotifySubscribersAsync(
            gameId,
            static (subscriber, id) => subscriber.OnMessagesChangedAsync(id)
        );
    }

    private async Task NotifyPresenceChangedCoreAsync(Guid gameId)
    {
        await hubContext.Clients.Group($"game:{gameId}").SendAsync("PresenceChanged", gameId);
        await NotifySubscribersAsync(
            gameId,
            static (subscriber, id) => subscriber.OnPresenceChangedAsync(id)
        );
    }

    private Task NotifySubscribersAsync(
        Guid gameId,
        Func<IGameInvalidationSubscriber, Guid, Task> notifyAsync
    )
    {
        if (!subscribers.TryGetValue(gameId, out var gameSubscribers) || gameSubscribers.IsEmpty)
        {
            return Task.CompletedTask;
        }

        return Task.WhenAll(
            gameSubscribers.Keys.Select(subscriber => notifyAsync(subscriber, gameId))
        );
    }

    private async Task AddListenerAsync(Guid gameId)
    {
        var newCount = listenerCounts.AddOrUpdate(gameId, 1, (_, count) => count + 1);
        if (newCount == 1)
        {
            await clusterClient
                .GetGrain<ILobbyInvalidationGrain>(gameId)
                .Subscribe(GetObserverRef());
        }
    }

    private async Task RemoveListenerAsync(Guid gameId)
    {
        var newCount = listenerCounts.AddOrUpdate(gameId, 0, (_, count) => Math.Max(0, count - 1));
        if (newCount == 0)
        {
            listenerCounts.TryRemove(gameId, out _);
            await clusterClient
                .GetGrain<ILobbyInvalidationGrain>(gameId)
                .Unsubscribe(GetObserverRef());
        }
    }

    private ILobbyInvalidationObserver GetObserverRef() =>
        observerRef ??= clusterClient.CreateObjectReference<ILobbyInvalidationObserver>(this);
}

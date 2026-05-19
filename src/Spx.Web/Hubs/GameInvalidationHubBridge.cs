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

public sealed class GameInvalidationHubBridge(
    IHubContext<GameHub> hubContext,
    IClusterClient clusterClient
) : IGameInvalidationObserver, IGameInvalidationHubBridge, IHostedService
{
    private readonly ConcurrentDictionary<Guid, int> connectionCounts = new();
    private IGameInvalidationObserver? observerRef;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        observerRef = clusterClient.CreateObjectReference<IGameInvalidationObserver>(this);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task OnGameConnectedAsync(Guid gameId)
    {
        var newCount = connectionCounts.AddOrUpdate(gameId, 1, (_, c) => c + 1);
        if (newCount == 1)
        {
            await clusterClient.GetGrain<IGameInvalidationGrain>(gameId).Subscribe(observerRef!);
        }
    }

    public async Task OnGameDisconnectedAsync(Guid gameId)
    {
        var newCount = connectionCounts.AddOrUpdate(gameId, 0, (_, c) => Math.Max(0, c - 1));
        if (newCount == 0)
        {
            connectionCounts.TryRemove(gameId, out _);
            await clusterClient.GetGrain<IGameInvalidationGrain>(gameId).Unsubscribe(observerRef!);
        }
    }

    public void OnLobbyInvalidated(Guid gameId) =>
        hubContext.Clients.Group($"game:{gameId}").SendAsync("GameStateChanged", gameId);

    public void OnSessionInvalidated(Guid gameId) =>
        hubContext.Clients.Group($"game:{gameId}").SendAsync("GameStateChanged", gameId);

    public void OnMessagesInvalidated(Guid gameId) =>
        hubContext.Clients.Group($"game:{gameId}").SendAsync("MessagesChanged", gameId);
}

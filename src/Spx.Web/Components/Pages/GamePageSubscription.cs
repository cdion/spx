using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Spx.Contracts;

namespace Spx.Web.Components.Pages;

internal sealed class GamePageSubscription(
    IClusterClient clusterClient,
    ILogger<GamePageSubscription> logger,
    Guid gameId,
    Func<Task> onLobbyInvalidated,
    Func<Task> onSessionInvalidated,
    Func<Task> onMessagesInvalidated,
    Func<Task> onPresenceInvalidated)
    : IGameInvalidationObserver, IAsyncDisposable
{
    private IGameInvalidationObserver? observerReference;
    private bool isSubscribed;

    public Guid GameId { get; } = gameId;

    public async Task SubscribeAsync(CancellationToken cancellationToken = default)
    {
        if (isSubscribed)
        {
            return;
        }

        observerReference = clusterClient.CreateObjectReference<IGameInvalidationObserver>(this);

        try
        {
            await clusterClient.GetGrain<IGameInvalidationGrain>(GameId).Subscribe(observerReference);
            isSubscribed = true;
        }
        catch (OrleansException exception)
        {
            logger.LogWarning(exception, "Failed to subscribe to live game invalidation events for game {GameId}.", GameId);
            throw;
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(exception, "Failed to subscribe to live game invalidation events for game {GameId}.", GameId);
            throw;
        }
        finally
        {
            if (!isSubscribed && observerReference is not null)
            {
                clusterClient.DeleteObjectReference<IGameInvalidationObserver>(observerReference);
                observerReference = null;
            }
        }
    }

    public void OnLobbyInvalidated(Guid changedGameId)
    {
        if (changedGameId != GameId || !isSubscribed)
        {
            return;
        }

        _ = onLobbyInvalidated();
    }

    public void OnSessionInvalidated(Guid changedGameId)
    {
        if (changedGameId != GameId || !isSubscribed)
        {
            return;
        }

        _ = onSessionInvalidated();
    }

    public void OnMessagesInvalidated(Guid changedGameId)
    {
        if (changedGameId != GameId || !isSubscribed)
        {
            return;
        }

        _ = onMessagesInvalidated();
    }

    public void OnPresenceInvalidated(Guid changedGameId)
    {
        if (changedGameId != GameId || !isSubscribed)
        {
            return;
        }

        _ = onPresenceInvalidated();
    }

    public async ValueTask DisposeAsync()
    {
        if (!isSubscribed || observerReference is null)
        {
            return;
        }

        try
        {
            await clusterClient.GetGrain<IGameInvalidationGrain>(GameId).Unsubscribe(observerReference);
        }
        finally
        {
            clusterClient.DeleteObjectReference<IGameInvalidationObserver>(observerReference);
            observerReference = null;
            isSubscribed = false;
        }
    }
}
using Orleans;
using Spx.Contracts;

namespace Spx.Web.Components.Pages;

internal sealed class GamePageSubscription(
    IClusterClient clusterClient,
    Guid gameId,
    Func<Task> onLobbyChanged,
    Func<Task> onMessagesChanged)
    : IGameLobbyObserver, IAsyncDisposable
{
    private IGameLobbyObserver? observerReference;
    private bool isSubscribed;

    public Guid GameId { get; } = gameId;

    public async Task SubscribeAsync(CancellationToken cancellationToken = default)
    {
        if (isSubscribed)
        {
            return;
        }

        observerReference = clusterClient.CreateObjectReference<IGameLobbyObserver>(this);

        try
        {
            await clusterClient.GetGrain<IGameLobbyEventsGrain>(GameId).Subscribe(observerReference);
            isSubscribed = true;
        }
        catch
        {
            clusterClient.DeleteObjectReference<IGameLobbyObserver>(observerReference);
            observerReference = null;
            throw;
        }
    }

    public void OnLobbyChanged(Guid changedGameId)
    {
        if (changedGameId != GameId || !isSubscribed)
        {
            return;
        }

        _ = onLobbyChanged();
    }

    public void OnMessagesChanged(Guid changedGameId)
    {
        if (changedGameId != GameId || !isSubscribed)
        {
            return;
        }

        _ = onMessagesChanged();
    }

    public async ValueTask DisposeAsync()
    {
        if (!isSubscribed || observerReference is null)
        {
            return;
        }

        try
        {
            await clusterClient.GetGrain<IGameLobbyEventsGrain>(GameId).Unsubscribe(observerReference);
        }
        finally
        {
            clusterClient.DeleteObjectReference<IGameLobbyObserver>(observerReference);
            observerReference = null;
            isSubscribed = false;
        }
    }
}
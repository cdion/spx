namespace Spx.Web.Services;

public interface IGameLobbySubscriptionService
{
    ValueTask<IAsyncDisposable> SubscribeAsync(Guid gameId, Func<Task> onLobbyChanged, CancellationToken cancellationToken = default);
}
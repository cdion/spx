namespace Spx.Web.Adapters.Games;

public interface IGameLobbySubscriptionService
{
    ValueTask<IAsyncDisposable> SubscribeAsync(Guid gameId, Func<Task> onLobbyChanged, CancellationToken cancellationToken = default);
}
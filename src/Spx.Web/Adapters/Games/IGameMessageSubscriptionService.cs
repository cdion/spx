namespace Spx.Web.Adapters.Games;

public interface IGameMessageSubscriptionService
{
    ValueTask<IAsyncDisposable> SubscribeToMessagesAsync(Guid gameId, Func<Task> onMessagesChanged, CancellationToken cancellationToken = default);
}
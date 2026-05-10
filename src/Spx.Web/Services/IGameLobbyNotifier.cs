namespace Spx.Web.Services;

public interface IGameLobbyNotifier
{
    Task PublishLobbyChangedAsync(Guid gameId, CancellationToken cancellationToken = default);

    ValueTask<IAsyncDisposable> SubscribeAsync(Guid gameId, Func<Task> onLobbyChanged, CancellationToken cancellationToken = default);
}
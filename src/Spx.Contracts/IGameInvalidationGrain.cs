using Orleans;

namespace Spx.Contracts;

public interface IGameInvalidationGrain : IGrainWithGuidKey
{
    Task Subscribe(IGameInvalidationObserver observer);

    Task Unsubscribe(IGameInvalidationObserver observer);

    Task PublishLobbyInvalidated();

    Task PublishSessionInvalidated();

    Task PublishMessagesInvalidated();
}

using Orleans;

namespace Spx.Contracts;

public interface ILobbyInvalidationGrain : IGrainWithGuidKey
{
    Task Subscribe(ILobbyInvalidationObserver observer);

    Task Unsubscribe(ILobbyInvalidationObserver observer);

    Task PublishLobbyInvalidated();

    Task PublishSessionInvalidated();

    Task PublishMessagesInvalidated();

    Task PublishPresenceInvalidated();
}

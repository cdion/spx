using Orleans;

namespace Spx.Contracts;

public interface IGameLobbyEventsGrain : IGrainWithGuidKey
{
    Task Subscribe(IGameLobbyObserver observer);

    Task Unsubscribe(IGameLobbyObserver observer);

    Task PublishLobbyChanged();
}
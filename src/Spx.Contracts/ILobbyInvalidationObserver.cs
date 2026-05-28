using Orleans;

namespace Spx.Contracts;

public interface ILobbyInvalidationObserver : IGrainObserver
{
    void OnLobbyInvalidated(Guid gameId);

    void OnSessionInvalidated(Guid gameId);

    void OnMessagesInvalidated(Guid gameId);
}

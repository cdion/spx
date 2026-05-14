using Orleans;

namespace Spx.Contracts;

public interface IGameInvalidationObserver : IGrainObserver
{
    void OnLobbyInvalidated(Guid gameId);

    void OnSessionInvalidated(Guid gameId);

    void OnMessagesInvalidated(Guid gameId);

    void OnPresenceInvalidated(Guid gameId);
}
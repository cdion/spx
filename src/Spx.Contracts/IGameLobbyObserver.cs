using Orleans;

namespace Spx.Contracts;

public interface IGameLobbyObserver : IGrainObserver
{
    void OnLobbyChanged(Guid gameId);
}
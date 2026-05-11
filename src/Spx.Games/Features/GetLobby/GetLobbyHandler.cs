namespace Spx.Games.Features.GetLobby;

internal sealed class GetLobbyHandler(IGamePersistence gamePersistence) : IGetLobbyHandler
{
    public async Task<GameLobbyView?> HandleAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
        => await gamePersistence.GetLobbyAsync(gameId, userId, cancellationToken);
}
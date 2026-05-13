namespace Spx.Game.Application.Features.GetGameLobby;

internal sealed class GetGameLobbyHandler(IGamePersistence gamePersistence) : IGetGameLobbyHandler
{
    public async Task<GameLobbyView?> HandleAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
        => await gamePersistence.GetLobbyAsync(gameId, userId, cancellationToken);
}
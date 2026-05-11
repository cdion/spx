namespace Spx.Games.Features.GetUserGames;

internal sealed class GetUserGamesHandler(IGamePersistence gamePersistence) : IGetUserGamesHandler
{
    public async Task<UserGamesView> HandleAsync(string userId, CancellationToken cancellationToken = default)
        => await gamePersistence.GetUserGamesAsync(userId, cancellationToken);
}
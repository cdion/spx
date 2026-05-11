namespace Spx.Games.Features.GetUserGames;

public interface IGetUserGamesHandler
{
    Task<UserGamesView> HandleAsync(string userId, CancellationToken cancellationToken = default);
}
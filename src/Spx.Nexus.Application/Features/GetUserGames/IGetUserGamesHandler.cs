namespace Spx.Nexus.Application.Features.GetUserGames;

public interface IGetUserGamesHandler
{
    Task<UserGamesView> HandleAsync(string userId, CancellationToken cancellationToken = default);
}

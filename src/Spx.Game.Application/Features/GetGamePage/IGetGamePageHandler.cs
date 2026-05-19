namespace Spx.Game.Application.Features.GetGamePage;

public interface IGetGamePageHandler
{
    Task<GamePageView?> HandleAsync(
        Guid gameId,
        string userId,
        CancellationToken cancellationToken = default
    );
}

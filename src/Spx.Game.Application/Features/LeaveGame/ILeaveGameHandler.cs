namespace Spx.Game.Application.Features.LeaveGame;

public interface ILeaveGameHandler
{
    Task<GameCommandOutcome> HandleAsync(
        Guid gameId,
        string userId,
        CancellationToken cancellationToken = default
    );
}

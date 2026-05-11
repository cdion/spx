namespace Spx.Games.Features.LeaveGame;

public interface ILeaveGameHandler
{
    Task<GameCommandResult> HandleAsync(Guid gameId, string userId, CancellationToken cancellationToken = default);
}
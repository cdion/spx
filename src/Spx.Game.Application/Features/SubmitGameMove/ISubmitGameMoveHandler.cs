using Spx.Contracts;

namespace Spx.Game.Application.Features.SubmitGameMove;

public interface ISubmitGameMoveHandler
{
    Task<SubmitGameMoveOutcome> HandleAsync(
        Guid gameId,
        string userId,
        int expectedRoundNumber,
        GameMove move,
        CancellationToken cancellationToken = default);
}
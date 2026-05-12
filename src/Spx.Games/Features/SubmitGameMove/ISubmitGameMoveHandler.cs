using Spx.Contracts;

namespace Spx.Games.Features.SubmitGameMove;

public interface ISubmitGameMoveHandler
{
    Task<SubmitGameMoveResult> HandleAsync(
        Guid gameId,
        string userId,
        int expectedRoundNumber,
        GameMove move,
        CancellationToken cancellationToken = default);
}
using Spx.Contracts;

namespace Spx.Game.Application.Features.SubmitAcquireCard;

public interface ISubmitAcquireCardHandler
{
    Task<GameSessionCommandOutcome> HandleAsync(
        Guid gameId,
        string userId,
        int expectedRoundNumber,
        Guid marketCardInstanceId,
        CancellationToken cancellationToken = default);
}

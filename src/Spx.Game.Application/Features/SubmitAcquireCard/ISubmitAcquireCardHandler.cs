namespace Spx.Game.Application.Features.SubmitAcquireCard;

public interface ISubmitAcquireCardHandler
{
    Task<GameSessionCommandOutcome> HandleAsync(
        Guid gameId,
        Guid playerId,
        int expectedRoundNumber,
        Guid marketCardInstanceId,
        CancellationToken cancellationToken = default);
}

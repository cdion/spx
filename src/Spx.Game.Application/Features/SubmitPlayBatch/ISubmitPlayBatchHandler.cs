namespace Spx.Game.Application.Features.SubmitPlayBatch;

public interface ISubmitPlayBatchHandler
{
    Task<GameSessionCommandOutcome> HandleAsync(
        Guid gameId,
        Guid playerId,
        int expectedRoundNumber,
    IReadOnlyList<GameBatchCardSelection> cards,
        CancellationToken cancellationToken = default);
}

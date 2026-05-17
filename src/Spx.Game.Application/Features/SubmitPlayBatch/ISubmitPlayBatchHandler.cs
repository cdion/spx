namespace Spx.Game.Application.Features.SubmitPlayBatch;

public interface ISubmitPlayBatchHandler
{
    Task<GameSessionCommandOutcome> HandleAsync(
        Guid gameId,
        string userId,
        int expectedRoundNumber,
    IReadOnlyList<GameBatchCardSelection> cards,
        CancellationToken cancellationToken = default);
}

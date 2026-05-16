using Spx.Contracts;

namespace Spx.Game.Application.Features.SubmitPlayBatch;

public interface ISubmitPlayBatchHandler
{
    Task<GameSessionCommandOutcome> HandleAsync(
        Guid gameId,
        string userId,
        int expectedRoundNumber,
        IReadOnlyList<GameBatchCardCommand> cards,
        CancellationToken cancellationToken = default);
}

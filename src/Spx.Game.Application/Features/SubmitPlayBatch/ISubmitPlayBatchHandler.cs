namespace Spx.Game.Application.Features.SubmitPlayBatch;

public interface ISubmitPlayBatchHandler
{
    Task<GameSessionCommandOutcome> HandleAsync(
        Guid gameId,
        Guid playerId,
        int expectedRoundNumber,
        IReadOnlyList<GameBatchCardCommand> cards,
        CancellationToken cancellationToken = default
    );
}

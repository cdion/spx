namespace Spx.Game.Application;

public interface IGameSessionService
{
    Task<bool> EnsureSessionAsync(
        Guid gameId,
        IReadOnlyList<GameSessionParticipant> players,
        CancellationToken cancellationToken = default
    );

    Task<GameSessionView?> GetSessionAsync(
        Guid gameId,
        Guid playerId,
        CancellationToken cancellationToken = default
    );

    Task<GameSessionCommandOutcome> SubmitAcquireAsync(
        Guid gameId,
        SubmitAcquireCommand command,
        CancellationToken cancellationToken = default
    );

    Task<GameSessionCommandOutcome> SubmitPlayBatchAsync(
        Guid gameId,
        SubmitPlayBatchCommand command,
        CancellationToken cancellationToken = default
    );

    Task AcknowledgeGameplayEventBatchAsync(
        Guid gameId,
        Guid gameplayEventBatchId,
        CancellationToken cancellationToken = default
    );

    Task AbandonAsync(Guid gameId, Guid playerId, CancellationToken cancellationToken = default);
}

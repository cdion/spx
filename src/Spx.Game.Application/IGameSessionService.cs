using Spx.Contracts;

namespace Spx.Game.Application;

public interface IGameSessionService
{
    Task<bool> EnsureSessionAsync(Guid gameId, IReadOnlyList<GameSessionParticipantView> players, CancellationToken cancellationToken = default);

    Task<GameSessionView?> GetSessionViewAsync(Guid gameId, string userId, CancellationToken cancellationToken = default);

    Task<GameSessionCommandOutcome> SubmitAcquireAsync(Guid gameId, SubmitAcquireCardCommand command, CancellationToken cancellationToken = default);

    Task<GameSessionCommandOutcome> SubmitPlayBatchAsync(Guid gameId, SubmitPlayBatchCommand command, CancellationToken cancellationToken = default);

    Task<GameSessionView> AbandonAsync(Guid gameId, string userId, CancellationToken cancellationToken = default);
}
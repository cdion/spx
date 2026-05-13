using Spx.Contracts;

namespace Spx.Game.Application;

public interface IGameSessionService
{
    Task<bool> EnsureSessionAsync(Guid gameId, IReadOnlyList<GameSessionParticipantView> players, CancellationToken cancellationToken = default);

    Task<GameSessionView?> GetSessionViewAsync(Guid gameId, string userId, CancellationToken cancellationToken = default);

    Task<GameSessionView> SubmitMoveAsync(Guid gameId, SubmitGameMoveCommand command, CancellationToken cancellationToken = default);

    Task<GameSessionView> AbandonAsync(Guid gameId, string userId, CancellationToken cancellationToken = default);
}
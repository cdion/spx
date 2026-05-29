using Spx.Nexus.Domain;

namespace Spx.Game.Application.Nexus;

public interface INexusSessionService
{
    Task<bool> EnsureSessionAsync(
        Guid gameId,
        IReadOnlyList<Guid> playerIds,
        CancellationToken cancellationToken = default
    );

    Task<GameSessionOutcome> GetSessionAsync(
        Guid gameId,
        Guid playerId,
        CancellationToken cancellationToken = default
    );

    Task<GameSessionCommandOutcome> SubmitOrdersAsync(
        Guid gameId,
        NexusTurnOrdersCommand command,
        CancellationToken cancellationToken = default
    );

    Task AbandonAsync(Guid gameId, Guid playerId, CancellationToken cancellationToken = default);
}

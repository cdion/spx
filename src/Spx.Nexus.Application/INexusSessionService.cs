namespace Spx.Nexus.Application;

public interface INexusSessionService
{
    Task<bool> EnsureSessionAsync(
        Guid gameId,
        IReadOnlyList<GameSessionParticipant> players,
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

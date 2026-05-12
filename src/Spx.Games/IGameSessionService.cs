using Spx.Contracts;

namespace Spx.Games;

public interface IGameSessionService
{
    Task<bool> TryInitializeAsync(Guid gameId, IReadOnlyList<GameSessionPlayer> players, CancellationToken cancellationToken = default);

    Task<GameSessionPlayerView?> GetPlayerViewAsync(Guid gameId, string userId, CancellationToken cancellationToken = default);

    Task<GameSessionPlayerView> SubmitMoveAsync(Guid gameId, SubmitGameMoveCommand command, CancellationToken cancellationToken = default);

    Task<GameSessionPlayerView> AbandonAsync(Guid gameId, string userId, CancellationToken cancellationToken = default);
}
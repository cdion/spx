namespace Spx.Game.Application.Nexus;

public interface INexusSessionRosterProvider
{
    Task<IReadOnlyList<Guid>?> GetActiveSessionPlayersAsync(
        Guid gameId,
        CancellationToken cancellationToken
    );
}

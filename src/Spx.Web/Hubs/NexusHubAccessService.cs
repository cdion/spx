using System.Security.Claims;
using Spx.Game.Application;

namespace Spx.Web.Hubs;

internal sealed class NexusHubAccessService(IGamePersistence gamePersistence)
    : INexusHubAccessService
{
    public async Task<NexusHubAccess?> GetAccessAsync(
        Guid gameId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default
    )
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var lobby = await gamePersistence.GetLobbyAsync(gameId, userId, cancellationToken);
        if (lobby is null)
        {
            return null;
        }

        return new NexusHubAccess(lobby.CurrentPlayerId, lobby.IsCurrentUserActive);
    }
}

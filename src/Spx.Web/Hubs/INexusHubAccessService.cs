using System.Security.Claims;

namespace Spx.Web.Hubs;

public interface INexusHubAccessService
{
    Task<NexusHubAccess?> GetAccessAsync(
        Guid gameId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default
    );
}

public sealed record NexusHubAccess(Guid PlayerId, bool IsActivePlayer);

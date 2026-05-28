namespace Spx.Nexus.Application.Features.GetNexusPage;

public interface IGetNexusPageHandler
{
    Task<GamePageView?> HandleAsync(
        Guid gameId,
        string userId,
        CancellationToken cancellationToken = default
    );
}

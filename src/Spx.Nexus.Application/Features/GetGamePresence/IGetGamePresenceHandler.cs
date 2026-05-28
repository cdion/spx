namespace Spx.Nexus.Application.Features.GetGamePresence;

public interface IGetGamePresenceHandler
{
    Task<GamePresenceView> HandleAsync(Guid gameId, CancellationToken cancellationToken = default);
}

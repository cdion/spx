namespace Spx.Game.Application.Features.GetGamePresence;

internal sealed class GetGamePresenceHandler(IGamePresenceService gamePresenceService) : IGetGamePresenceHandler
{
    public async Task<GamePresenceView> HandleAsync(Guid gameId, CancellationToken cancellationToken = default)
        => await gamePresenceService.GetPresenceAsync(gameId, cancellationToken);
}
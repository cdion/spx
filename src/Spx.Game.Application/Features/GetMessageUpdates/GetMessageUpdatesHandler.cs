namespace Spx.Game.Application.Features.GetMessageUpdates;

internal sealed class GetMessageUpdatesHandler(IGameMessagePersistence gameMessagePersistence) : IGetMessageUpdatesHandler
{
    public async Task<IReadOnlyList<GameTimelineEntryView>?> HandleAsync(Guid gameId, Guid playerId, Guid? afterMessageId, int take = GameMessageSupport.DefaultPageSize, CancellationToken cancellationToken = default)
        => await gameMessagePersistence.GetMessageUpdatesAsync(gameId, playerId, afterMessageId, take, cancellationToken);
}
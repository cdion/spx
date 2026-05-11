namespace Spx.Games.Features.GetMessageUpdates;

internal sealed class GetMessageUpdatesHandler(IGameMessagePersistence gameMessagePersistence) : IGetMessageUpdatesHandler
{
    public async Task<IReadOnlyList<GameMessageView>?> HandleAsync(Guid gameId, string userId, Guid? afterMessageId, int take = GameMessageSupport.DefaultPageSize, CancellationToken cancellationToken = default)
        => await gameMessagePersistence.GetMessageUpdatesAsync(gameId, userId, afterMessageId, take, cancellationToken);
}
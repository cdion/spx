namespace Spx.Games.Features.GetMessages;

internal sealed class GetMessagesHandler(IGameMessagePersistence gameMessagePersistence) : IGetMessagesHandler
{
    public async Task<GameMessagePageView?> HandleAsync(Guid gameId, string userId, Guid? beforeMessageId = default, int take = GameMessageSupport.DefaultPageSize, CancellationToken cancellationToken = default)
        => await gameMessagePersistence.GetMessagesAsync(gameId, userId, beforeMessageId, take, cancellationToken);
}
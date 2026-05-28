namespace Spx.Nexus.Application.Features.GetMessages;

internal sealed class GetMessagesHandler(IGameMessagePersistence gameMessagePersistence)
    : IGetMessagesHandler
{
    public async Task<GameTimelinePageView?> HandleAsync(
        Guid gameId,
        Guid playerId,
        Guid? beforeMessageId = default,
        int take = GameMessageSupport.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        await gameMessagePersistence.GetMessagesAsync(
            gameId,
            playerId,
            beforeMessageId,
            take,
            cancellationToken
        );
}

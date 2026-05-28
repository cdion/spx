namespace Spx.Nexus.Application.Features.GetMessages;

public interface IGetMessagesHandler
{
    Task<GameTimelinePageView?> HandleAsync(
        Guid gameId,
        Guid playerId,
        Guid? beforeMessageId = default,
        int take = GameMessageSupport.DefaultPageSize,
        CancellationToken cancellationToken = default
    );
}

namespace Spx.Games.Features.GetMessages;

public interface IGetMessagesHandler
{
    Task<GameTimelinePageView?> HandleAsync(Guid gameId, string userId, Guid? beforeMessageId = default, int take = GameMessageSupport.DefaultPageSize, CancellationToken cancellationToken = default);
}
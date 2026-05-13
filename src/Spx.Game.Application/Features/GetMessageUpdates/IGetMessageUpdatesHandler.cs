namespace Spx.Game.Application.Features.GetMessageUpdates;

public interface IGetMessageUpdatesHandler
{
    Task<IReadOnlyList<GameTimelineEntryView>?> HandleAsync(Guid gameId, string userId, Guid? afterMessageId, int take = GameMessageSupport.DefaultPageSize, CancellationToken cancellationToken = default);
}
namespace Spx.Games.Features.GetMessageUpdates;

public interface IGetMessageUpdatesHandler
{
    Task<IReadOnlyList<GameMessageView>?> HandleAsync(Guid gameId, string userId, Guid? afterMessageId, int take = GameMessageSupport.DefaultPageSize, CancellationToken cancellationToken = default);
}
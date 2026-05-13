namespace Spx.Game.Application;

public interface IGameMessagePersistence
{
    Task<GameTimelinePageView?> GetMessagesAsync(
        Guid gameId,
        string userId,
        Guid? beforeMessageId,
        int take,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<GameTimelineEntryView>?> GetMessageUpdatesAsync(
        Guid gameId,
        string userId,
        Guid? afterMessageId,
        int take,
        CancellationToken cancellationToken);

    Task<GameMessageCommandOutcome> SendPublicMessageAsync(
        Guid gameId,
        string userId,
        string body,
        CancellationToken cancellationToken);

    Task<GameMessageCommandOutcome> SendPrivateMessageAsync(
        Guid gameId,
        string userId,
        Guid recipientPlayerId,
        string body,
        CancellationToken cancellationToken);

    Task<GameMessageCommandOutcome> EditMessageAsync(
        Guid gameId,
        string userId,
        Guid messageId,
        string body,
        CancellationToken cancellationToken);

    Task<GameMessageCommandOutcome> DeleteMessageAsync(
        Guid gameId,
        string userId,
        Guid messageId,
        CancellationToken cancellationToken);
}
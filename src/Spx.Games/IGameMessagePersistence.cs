namespace Spx.Games;

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

    Task<GameMessageCommandResult> SendPublicMessageAsync(
        Guid gameId,
        string userId,
        string body,
        CancellationToken cancellationToken);

    Task<GameMessageCommandResult> SendPrivateMessageAsync(
        Guid gameId,
        string userId,
        Guid recipientPlayerId,
        string body,
        CancellationToken cancellationToken);

    Task<GameMessageCommandResult> EditMessageAsync(
        Guid gameId,
        string userId,
        Guid messageId,
        string body,
        CancellationToken cancellationToken);

    Task<GameMessageCommandResult> DeleteMessageAsync(
        Guid gameId,
        string userId,
        Guid messageId,
        CancellationToken cancellationToken);
}
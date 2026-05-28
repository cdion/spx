namespace Spx.Game.Application;

public interface IGameMessagePersistence
{
    Task<GameTimelinePageView?> GetMessagesAsync(
        Guid gameId,
        Guid playerId,
        Guid? beforeMessageId,
        int take,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<GameTimelineEntryView>?> GetMessageUpdatesAsync(
        Guid gameId,
        Guid playerId,
        Guid? afterMessageId,
        int take,
        CancellationToken cancellationToken
    );

    Task<GameMessageCommandOutcome> SendPublicMessageAsync(
        Guid gameId,
        Guid playerId,
        string body,
        CancellationToken cancellationToken
    );

    Task<GameMessageCommandOutcome> SendPrivateMessageAsync(
        Guid gameId,
        Guid playerId,
        Guid recipientPlayerId,
        string body,
        CancellationToken cancellationToken
    );

    Task<GameMessageCommandOutcome> EditMessageAsync(
        Guid gameId,
        Guid playerId,
        Guid messageId,
        string body,
        CancellationToken cancellationToken
    );

    Task<GameMessageCommandOutcome> DeleteMessageAsync(
        Guid gameId,
        Guid playerId,
        Guid messageId,
        CancellationToken cancellationToken
    );

    Task WriteGameplayEventsAsync(
        Guid gameId,
        IReadOnlyList<string> bodies,
        CancellationToken cancellationToken = default
    );
}

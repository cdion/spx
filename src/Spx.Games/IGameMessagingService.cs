namespace Spx.Games;

public interface IGameMessagingService
{
    Task<GameMessagePageView?> GetMessagesAsync(Guid gameId, string userId, Guid? beforeMessageId = default, int take = 20, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GameMessageView>?> GetMessageUpdatesAsync(Guid gameId, string userId, Guid? afterMessageId, int take = 20, CancellationToken cancellationToken = default);

    Task<GameMessageCommandResult> SendPublicMessageAsync(Guid gameId, string userId, SendGameMessageRequest request, CancellationToken cancellationToken = default);

    Task<GameMessageCommandResult> SendPrivateMessageAsync(Guid gameId, string userId, Guid recipientPlayerId, SendGameMessageRequest request, CancellationToken cancellationToken = default);

    Task<GameMessageCommandResult> EditMessageAsync(Guid gameId, string userId, Guid messageId, UpdateGameMessageRequest request, CancellationToken cancellationToken = default);

    Task<GameMessageCommandResult> DeleteMessageAsync(Guid gameId, string userId, Guid messageId, CancellationToken cancellationToken = default);
}
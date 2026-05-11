using Microsoft.Extensions.DependencyInjection;
using Spx.Games;

namespace Spx.Games.Tests;

internal static class GameMessageHandlerTestServices
{
    public static ServiceProvider Create(FakeGameMessagePersistence persistence, FakeGameMessageEventsPublisher publisher)
    {
        var services = new ServiceCollection();
        services.AddGameApplication();
        services.AddSingleton<IGamePersistence, StubGamePersistence>();
        services.AddSingleton<IGameLobbyEventsPublisher, StubGameLobbyEventsPublisher>();
        services.AddSingleton<IGameMessageEventsPublisher>(publisher);
        services.AddSingleton<IGameMessagePersistence>(persistence);
        return services.BuildServiceProvider();
    }
}

internal sealed class FakeGameMessageEventsPublisher : IGameMessageEventsPublisher
{
    public List<Guid> PublishedGameIds { get; } = [];

    public Task PublishMessagesChangedAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        PublishedGameIds.Add(gameId);
        return Task.CompletedTask;
    }
}

internal sealed class FakeGameMessagePersistence : IGameMessagePersistence
{
    public int SendPublicMessageCallCount { get; private set; }

    public int SendPrivateMessageCallCount { get; private set; }

    public int EditMessageCallCount { get; private set; }

    public int DeleteMessageCallCount { get; private set; }

    public string? LastPublicBody { get; private set; }

    public string? LastPrivateBody { get; private set; }

    public string? LastEditBody { get; private set; }

    public Guid? LastRecipientPlayerId { get; private set; }

    public Guid? LastEditedMessageId { get; private set; }

    public Guid? LastDeletedMessageId { get; private set; }

    public GameMessageCommandResult SendPublicMessageResult { get; init; }
        = GameMessageCommandResult.Failure("Send failed.");

    public GameMessageCommandResult SendPrivateMessageResult { get; init; }
        = GameMessageCommandResult.Failure("Send failed.");

    public GameMessageCommandResult EditMessageResult { get; init; }
        = GameMessageCommandResult.Failure("Edit failed.");

    public GameMessageCommandResult DeleteMessageResult { get; init; }
        = GameMessageCommandResult.Failure("Delete failed.");

    public Task<GameMessagePageView?> GetMessagesAsync(Guid gameId, string userId, Guid? beforeMessageId, int take, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<GameMessageView>?> GetMessageUpdatesAsync(Guid gameId, string userId, Guid? afterMessageId, int take, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<GameMessageCommandResult> SendPublicMessageAsync(Guid gameId, string userId, string body, CancellationToken cancellationToken)
    {
        SendPublicMessageCallCount++;
        LastPublicBody = body;
        return Task.FromResult(SendPublicMessageResult);
    }

    public Task<GameMessageCommandResult> SendPrivateMessageAsync(Guid gameId, string userId, Guid recipientPlayerId, string body, CancellationToken cancellationToken)
    {
        SendPrivateMessageCallCount++;
        LastRecipientPlayerId = recipientPlayerId;
        LastPrivateBody = body;
        return Task.FromResult(SendPrivateMessageResult);
    }

    public Task<GameMessageCommandResult> EditMessageAsync(Guid gameId, string userId, Guid messageId, string body, CancellationToken cancellationToken)
    {
        EditMessageCallCount++;
        LastEditedMessageId = messageId;
        LastEditBody = body;
        return Task.FromResult(EditMessageResult);
    }

    public Task<GameMessageCommandResult> DeleteMessageAsync(Guid gameId, string userId, Guid messageId, CancellationToken cancellationToken)
    {
        DeleteMessageCallCount++;
        LastDeletedMessageId = messageId;
        return Task.FromResult(DeleteMessageResult);
    }
}

internal sealed class StubGamePersistence : IGamePersistence
{
    public Task<Guid?> TryCreateGameAsync(CreateGamePersistenceRequest request, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<JoinGamePersistenceResult> JoinGameAsync(JoinGamePersistenceRequest request, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<LeaveGamePersistenceResult> LeaveGameAsync(Guid gameId, string userId, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<GameLobbyView?> GetLobbyAsync(Guid gameId, string userId, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<UserGamesView> GetUserGamesAsync(string userId, CancellationToken cancellationToken)
        => throw new NotSupportedException();
}

internal sealed class StubGameLobbyEventsPublisher : IGameLobbyEventsPublisher
{
    public Task PublishLobbyChangedAsync(Guid gameId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
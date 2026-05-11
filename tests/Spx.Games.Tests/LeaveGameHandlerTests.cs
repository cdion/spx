using Microsoft.Extensions.DependencyInjection;
using Spx.Games;
using Spx.Games.Features.LeaveGame;
using Xunit;

namespace Spx.Games.Tests;

public sealed class LeaveGameHandlerTests
{
    [Fact]
    public async Task HandleAsync_publishes_lobby_and_messages_when_leave_changes_state()
    {
        var gameId = Guid.NewGuid();
        var persistence = new FakeGamePersistence
        {
            LeaveGameResult = new LeaveGamePersistenceResult(GameCommandResult.Success(gameId), true)
        };
        var lobbyPublisher = new FakeGameLobbyEventsPublisher();
        var messagePublisher = new FakeGameMessageEventsPublisher();
        using var services = CreateServices(persistence, lobbyPublisher, messagePublisher);

        var handler = services.GetRequiredService<ILeaveGameHandler>();
        var result = await handler.HandleAsync(gameId, "user-1");

        Assert.True(result.Succeeded);
        Assert.Equal(gameId, persistence.LastLeaveGameId);
        Assert.Equal("user-1", persistence.LastLeaveUserId);
        Assert.Equal([gameId], lobbyPublisher.PublishedGameIds);
        Assert.Equal([gameId], messagePublisher.PublishedGameIds);
    }

    [Fact]
    public async Task HandleAsync_does_not_publish_when_leave_result_is_unchanged()
    {
        var gameId = Guid.NewGuid();
        var persistence = new FakeGamePersistence
        {
            LeaveGameResult = new LeaveGamePersistenceResult(GameCommandResult.Failure("You are not an active player in this game."), false)
        };
        var lobbyPublisher = new FakeGameLobbyEventsPublisher();
        var messagePublisher = new FakeGameMessageEventsPublisher();
        using var services = CreateServices(persistence, lobbyPublisher, messagePublisher);

        var handler = services.GetRequiredService<ILeaveGameHandler>();
        var result = await handler.HandleAsync(gameId, "user-1");

        Assert.False(result.Succeeded);
        Assert.Equal("You are not an active player in this game.", result.ErrorMessage);
        Assert.Empty(lobbyPublisher.PublishedGameIds);
        Assert.Empty(messagePublisher.PublishedGameIds);
    }

    private static ServiceProvider CreateServices(
        FakeGamePersistence persistence,
        FakeGameLobbyEventsPublisher lobbyPublisher,
        FakeGameMessageEventsPublisher messagePublisher)
    {
        var services = new ServiceCollection();
        services.AddGameApplication();
        services.AddSingleton<IGamePersistence>(persistence);
        services.AddSingleton<IGameLobbyEventsPublisher>(lobbyPublisher);
        services.AddSingleton<IGameMessageEventsPublisher>(messagePublisher);
        services.AddSingleton<IGameMessagePersistence, FakeGameMessagePersistence>();
        return services.BuildServiceProvider();
    }

    private sealed class FakeGamePersistence : IGamePersistence
    {
        public LeaveGamePersistenceResult LeaveGameResult { get; init; }
            = new(GameCommandResult.Success(Guid.NewGuid()), false);

        public Guid? LastLeaveGameId { get; private set; }

        public string? LastLeaveUserId { get; private set; }

        public Task<Guid?> TryCreateGameAsync(CreateGamePersistenceRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<JoinGamePersistenceResult> JoinGameAsync(JoinGamePersistenceRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<LeaveGamePersistenceResult> LeaveGameAsync(Guid gameId, string userId, CancellationToken cancellationToken)
        {
            LastLeaveGameId = gameId;
            LastLeaveUserId = userId;
            return Task.FromResult(LeaveGameResult);
        }

        public Task<GameLobbyView?> GetLobbyAsync(Guid gameId, string userId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<UserGamesView> GetUserGamesAsync(string userId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class FakeGameLobbyEventsPublisher : IGameLobbyEventsPublisher
    {
        public List<Guid> PublishedGameIds { get; } = [];

        public Task PublishLobbyChangedAsync(Guid gameId, CancellationToken cancellationToken = default)
        {
            PublishedGameIds.Add(gameId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGameMessageEventsPublisher : IGameMessageEventsPublisher
    {
        public List<Guid> PublishedGameIds { get; } = [];

        public Task PublishMessagesChangedAsync(Guid gameId, CancellationToken cancellationToken = default)
        {
            PublishedGameIds.Add(gameId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGameMessagePersistence : IGameMessagePersistence
    {
        public Task<GameMessagePageView?> GetMessagesAsync(Guid gameId, string userId, Guid? beforeMessageId, int take, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<GameMessageView>?> GetMessageUpdatesAsync(Guid gameId, string userId, Guid? afterMessageId, int take, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<GameMessageCommandResult> SendPublicMessageAsync(Guid gameId, string userId, string body, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<GameMessageCommandResult> SendPrivateMessageAsync(Guid gameId, string userId, Guid recipientPlayerId, string body, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<GameMessageCommandResult> EditMessageAsync(Guid gameId, string userId, Guid messageId, string body, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<GameMessageCommandResult> DeleteMessageAsync(Guid gameId, string userId, Guid messageId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}
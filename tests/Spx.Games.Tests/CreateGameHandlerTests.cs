using Microsoft.Extensions.DependencyInjection;
using Spx.Games;
using Spx.Games.Features.CreateGame;
using Xunit;

namespace Spx.Games.Tests;

public sealed class CreateGameHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_validation_failure_for_short_game_name()
    {
        var persistence = new FakeGamePersistence();
        var lobbyPublisher = new FakeGameLobbyEventsPublisher();
        var messagePublisher = new FakeGameMessageEventsPublisher();
        using var services = CreateServices(persistence, lobbyPublisher, messagePublisher);

        var handler = services.GetRequiredService<ICreateGameHandler>();
        var result = await handler.HandleAsync("user-1", new CreateGameRequest("A", "Captain Red"));

        Assert.False(result.Succeeded);
        Assert.Equal("Game names must be at least 2 characters long.", result.ErrorMessage);
        Assert.Empty(persistence.CreateRequests);
        Assert.Empty(lobbyPublisher.PublishedGameIds);
        Assert.Empty(messagePublisher.PublishedGameIds);
    }

    [Fact]
    public async Task HandleAsync_publishes_events_when_game_is_created()
    {
        var gameId = Guid.NewGuid();
        var persistence = new FakeGamePersistence(gameId);
        var lobbyPublisher = new FakeGameLobbyEventsPublisher();
        var messagePublisher = new FakeGameMessageEventsPublisher();
        using var services = CreateServices(persistence, lobbyPublisher, messagePublisher);

        var handler = services.GetRequiredService<ICreateGameHandler>();
        var result = await handler.HandleAsync("user-1", new CreateGameRequest("  Weekend Match  ", "  Captain Red  "));

        Assert.True(result.Succeeded);
        Assert.Equal(gameId, result.GameId);
        Assert.Single(persistence.CreateRequests);
        Assert.Equal("Weekend Match", persistence.CreateRequests[0].GameName);
        Assert.Equal("Captain Red", persistence.CreateRequests[0].PlayerName);
        Assert.Equal("CAPTAIN RED", persistence.CreateRequests[0].PlayerNameLookup);
        Assert.Equal([gameId], lobbyPublisher.PublishedGameIds);
        Assert.Equal([gameId], messagePublisher.PublishedGameIds);
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

    private sealed class FakeGamePersistence(Guid? gameId = null) : IGamePersistence
    {
        public List<CreateGamePersistenceRequest> CreateRequests { get; } = [];

        public Task<Guid?> TryCreateGameAsync(CreateGamePersistenceRequest request, CancellationToken cancellationToken)
        {
            CreateRequests.Add(request);
            return Task.FromResult(gameId);
        }

        public Task<JoinGamePersistenceResult> JoinGameAsync(JoinGamePersistenceRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<LeaveGamePersistenceResult> LeaveGameAsync(Guid gameId, string userId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

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

        public Task<GameMessageCommandResult> SendPublicMessageAsync(Guid gameId, string userId, string body, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<GameMessageCommandResult> SendPrivateMessageAsync(Guid gameId, string userId, Guid recipientPlayerId, string body, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<GameMessageView>?> GetMessageUpdatesAsync(Guid gameId, string userId, Guid? afterMessageId, int take, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<GameMessageCommandResult> EditMessageAsync(Guid gameId, string userId, Guid messageId, string body, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<GameMessageCommandResult> DeleteMessageAsync(Guid gameId, string userId, Guid messageId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}
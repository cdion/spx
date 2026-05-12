using Microsoft.Extensions.DependencyInjection;
using Spx.Contracts;
using Spx.Games;
using Spx.Games.Features.JoinGame;
using Xunit;

namespace Spx.Games.Tests;

public sealed class JoinGameHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_validation_failure_for_short_invite_code()
    {
        var persistence = new FakeGamePersistence();
        var lobbyPublisher = new FakeGameLobbyEventsPublisher();
        var messagePublisher = new FakeGameMessageEventsPublisher();
        var sessionService = new FakeGameSessionService();
        using var services = CreateServices(persistence, lobbyPublisher, messagePublisher, sessionService);

        var handler = services.GetRequiredService<IJoinGameHandler>();
        var result = await handler.HandleAsync("user-1", new JoinGameRequest("abc", "Captain Red"));

        Assert.False(result.Succeeded);
        Assert.Equal("Invite codes must be six characters long.", result.ErrorMessage);
        Assert.Null(persistence.LastJoinRequest);
        Assert.Empty(sessionService.InitializedGameIds);
        Assert.Empty(lobbyPublisher.PublishedGameIds);
        Assert.Empty(messagePublisher.PublishedGameIds);
    }

    [Fact]
    public async Task HandleAsync_publishes_lobby_and_messages_when_persistence_requests_both()
    {
        var gameId = Guid.NewGuid();
        var persistence = new FakeGamePersistence
        {
            JoinGameResult = new JoinGamePersistenceResult(GameCommandResult.Success(gameId), gameId, true),
            ActiveSessionPlayers =
            [
                new GameSessionPlayer(Guid.NewGuid(), "user-1", "Captain Red"),
                new GameSessionPlayer(Guid.NewGuid(), "user-2", "Captain Blue")
            ]
        };
        var lobbyPublisher = new FakeGameLobbyEventsPublisher();
        var messagePublisher = new FakeGameMessageEventsPublisher();
        var sessionService = new FakeGameSessionService();
        using var services = CreateServices(persistence, lobbyPublisher, messagePublisher, sessionService);

        var handler = services.GetRequiredService<IJoinGameHandler>();
        var result = await handler.HandleAsync("user-1", new JoinGameRequest(" abc123 ", " Captain Red "));

        Assert.True(result.Succeeded);
        Assert.NotNull(persistence.LastJoinRequest);
        Assert.Equal("ABC123", persistence.LastJoinRequest!.InviteCode);
        Assert.Equal("Captain Red", persistence.LastJoinRequest.PlayerName);
        Assert.Equal("CAPTAIN RED", persistence.LastJoinRequest.PlayerNameLookup);
        Assert.Equal([gameId], sessionService.InitializedGameIds);
        Assert.Equal([gameId], lobbyPublisher.PublishedGameIds);
        Assert.Equal([gameId], messagePublisher.PublishedGameIds);
    }

    [Fact]
    public async Task HandleAsync_does_not_publish_when_persistence_returns_failure_without_game_id()
    {
        var persistence = new FakeGamePersistence
        {
            JoinGameResult = new JoinGamePersistenceResult(
                GameCommandResult.Failure("That player name is already taken in this game."),
                null,
                false)
        };
        var lobbyPublisher = new FakeGameLobbyEventsPublisher();
        var messagePublisher = new FakeGameMessageEventsPublisher();
        var sessionService = new FakeGameSessionService();
        using var services = CreateServices(persistence, lobbyPublisher, messagePublisher, sessionService);

        var handler = services.GetRequiredService<IJoinGameHandler>();
        var result = await handler.HandleAsync("user-3", new JoinGameRequest("ABC123", "Captain Red"));

        Assert.False(result.Succeeded);
        Assert.Equal("That player name is already taken in this game.", result.ErrorMessage);
        Assert.Empty(sessionService.InitializedGameIds);
        Assert.Empty(lobbyPublisher.PublishedGameIds);
        Assert.Empty(messagePublisher.PublishedGameIds);
    }

    private static ServiceProvider CreateServices(
        FakeGamePersistence persistence,
        FakeGameLobbyEventsPublisher lobbyPublisher,
        FakeGameMessageEventsPublisher messagePublisher,
        FakeGameSessionService sessionService)
    {
        var services = new ServiceCollection();
        services.AddGameApplication();
        services.AddSingleton<IGamePersistence>(persistence);
        services.AddSingleton<IGameSessionService>(sessionService);
        services.AddSingleton<IGameLobbyEventsPublisher>(lobbyPublisher);
        services.AddSingleton<IGameMessageEventsPublisher>(messagePublisher);
        services.AddSingleton<IGameMessagePersistence, FakeGameMessagePersistence>();
        return services.BuildServiceProvider();
    }

    private sealed class FakeGamePersistence : IGamePersistence
    {
        public JoinGamePersistenceResult JoinGameResult { get; init; }
            = new(GameCommandResult.Success(Guid.NewGuid()), Guid.NewGuid(), false);

        public JoinGamePersistenceRequest? LastJoinRequest { get; private set; }

        public IReadOnlyList<GameSessionPlayer>? ActiveSessionPlayers { get; init; }

        public Task<Guid?> TryCreateGameAsync(CreateGamePersistenceRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<JoinGamePersistenceResult> JoinGameAsync(JoinGamePersistenceRequest request, CancellationToken cancellationToken)
        {
            LastJoinRequest = request;
            return Task.FromResult(JoinGameResult);
        }

        public Task<LeaveGamePersistenceResult> LeaveGameAsync(Guid gameId, string userId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<GameSessionPlayer>?> GetActiveSessionPlayersAsync(Guid gameId, CancellationToken cancellationToken)
            => Task.FromResult(ActiveSessionPlayers);

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

    private sealed class FakeGameSessionService : IGameSessionService
    {
        public List<Guid> InitializedGameIds { get; } = [];

        public Task<bool> TryInitializeAsync(Guid gameId, IReadOnlyList<GameSessionPlayer> players, CancellationToken cancellationToken = default)
        {
            InitializedGameIds.Add(gameId);
            return Task.FromResult(true);
        }

        public Task<GameSessionPlayerView?> GetPlayerViewAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GameSessionPlayerView> SubmitMoveAsync(Guid gameId, SubmitGameMoveCommand command, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GameSessionPlayerView> AbandonAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
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
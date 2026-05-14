using Microsoft.Extensions.DependencyInjection;
using Spx.Contracts;
using Spx.Game.Application;
using Spx.Game.Application.Features.JoinGame;
using Xunit;

namespace Spx.Game.Application.Tests;

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

        var failed = Assert.IsType<GameCommandFailed>(result);
        Assert.Equal("Invite codes must be six characters long.", failed.ErrorMessage);
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
            JoinGameResult = new JoinGamePersistenceResult(new GameCommandSucceeded(gameId), gameId, true),
            ActiveSessionPlayers =
            [
                new GameSessionParticipantView(Guid.NewGuid(), "user-1"),
                new GameSessionParticipantView(Guid.NewGuid(), "user-2")
            ]
        };
        var lobbyPublisher = new FakeGameLobbyEventsPublisher();
        var messagePublisher = new FakeGameMessageEventsPublisher();
        var sessionService = new FakeGameSessionService();
        using var services = CreateServices(persistence, lobbyPublisher, messagePublisher, sessionService);

        var handler = services.GetRequiredService<IJoinGameHandler>();
        var result = await handler.HandleAsync("user-1", new JoinGameRequest(" abc123 ", " Captain Red "));

        Assert.IsType<GameCommandSucceeded>(result);
        Assert.NotNull(persistence.LastJoinRequest);
        Assert.Equal("ABC123", persistence.LastJoinRequest!.InviteCode);
        Assert.Equal("Captain Red", persistence.LastJoinRequest.PlayerName);
        Assert.Equal("CAPTAIN RED", persistence.LastJoinRequest.PlayerNameLookup);
        Assert.Equal([gameId], sessionService.InitializedGameIds);
        Assert.Equal([gameId], lobbyPublisher.PublishedGameIds);
        Assert.Equal([gameId], messagePublisher.PublishedGameIds);
    }

    [Fact]
    public async Task HandleAsync_keeps_join_success_and_publishes_when_session_initialization_fails()
    {
        var gameId = Guid.NewGuid();
        var persistence = new FakeGamePersistence
        {
            JoinGameResult = new JoinGamePersistenceResult(new GameCommandSucceeded(gameId), gameId, true),
            ActiveSessionPlayers =
            [
                new GameSessionParticipantView(Guid.NewGuid(), "user-1"),
                new GameSessionParticipantView(Guid.NewGuid(), "user-2")
            ]
        };
        var lobbyPublisher = new FakeGameLobbyEventsPublisher();
        var messagePublisher = new FakeGameMessageEventsPublisher();
        var sessionService = new FakeGameSessionService
        {
            TryInitializeResult = false
        };
        using var services = CreateServices(persistence, lobbyPublisher, messagePublisher, sessionService);

        var handler = services.GetRequiredService<IJoinGameHandler>();
        var result = await handler.HandleAsync("user-1", new JoinGameRequest("ABC123", "Captain Red"));

        var succeeded = Assert.IsType<GameCommandSucceeded>(result);
        Assert.Equal(gameId, succeeded.GameId);
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
                new GameCommandFailed("That player name is already taken in this game."),
                null,
                false)
        };
        var lobbyPublisher = new FakeGameLobbyEventsPublisher();
        var messagePublisher = new FakeGameMessageEventsPublisher();
        var sessionService = new FakeGameSessionService();
        using var services = CreateServices(persistence, lobbyPublisher, messagePublisher, sessionService);

        var handler = services.GetRequiredService<IJoinGameHandler>();
        var result = await handler.HandleAsync("user-3", new JoinGameRequest("ABC123", "Captain Red"));

        var failed = Assert.IsType<GameCommandFailed>(result);
        Assert.Equal("That player name is already taken in this game.", failed.ErrorMessage);
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
        services.AddSingleton<IGameLobbyInvalidationPublisher>(lobbyPublisher);
        services.AddSingleton<IGameSessionInvalidationPublisher, StubGameSessionInvalidationPublisher>();
        services.AddSingleton<IGameMessageInvalidationPublisher>(messagePublisher);
        services.AddSingleton<IGameMessagePersistence, FakeGameMessagePersistence>();
        return services.BuildServiceProvider();
    }

    private sealed class FakeGamePersistence : IGamePersistence
    {
        public JoinGamePersistenceResult JoinGameResult { get; init; }
            = new(new GameCommandSucceeded(Guid.NewGuid()), Guid.NewGuid(), false);

        public JoinGamePersistenceRequest? LastJoinRequest { get; private set; }

        public IReadOnlyList<GameSessionParticipantView>? ActiveSessionPlayers { get; init; }

        public Task<Guid?> TryCreateGameAsync(CreateGamePersistenceRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<JoinGamePersistenceResult> JoinGameAsync(JoinGamePersistenceRequest request, CancellationToken cancellationToken)
        {
            LastJoinRequest = request;
            return Task.FromResult(JoinGameResult);
        }

        public Task<LeaveGamePersistenceResult> LeaveGameAsync(Guid gameId, string userId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<GameSessionParticipantView>?> GetActiveSessionPlayersAsync(Guid gameId, CancellationToken cancellationToken)
            => Task.FromResult(ActiveSessionPlayers);

        public Task<GameLobbyView?> GetLobbyAsync(Guid gameId, string userId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<UserGamesView> GetUserGamesAsync(string userId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class FakeGameLobbyEventsPublisher : IGameLobbyInvalidationPublisher
    {
        public List<Guid> PublishedGameIds { get; } = [];

        public Task PublishLobbyInvalidatedAsync(Guid gameId, CancellationToken cancellationToken = default)
        {
            PublishedGameIds.Add(gameId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGameMessageEventsPublisher : IGameMessageInvalidationPublisher
    {
        public List<Guid> PublishedGameIds { get; } = [];

        public Task PublishMessagesInvalidatedAsync(Guid gameId, CancellationToken cancellationToken = default)
        {
            PublishedGameIds.Add(gameId);
            return Task.CompletedTask;
        }
    }

    private sealed class StubGameSessionInvalidationPublisher : IGameSessionInvalidationPublisher
    {
        public Task PublishSessionInvalidatedAsync(Guid gameId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeGameSessionService : IGameSessionService
    {
        public List<Guid> InitializedGameIds { get; } = [];

        public bool TryInitializeResult { get; init; } = true;

        public Task<bool> EnsureSessionAsync(Guid gameId, IReadOnlyList<GameSessionParticipantView> players, CancellationToken cancellationToken = default)
        {
            InitializedGameIds.Add(gameId);
            return Task.FromResult(TryInitializeResult);
        }

        public Task<GameSessionView?> GetSessionViewAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<SubmitGameMoveOutcome> SubmitMoveAsync(Guid gameId, SubmitGameMoveCommand command, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GameSessionView> AbandonAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class FakeGameMessagePersistence : IGameMessagePersistence
    {
        public Task<GameTimelinePageView?> GetMessagesAsync(Guid gameId, string userId, Guid? beforeMessageId, int take, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<GameMessageCommandOutcome> SendPublicMessageAsync(Guid gameId, string userId, string body, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<GameMessageCommandOutcome> SendPrivateMessageAsync(Guid gameId, string userId, Guid recipientPlayerId, string body, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<GameTimelineEntryView>?> GetMessageUpdatesAsync(Guid gameId, string userId, Guid? afterMessageId, int take, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<GameMessageCommandOutcome> EditMessageAsync(Guid gameId, string userId, Guid messageId, string body, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<GameMessageCommandOutcome> DeleteMessageAsync(Guid gameId, string userId, Guid messageId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}
using Microsoft.Extensions.DependencyInjection;
using Spx.Contracts;
using Spx.Game.Application;
using Spx.Game.Application.Features.LeaveGame;
using Xunit;

namespace Spx.Game.Application.Tests;

public sealed class LeaveGameHandlerTests
{
    [Fact]
    public async Task HandleAsync_publishes_lobby_and_messages_when_leave_changes_state()
    {
        var gameId = Guid.NewGuid();
        var persistence = new FakeGamePersistence
        {
            LeaveGameResult = new LeaveGamePersistenceResult(new GameCommandSucceeded(gameId), true)
        };
        var sessionService = new FakeGameSessionService();
        var lobbyPublisher = new FakeGameLobbyEventsPublisher();
        var messagePublisher = new FakeGameMessageEventsPublisher();
        using var services = CreateServices(persistence, sessionService, lobbyPublisher, messagePublisher);

        var handler = services.GetRequiredService<ILeaveGameHandler>();
        var result = await handler.HandleAsync(gameId, "user-1");

        Assert.IsType<GameCommandSucceeded>(result);
        Assert.Equal(gameId, persistence.LastLeaveGameId);
        Assert.Equal("user-1", persistence.LastLeaveUserId);
        Assert.Equal([gameId], sessionService.AbandonedGameIds);
        Assert.Equal([gameId], lobbyPublisher.PublishedGameIds);
        Assert.Equal([gameId], messagePublisher.PublishedGameIds);
    }

    [Fact]
    public async Task HandleAsync_does_not_publish_when_leave_result_is_unchanged()
    {
        var gameId = Guid.NewGuid();
        var persistence = new FakeGamePersistence
        {
            LeaveGameResult = new LeaveGamePersistenceResult(new GameCommandFailed("You are not an active player in this game."), false)
        };
        var sessionService = new FakeGameSessionService { ActiveSessionView = null };
        var lobbyPublisher = new FakeGameLobbyEventsPublisher();
        var messagePublisher = new FakeGameMessageEventsPublisher();
        using var services = CreateServices(persistence, sessionService, lobbyPublisher, messagePublisher);

        var handler = services.GetRequiredService<ILeaveGameHandler>();
        var result = await handler.HandleAsync(gameId, "user-1");

        var failed = Assert.IsType<GameCommandFailed>(result);
        Assert.Equal("You are not an active player in this game.", failed.ErrorMessage);
        Assert.Empty(sessionService.AbandonedGameIds);
        Assert.Empty(lobbyPublisher.PublishedGameIds);
        Assert.Empty(messagePublisher.PublishedGameIds);
    }

    private static ServiceProvider CreateServices(
        FakeGamePersistence persistence,
        FakeGameSessionService sessionService,
        FakeGameLobbyEventsPublisher lobbyPublisher,
        FakeGameMessageEventsPublisher messagePublisher)
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
        public LeaveGamePersistenceResult LeaveGameResult { get; init; }
            = new(new GameCommandSucceeded(Guid.NewGuid()), false);

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

        public Task<IReadOnlyList<GameSessionParticipantView>?> GetActiveSessionPlayersAsync(Guid gameId, CancellationToken cancellationToken)
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
        public Task<GameTimelinePageView?> GetMessagesAsync(Guid gameId, string userId, Guid? beforeMessageId, int take, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<GameTimelineEntryView>?> GetMessageUpdatesAsync(Guid gameId, string userId, Guid? afterMessageId, int take, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<GameMessageCommandOutcome> SendPublicMessageAsync(Guid gameId, string userId, string body, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<GameMessageCommandOutcome> SendPrivateMessageAsync(Guid gameId, string userId, Guid recipientPlayerId, string body, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<GameMessageCommandOutcome> EditMessageAsync(Guid gameId, string userId, Guid messageId, string body, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<GameMessageCommandOutcome> DeleteMessageAsync(Guid gameId, string userId, Guid messageId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class FakeGameSessionService : IGameSessionService
    {
        public List<Guid> AbandonedGameIds { get; } = [];

        public GameSessionView? ActiveSessionView { get; init; }
            = new(
                Guid.NewGuid(),
                1,
                new GameSessionParticipantView(Guid.NewGuid(), "user-1"),
                new GameSessionParticipantView(Guid.NewGuid(), "user-2"),
                false,
                false,
                null);

        public Task<bool> EnsureSessionAsync(Guid gameId, IReadOnlyList<GameSessionParticipantView> players, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<GameSessionView?> GetSessionViewAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(ActiveSessionView is null ? null : ActiveSessionView with { GameId = gameId });

        public Task<SubmitGameMoveOutcome> SubmitMoveAsync(Guid gameId, SubmitGameMoveCommand command, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GameSessionView> AbandonAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
        {
            AbandonedGameIds.Add(gameId);
            return Task.FromResult(ActiveSessionView ?? throw new InvalidOperationException("No active session view was configured for this test."));
        }
    }
}
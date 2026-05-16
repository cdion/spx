using Microsoft.Extensions.DependencyInjection;
using Spx.Contracts;
using Spx.Game.Application;
using Spx.Game.Application.Features.GetGamePage;
using Xunit;

namespace Spx.Game.Application.Tests;

public sealed class GetGamePageHandlerTests
{
    private static readonly Guid CurrentPlayerId = Guid.NewGuid();
    private static readonly Guid OpponentPlayerId = Guid.NewGuid();

    [Fact]
    public async Task HandleAsync_returns_null_when_lobby_not_found()
    {
        var persistence = new FakeGamePersistence();
        var sessionService = new FakeGameSessionService();
        using var services = CreateServices(persistence, sessionService);

        var handler = services.GetRequiredService<IGetGamePageHandler>();
        var result = await handler.HandleAsync(Guid.NewGuid(), "user-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_returns_lobby_and_session_when_available()
    {
        var gameId = Guid.NewGuid();
        var presence = new GamePresenceView([CurrentPlayerId]);
        var lobby = new GameLobbyView(
            gameId,
            "Arena",
            "ABC123",
            GameStatus.Open,
            2,
            DateTime.UtcNow,
            null,
            "Captain Red",
            [new GamePlayerView(CurrentPlayerId, "Captain Red", DateTime.UtcNow, true), new GamePlayerView(OpponentPlayerId, "Captain Blue", DateTime.UtcNow, false)],
            true);

        var session = CreateSession(gameId, 3, waitingForOpponent: true);

        var persistence = new FakeGamePersistence { Lobby = lobby };
        var sessionService = new FakeGameSessionService { Session = session };
        var presenceService = new FakeGamePresenceService { Presence = presence };
        using var services = CreateServices(persistence, sessionService, presenceService);

        var handler = services.GetRequiredService<IGetGamePageHandler>();
        var result = await handler.HandleAsync(gameId, "user-1");

        Assert.NotNull(result);
        Assert.Equal(lobby, result!.Lobby);
        Assert.Equal(session, result.Session);
        Assert.Equal(presence, result.Presence);
    }

    [Fact]
    public async Task HandleAsync_repairs_missing_session_for_full_active_lobby()
    {
        var gameId = Guid.NewGuid();
        var lobby = new GameLobbyView(
            gameId,
            "Arena",
            "ABC123",
            GameStatus.Open,
            2,
            DateTime.UtcNow,
            null,
            "Captain Red",
            [new GamePlayerView(CurrentPlayerId, "Captain Red", DateTime.UtcNow, true), new GamePlayerView(OpponentPlayerId, "Captain Blue", DateTime.UtcNow, false)],
            true);

        var session = CreateSession(gameId, 1, waitingForOpponent: false);
        var presence = new GamePresenceView([OpponentPlayerId]);

        var persistence = new FakeGamePersistence
        {
            Lobby = lobby,
            ActiveSessionPlayers =
            [
                new GameSessionParticipantView(CurrentPlayerId, "user-1"),
                new GameSessionParticipantView(OpponentPlayerId, "user-2")
            ]
        };
        var sessionService = new FakeGameSessionService
        {
            Session = null,
            SessionAfterInitialize = session
        };
        var presenceService = new FakeGamePresenceService { Presence = presence };
        using var services = CreateServices(persistence, sessionService, presenceService);

        var handler = services.GetRequiredService<IGetGamePageHandler>();
        var result = await handler.HandleAsync(gameId, "user-1");

        Assert.NotNull(result);
        Assert.Equal(session, result!.Session);
        Assert.Equal(presence, result.Presence);
        Assert.Equal(1, sessionService.InitializeCalls);
    }

    private static ServiceProvider CreateServices(FakeGamePersistence persistence, FakeGameSessionService sessionService, FakeGamePresenceService? presenceService = null)
    {
        var services = new ServiceCollection();
        services.AddGameApplication();
        services.AddSingleton<IGamePersistence>(persistence);
        services.AddSingleton<IGameSessionService>(sessionService);
        services.AddSingleton<IGamePresenceService>(presenceService ?? new FakeGamePresenceService());
        services.AddSingleton<IGameLobbyInvalidationPublisher, StubGameLobbyEventsPublisher>();
        services.AddSingleton<IGameSessionInvalidationPublisher, StubGameSessionInvalidationPublisher>();
        services.AddSingleton<IGameMessageInvalidationPublisher, StubGameMessageEventsPublisher>();
        services.AddSingleton<IGameMessagePersistence, StubGameMessagePersistence>();
        return services.BuildServiceProvider();
    }

    private static GameSessionView CreateSession(Guid gameId, int roundNumber, bool waitingForOpponent)
    {
        var currentPlayer = new GameSessionParticipantView(CurrentPlayerId, "user-1");
        var opponentPlayer = new GameSessionParticipantView(OpponentPlayerId, "user-2");

        return new GameSessionView(
            gameId,
            roundNumber,
            GamePhase.Play,
            new GamePlayerStateView(currentPlayer, [], false, 0, 0, false, false, []),
            new GamePlayerStateView(opponentPlayer, [], false, 0, 0, false, true, []),
            [],
            0,
            waitingForOpponent,
            false,
            false,
            GameCardCatalog.MaxBatchSize,
            null,
            null);
    }

    private sealed class FakeGamePersistence : IGamePersistence
    {
        public GameLobbyView? Lobby { get; init; }

        public IReadOnlyList<GameSessionParticipantView>? ActiveSessionPlayers { get; init; }

        public Task<Guid?> TryCreateGameAsync(CreateGamePersistenceRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<JoinGamePersistenceResult> JoinGameAsync(JoinGamePersistenceRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<LeaveGamePersistenceResult> LeaveGameAsync(Guid gameId, string userId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<GameSessionParticipantView>?> GetActiveSessionPlayersAsync(Guid gameId, CancellationToken cancellationToken)
            => Task.FromResult(ActiveSessionPlayers);

        public Task<GameLobbyView?> GetLobbyAsync(Guid gameId, string userId, CancellationToken cancellationToken)
            => Task.FromResult(Lobby);

        public Task<UserGamesView> GetUserGamesAsync(string userId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class FakeGameSessionService : IGameSessionService
    {
        public GameSessionView? Session { get; set; }

        public GameSessionView? SessionAfterInitialize { get; init; }

        public int InitializeCalls { get; private set; }

        public bool TryInitializeResult { get; init; } = true;

        public Task<bool> EnsureSessionAsync(Guid gameId, IReadOnlyList<GameSessionParticipantView> players, CancellationToken cancellationToken = default)
        {
            InitializeCalls++;
            if (TryInitializeResult)
            {
                Session = SessionAfterInitialize ?? Session;
            }

            return Task.FromResult(TryInitializeResult);
        }

        public Task<GameSessionView?> GetSessionViewAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(Session);

        public Task<GameSessionCommandOutcome> SubmitAcquireAsync(Guid gameId, SubmitAcquireCardCommand command, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GameSessionCommandOutcome> SubmitPlayBatchAsync(Guid gameId, SubmitPlayBatchCommand command, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GameSessionView> AbandonAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class FakeGamePresenceService : IGamePresenceService
    {
        public GamePresenceView Presence { get; init; } = GamePresenceView.Empty;

        public Task<GamePresenceView> GetPresenceAsync(Guid gameId, CancellationToken cancellationToken = default)
            => Task.FromResult(Presence);

        public Task UpsertPresenceLeaseAsync(Guid gameId, Guid playerId, Guid connectionId, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task RemovePresenceLeaseAsync(Guid gameId, Guid playerId, Guid connectionId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class StubGameLobbyEventsPublisher : IGameLobbyInvalidationPublisher
    {
        public Task PublishLobbyInvalidatedAsync(Guid gameId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class StubGameSessionInvalidationPublisher : IGameSessionInvalidationPublisher
    {
        public Task PublishSessionInvalidatedAsync(Guid gameId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class StubGameMessageEventsPublisher : IGameMessageInvalidationPublisher
    {
        public Task PublishMessagesInvalidatedAsync(Guid gameId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class StubGameMessagePersistence : IGameMessagePersistence
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
}
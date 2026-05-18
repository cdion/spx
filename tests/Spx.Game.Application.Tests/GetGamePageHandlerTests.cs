using Microsoft.Extensions.DependencyInjection;
using Spx.Game.Domain;
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
            CurrentPlayerId,
            [new GamePlayerView(CurrentPlayerId, "Captain Red", DateTime.UtcNow), new GamePlayerView(OpponentPlayerId, "Captain Blue", DateTime.UtcNow)],
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
    public async Task HandleAsync_returns_null_session_without_repair_when_session_is_missing()
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
            CurrentPlayerId,
            [new GamePlayerView(CurrentPlayerId, "Captain Red", DateTime.UtcNow), new GamePlayerView(OpponentPlayerId, "Captain Blue", DateTime.UtcNow)],
            true);

        var presence = new GamePresenceView([OpponentPlayerId]);

        var persistence = new FakeGamePersistence
        {
            Lobby = lobby
        };
        var sessionService = new FakeGameSessionService
        {
            Session = null
        };
        var presenceService = new FakeGamePresenceService { Presence = presence };
        using var services = CreateServices(persistence, sessionService, presenceService);

        var handler = services.GetRequiredService<IGetGamePageHandler>();
        var result = await handler.HandleAsync(gameId, "user-1");

        Assert.NotNull(result);
        Assert.Null(result!.Session);
        Assert.Equal(presence, result.Presence);
        Assert.Equal(0, sessionService.InitializeCalls);
    }

    private static ServiceProvider CreateServices(FakeGamePersistence persistence, FakeGameSessionService sessionService, FakeGamePresenceService? presenceService = null)
    {
        var services = new ServiceCollection();
        services.AddGameApplication();
        services.AddSingleton<IGamePersistence>(persistence);
        services.AddSingleton<IGameSessionService>(sessionService);
        services.AddSingleton<IGamePresenceService>(presenceService ?? new FakeGamePresenceService());
        services.AddSingleton(Substitute.For<IGameLobbyInvalidationPublisher>());
        services.AddSingleton(Substitute.For<IGameSessionInvalidationPublisher>());
        services.AddSingleton(Substitute.For<IGameMessageInvalidationPublisher>());
        services.AddSingleton(Substitute.For<IGameMessagePersistence>());
        return services.BuildServiceProvider();
    }

    private static GameSessionView CreateSession(Guid gameId, int roundNumber, bool waitingForOpponent)
    {
        var currentPlayer = new GameSessionParticipant(CurrentPlayerId);
        var opponentPlayer = new GameSessionParticipant(OpponentPlayerId);

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

        public IReadOnlyList<GameSessionParticipant>? ActiveSessionPlayers { get; init; }

        public Task<Guid?> TryCreateGameAsync(CreateGamePersistenceRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<JoinGamePersistenceResult> JoinGameAsync(JoinGamePersistenceRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<LeaveGamePersistenceResult> LeaveGameAsync(Guid gameId, string userId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<GameSessionParticipant>?> GetActiveSessionPlayersAsync(Guid gameId, CancellationToken cancellationToken)
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

        public Task<bool> EnsureSessionAsync(Guid gameId, IReadOnlyList<GameSessionParticipant> players, CancellationToken cancellationToken = default)
        {
            InitializeCalls++;
            if (TryInitializeResult)
            {
                Session = SessionAfterInitialize ?? Session;
            }

            return Task.FromResult(TryInitializeResult);
        }

        public Task<GameSessionView?> GetSessionAsync(Guid gameId, Guid playerId, CancellationToken cancellationToken = default)
            => Task.FromResult(Session);

        public Task AcknowledgeGameplayEventBatchAsync(Guid gameId, Guid gameplayEventBatchId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<GameSessionCommandOutcome> SubmitAcquireAsync(Guid gameId, SubmitAcquireRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GameSessionCommandOutcome> SubmitPlayBatchAsync(Guid gameId, SubmitPlayBatchRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task AbandonAsync(Guid gameId, Guid playerId, CancellationToken cancellationToken = default)
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
}
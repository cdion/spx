using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Spx.Game.Application;
using Spx.Game.Application.Nexus.Features.GetNexusPage;
using Spx.Nexus.Domain;
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

        var handler = services.GetRequiredService<IGetNexusPageHandler>();
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
            [
                new GamePlayerView(CurrentPlayerId, "Captain Red", DateTime.UtcNow),
                new GamePlayerView(OpponentPlayerId, "Captain Blue", DateTime.UtcNow),
            ],
            true
        );

        var session = CreateSession(gameId, 3);

        var persistence = new FakeGamePersistence { Lobby = lobby };
        var sessionService = new FakeGameSessionService { Session = session };
        var presenceService = new FakeGamePresenceService { Presence = presence };
        using var services = CreateServices(persistence, sessionService, presenceService);

        var handler = services.GetRequiredService<IGetNexusPageHandler>();
        var result = await handler.HandleAsync(gameId, "user-1");

        Assert.NotNull(result);
        Assert.Equal(lobby, result!.Lobby);
        Assert.Equal(session, result.Session);
        Assert.Equal(presence, result.Presence);
    }

    [Fact]
    public async Task HandleAsync_repairs_missing_session_when_active_roster_has_two_players()
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
            [
                new GamePlayerView(CurrentPlayerId, "Captain Red", DateTime.UtcNow),
                new GamePlayerView(OpponentPlayerId, "Captain Blue", DateTime.UtcNow),
            ],
            true
        );

        var presence = new GamePresenceView([OpponentPlayerId]);
        var repairedSession = CreateSession(gameId, 1);

        var persistence = new FakeGamePersistence
        {
            Lobby = lobby,
            ActiveSessionPlayers = [CurrentPlayerId, OpponentPlayerId],
        };
        var sessionService = new FakeGameSessionService
        {
            SessionOutcomes = [new GameSessionUnavailable(), new GameSessionFound(repairedSession)],
        };
        var presenceService = new FakeGamePresenceService { Presence = presence };
        using var services = CreateServices(persistence, sessionService, presenceService);

        var handler = services.GetRequiredService<IGetNexusPageHandler>();
        var result = await handler.HandleAsync(gameId, "user-1");

        Assert.NotNull(result);
        Assert.Equal(repairedSession, result!.Session);
        Assert.Equal(presence, result.Presence);
        Assert.Equal(1, sessionService.InitializeCalls);
        Assert.Equal(2, sessionService.GetSessionCalls);
    }

    [Fact]
    public async Task HandleAsync_returns_null_session_when_missing_session_repair_fails()
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
            [
                new GamePlayerView(CurrentPlayerId, "Captain Red", DateTime.UtcNow),
                new GamePlayerView(OpponentPlayerId, "Captain Blue", DateTime.UtcNow),
            ],
            true
        );

        var persistence = new FakeGamePersistence
        {
            Lobby = lobby,
            ActiveSessionPlayers = [CurrentPlayerId, OpponentPlayerId],
        };
        var sessionService = new FakeGameSessionService
        {
            TryInitializeResult = false,
            SessionOutcomes = [new GameSessionUnavailable()],
        };
        var logger = new TestLogger<GetNexusPageHandler>();
        using var services = CreateServices(persistence, sessionService, logger: logger);

        var handler = services.GetRequiredService<IGetNexusPageHandler>();
        var result = await handler.HandleAsync(gameId, "user-1");

        Assert.NotNull(result);
        Assert.Null(result!.Session);
        Assert.Equal(1, sessionService.InitializeCalls);
        Assert.Equal(1, sessionService.GetSessionCalls);
        var warning = Assert.Single(logger.Entries, entry => entry.LogLevel == LogLevel.Warning);
        Assert.Contains(gameId.ToString(), warning.Message, StringComparison.Ordinal);
        Assert.Contains(CurrentPlayerId.ToString(), warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_reconciles_stale_opponent_when_database_roster_has_one_active_player()
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
            [new GamePlayerView(CurrentPlayerId, "Captain Red", DateTime.UtcNow)],
            true
        );
        var staleSession = CreateSession(gameId, 3, opponentIsActive: true);
        var reconciledSession = CreateSession(gameId, 3, opponentIsActive: false);

        var persistence = new FakeGamePersistence
        {
            Lobby = lobby,
            ActiveSessionPlayers = [CurrentPlayerId],
        };
        var sessionService = new FakeGameSessionService
        {
            SessionOutcomes =
            [
                new GameSessionFound(staleSession),
                new GameSessionFound(reconciledSession),
            ],
        };
        using var services = CreateServices(persistence, sessionService);

        var handler = services.GetRequiredService<IGetNexusPageHandler>();
        var result = await handler.HandleAsync(gameId, "user-1");

        Assert.NotNull(result);
        Assert.Equal(reconciledSession, result!.Session);
        Assert.Equal([(gameId, OpponentPlayerId)], sessionService.AbandonCalls);
        Assert.Equal(2, sessionService.GetSessionCalls);
        Assert.Equal(0, sessionService.InitializeCalls);
    }

    [Fact]
    public async Task HandleAsync_does_not_repair_session_for_former_player()
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
            [new GamePlayerView(OpponentPlayerId, "Captain Blue", DateTime.UtcNow)],
            false
        );

        var persistence = new FakeGamePersistence
        {
            Lobby = lobby,
            ActiveSessionPlayers = [OpponentPlayerId],
        };
        var sessionService = new FakeGameSessionService
        {
            SessionOutcomes = [new GameSessionUnavailable()],
        };
        using var services = CreateServices(persistence, sessionService);

        var handler = services.GetRequiredService<IGetNexusPageHandler>();
        var result = await handler.HandleAsync(gameId, "user-1");

        Assert.NotNull(result);
        Assert.Null(result!.Session);
        Assert.Equal(0, sessionService.InitializeCalls);
        Assert.Equal(1, sessionService.GetSessionCalls);
    }

    private static ServiceProvider CreateServices(
        FakeGamePersistence persistence,
        FakeGameSessionService sessionService,
        FakeGamePresenceService? presenceService = null,
        ILogger<GetNexusPageHandler>? logger = null
    )
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddSingleton<IGamePersistence>(persistence);
        services.AddSingleton<INexusSessionRosterProvider>(persistence);
        services.AddSingleton<INexusSessionService>(sessionService);
        services.AddSingleton<IGamePresenceService>(
            presenceService ?? new FakeGamePresenceService()
        );
        services.AddSingleton(Substitute.For<ILobbyInvalidationPublisher>());
        services.AddSingleton(Substitute.For<INexusSessionInvalidationPublisher>());
        services.AddSingleton(Substitute.For<IGameMessageInvalidationPublisher>());
        services.AddSingleton(Substitute.For<IGameMessagePersistence>());
        services.AddSingleton(logger ?? NullLogger<GetNexusPageHandler>.Instance);
        return services.BuildServiceProvider();
    }

    private static NexusGameView CreateSession(
        Guid gameId,
        int roundNumber,
        bool opponentIsActive = true
    )
    {
        var currentPlayer = new NexusPlayerView(
            CurrentPlayerId,
            NexusFactionColor.Red,
            0,
            NexusGateProgress.None,
            false,
            true,
            [],
            null,
            false,
            0,
            0
        );
        var opponentPlayer = new NexusPlayerView(
            OpponentPlayerId,
            NexusFactionColor.Blue,
            0,
            NexusGateProgress.None,
            false,
            opponentIsActive,
            null,
            null,
            false,
            0,
            0
        );

        return new NexusGameView(gameId, roundNumber, [], currentPlayer, opponentPlayer, [], null);
    }

    private sealed class FakeGamePersistence : IGamePersistence, INexusSessionRosterProvider
    {
        public GameLobbyView? Lobby { get; init; }

        public IReadOnlyList<Guid>? ActiveSessionPlayers { get; init; }

        public Task<Guid?> TryCreateGameAsync(
            CreateGamePersistenceRequest request,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<JoinGamePersistenceResult> JoinGameAsync(
            JoinGamePersistenceRequest request,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<LeaveGamePersistenceResult> LeaveGameAsync(
            Guid gameId,
            string userId,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<IReadOnlyList<Guid>?> GetActiveSessionPlayersAsync(
            Guid gameId,
            CancellationToken cancellationToken
        ) => Task.FromResult(ActiveSessionPlayers);

        public Task<GameLobbyView?> GetLobbyAsync(
            Guid gameId,
            string userId,
            CancellationToken cancellationToken
        ) => Task.FromResult(Lobby);

        public Task<UserGamesView> GetUserGamesAsync(
            string userId,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<IReadOnlyList<GamePlayerView>> GetActivePlayersAsync(
            Guid gameId,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();
    }

    private sealed class FakeGameSessionService : INexusSessionService
    {
        public NexusGameView? Session { get; set; }

        public IReadOnlyList<GameSessionOutcome>? SessionOutcomes { get; init; }

        public int InitializeCalls { get; private set; }

        public int GetSessionCalls { get; private set; }

        public List<(Guid GameId, Guid PlayerId)> AbandonCalls { get; } = [];

        private int _sessionOutcomeIndex;

        public bool TryInitializeResult { get; init; } = true;

        public Task<bool> EnsureSessionAsync(
            Guid gameId,
            IReadOnlyList<Guid> players,
            CancellationToken cancellationToken = default
        )
        {
            InitializeCalls++;
            return Task.FromResult(TryInitializeResult);
        }

        public Task<GameSessionOutcome> GetSessionAsync(
            Guid gameId,
            Guid playerId,
            CancellationToken cancellationToken = default
        )
        {
            GetSessionCalls++;

            if (SessionOutcomes is { Count: > 0 })
            {
                var index = Math.Min(_sessionOutcomeIndex, SessionOutcomes.Count - 1);
                _sessionOutcomeIndex++;
                return Task.FromResult(SessionOutcomes[index]);
            }

            return Task.FromResult<GameSessionOutcome>(
                Session is null ? new GameSessionUnavailable() : new GameSessionFound(Session)
            );
        }

        public Task<GameSessionCommandOutcome> SubmitOrdersAsync(
            Guid gameId,
            NexusTurnOrdersCommand command,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task AbandonAsync(
            Guid gameId,
            Guid playerId,
            CancellationToken cancellationToken = default
        )
        {
            AbandonCalls.Add((gameId, playerId));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGamePresenceService : IGamePresenceService
    {
        public GamePresenceView Presence { get; init; } = GamePresenceView.Empty;

        public Task<GamePresenceView> GetPresenceAsync(
            Guid gameId,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(Presence);

        public Task UpsertPresenceLeaseAsync(
            Guid gameId,
            Guid playerId,
            Guid connectionId,
            DateTime expiresAtUtc,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task RemovePresenceLeaseAsync(
            Guid gameId,
            Guid playerId,
            Guid connectionId,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
        }
    }

    private sealed record LogEntry(LogLevel LogLevel, string Message);
}

using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Spx.Game.Application;
using Spx.Game.Application.Features.GetGamePage;
using Spx.Game.Domain;
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
            [
                new GamePlayerView(CurrentPlayerId, "Captain Red", DateTime.UtcNow),
                new GamePlayerView(OpponentPlayerId, "Captain Blue", DateTime.UtcNow),
            ],
            true
        );

        var presence = new GamePresenceView([OpponentPlayerId]);

        var persistence = new FakeGamePersistence { Lobby = lobby };
        var sessionService = new FakeGameSessionService { Session = null };
        var presenceService = new FakeGamePresenceService { Presence = presence };
        using var services = CreateServices(persistence, sessionService, presenceService);

        var handler = services.GetRequiredService<IGetGamePageHandler>();
        var result = await handler.HandleAsync(gameId, "user-1");

        Assert.NotNull(result);
        Assert.Null(result!.Session);
        Assert.Equal(presence, result.Presence);
        Assert.Equal(0, sessionService.InitializeCalls);
    }

    private static ServiceProvider CreateServices(
        FakeGamePersistence persistence,
        FakeGameSessionService sessionService,
        FakeGamePresenceService? presenceService = null
    )
    {
        var services = new ServiceCollection();
        services.AddGameApplication();
        services.AddSingleton<IGamePersistence>(persistence);
        services.AddSingleton<IGameSessionService>(sessionService);
        services.AddSingleton<IGamePresenceService>(
            presenceService ?? new FakeGamePresenceService()
        );
        services.AddSingleton(Substitute.For<IGameLobbyInvalidationPublisher>());
        services.AddSingleton(Substitute.For<IGameSessionInvalidationPublisher>());
        services.AddSingleton(Substitute.For<IGameMessageInvalidationPublisher>());
        services.AddSingleton(Substitute.For<IGameMessagePersistence>());
        return services.BuildServiceProvider();
    }

    private static NexusGameView CreateSession(Guid gameId, int roundNumber)
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
            false
        );
        var opponentPlayer = new NexusPlayerView(
            OpponentPlayerId,
            NexusFactionColor.Blue,
            0,
            NexusGateProgress.None,
            false,
            true,
            null,
            null,
            false
        );

        return new NexusGameView(
            gameId,
            roundNumber,
            NexusGamePhase.Planning,
            [],
            currentPlayer,
            opponentPlayer,
            [],
            null
        );
    }

    private sealed class FakeGamePersistence : IGamePersistence
    {
        public GameLobbyView? Lobby { get; init; }

        public IReadOnlyList<GameSessionParticipant>? ActiveSessionPlayers { get; init; }

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

        public Task<IReadOnlyList<GameSessionParticipant>?> GetActiveSessionPlayersAsync(
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

    private sealed class FakeGameSessionService : IGameSessionService
    {
        public NexusGameView? Session { get; set; }

        public int InitializeCalls { get; private set; }

        public bool TryInitializeResult { get; init; } = true;

        public Task<bool> EnsureSessionAsync(
            Guid gameId,
            IReadOnlyList<GameSessionParticipant> players,
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
        ) =>
            Task.FromResult<GameSessionOutcome>(
                Session is null ? new GameSessionUnavailable() : new GameSessionFound(Session)
            );

        public Task<GameSessionCommandOutcome> SubmitOrdersAsync(
            Guid gameId,
            NexusTurnOrdersCommand command,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task AbandonAsync(
            Guid gameId,
            Guid playerId,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();
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
}

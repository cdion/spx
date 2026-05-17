using Microsoft.Extensions.DependencyInjection;
using Spx.Contracts;
using Spx.Game.Application;
using Spx.Game.Application.Features.EnsureGameSession;
using Xunit;

namespace Spx.Game.Application.Tests;

public sealed class EnsureGameSessionHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_false_when_active_roster_is_not_two_players()
    {
        var persistence = new FakeGamePersistence
        {
            ActiveSessionPlayers = [new GameSessionParticipant(Guid.NewGuid(), "user-1")]
        };
        var sessionService = new FakeGameSessionService();
        using var services = CreateServices(persistence, sessionService);

        var handler = services.GetRequiredService<IEnsureGameSessionHandler>();
        var result = await handler.HandleAsync(Guid.NewGuid());

        Assert.False(result);
        Assert.Empty(sessionService.InitializedGameIds);
    }

    [Fact]
    public async Task HandleAsync_calls_session_service_when_active_roster_has_two_players()
    {
        var gameId = Guid.NewGuid();
        var persistence = new FakeGamePersistence
        {
            ActiveSessionPlayers =
            [
                new GameSessionParticipant(Guid.NewGuid(), "user-1"),
                new GameSessionParticipant(Guid.NewGuid(), "user-2")
            ]
        };
        var sessionService = new FakeGameSessionService
        {
            TryInitializeResult = true
        };
        using var services = CreateServices(persistence, sessionService);

        var handler = services.GetRequiredService<IEnsureGameSessionHandler>();
        var result = await handler.HandleAsync(gameId);

        Assert.True(result);
        Assert.Equal([gameId], sessionService.InitializedGameIds);
    }

    private static ServiceProvider CreateServices(FakeGamePersistence persistence, FakeGameSessionService sessionService)
    {
        var services = new ServiceCollection();
        services.AddGameApplication();
        services.AddSingleton<IGamePersistence>(persistence);
        services.AddSingleton<IGameSessionService>(sessionService);
        services.AddSingleton<IGamePresenceService, StubGamePresenceService>();
        services.AddSingleton<IGameLobbyInvalidationPublisher, StubGameLobbyEventsPublisher>();
        services.AddSingleton<IGameSessionInvalidationPublisher, StubGameSessionInvalidationPublisher>();
        services.AddSingleton<IGameMessageInvalidationPublisher, StubGameMessageEventsPublisher>();
        services.AddSingleton<IGameMessagePersistence, StubGameMessagePersistence>();
        return services.BuildServiceProvider();
    }

    private sealed class FakeGamePersistence : IGamePersistence
    {
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
            => throw new NotSupportedException();

        public Task<UserGamesView> GetUserGamesAsync(string userId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class FakeGameSessionService : IGameSessionService
    {
        public List<Guid> InitializedGameIds { get; } = [];

        public bool TryInitializeResult { get; init; }

        public Task<bool> EnsureSessionAsync(Guid gameId, IReadOnlyList<GameSessionParticipant> players, CancellationToken cancellationToken = default)
        {
            InitializedGameIds.Add(gameId);
            return Task.FromResult(TryInitializeResult);
        }

        public Task<GameSessionSnapshot?> GetSessionAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task AcknowledgeGameplayEventBatchAsync(Guid gameId, Guid gameplayEventBatchId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GameSessionCommandOutcome> SubmitAcquireAsync(Guid gameId, SubmitAcquireRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GameSessionCommandOutcome> SubmitPlayBatchAsync(Guid gameId, SubmitPlayBatchRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GameSessionSnapshot> AbandonAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class StubGamePresenceService : IGamePresenceService
    {
        public Task<GamePresenceView> GetPresenceAsync(Guid gameId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

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
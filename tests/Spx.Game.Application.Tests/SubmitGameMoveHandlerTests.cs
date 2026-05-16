using Microsoft.Extensions.DependencyInjection;
using Spx.Contracts;
using Spx.Game.Application;
using Spx.Game.Application.Features.SubmitAcquireCard;
using Spx.Game.Application.Features.SubmitPlayBatch;
using Xunit;

namespace Spx.Game.Application.Tests;

public sealed class GameSessionCommandHandlerTests
{
    [Fact]
    public async Task SubmitAcquire_returns_failure_when_session_service_rejects_choice()
    {
        var sessionService = new FakeGameSessionService
        {
            AcquireResult = new GameSessionCommandFailed("The selected market card is no longer available.")
        };
        using var services = CreateServices(sessionService);

        var handler = services.GetRequiredService<ISubmitAcquireCardHandler>();
        var result = await handler.HandleAsync(Guid.NewGuid(), "user-1", 2, Guid.NewGuid());

        var failed = Assert.IsType<GameSessionCommandFailed>(result);
        Assert.Equal("The selected market card is no longer available.", failed.ErrorMessage);
    }

    [Fact]
    public async Task SubmitAcquire_returns_session_and_publishes_invalidation_on_success()
    {
        var session = CreateSession();
        var sessionService = new FakeGameSessionService
        {
            AcquireResult = new GameSessionCommandSucceeded(session)
        };
        var invalidationPublisher = new FakeGameSessionInvalidationPublisher();
        using var services = CreateServices(sessionService, invalidationPublisher);

        var handler = services.GetRequiredService<ISubmitAcquireCardHandler>();
        var result = await handler.HandleAsync(session.GameId, "user-1", session.RoundNumber, Guid.NewGuid());

        var succeeded = Assert.IsType<GameSessionCommandSucceeded>(result);
        Assert.Equal(session, succeeded.Session);
        Assert.Equal(session.GameId, invalidationPublisher.PublishedGameId);
    }

    [Fact]
    public async Task SubmitPlayBatch_returns_session_and_publishes_invalidation_on_success()
    {
        var session = CreateSession(
            phase: GamePhase.Play,
            canLockBatch: true,
            lastResolvedBatch: new GameResolvedBatchView(
                1,
                [
                    new GameResolvedPlayerBatchView(new GameSessionParticipantView(Guid.NewGuid(), "user-1"), [], false),
                    new GameResolvedPlayerBatchView(new GameSessionParticipantView(Guid.NewGuid(), "user-2"), [], false)
                ],
                DateTime.UtcNow));
        var gameplayEvents = new[] { "user-1 passed.", "user-2 passed." };
        var sessionService = new FakeGameSessionService
        {
            PlayBatchResult = new GameSessionCommandSucceeded(session, gameplayEvents)
        };
        var invalidationPublisher = new FakeGameSessionInvalidationPublisher();
        var messagePublisher = new FakeGameMessageInvalidationPublisher();
        var gameplayEventWriter = new FakeGameplayEventMessageWriter { PersistResult = 1 };
        using var services = CreateServices(sessionService, invalidationPublisher, messagePublisher, gameplayEventWriter);

        var handler = services.GetRequiredService<ISubmitPlayBatchHandler>();
        var result = await handler.HandleAsync(
            session.GameId,
            "user-1",
            session.RoundNumber,
            [new GameBatchCardCommand(Guid.NewGuid(), GameResourceColor.Red, null, null, null, [])]);

        var succeeded = Assert.IsType<GameSessionCommandSucceeded>(result);
        Assert.Equal(session, succeeded.Session);
        Assert.Equal(session.GameId, invalidationPublisher.PublishedGameId);
        Assert.Equal(session.GameId, messagePublisher.PublishedGameId);
        Assert.Equal(session, gameplayEventWriter.LastSession);
        Assert.Equal(gameplayEvents, gameplayEventWriter.LastGameplayEvents);
    }

    private static ServiceProvider CreateServices(
        FakeGameSessionService sessionService,
        FakeGameSessionInvalidationPublisher? invalidationPublisher = null,
        FakeGameMessageInvalidationPublisher? messagePublisher = null,
        FakeGameplayEventMessageWriter? gameplayEventWriter = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGameApplication();
        services.AddSingleton<IGameSessionService>(sessionService);
        services.AddSingleton<IGameplayEventMessageWriter>(gameplayEventWriter ?? new FakeGameplayEventMessageWriter());
        services.AddSingleton<IGamePersistence, StubGamePersistence>();
        services.AddSingleton<IGameLobbyInvalidationPublisher, StubGameLobbyEventsPublisher>();
        services.AddSingleton<IGameSessionInvalidationPublisher>(invalidationPublisher ?? new FakeGameSessionInvalidationPublisher());
        services.AddSingleton<IGameMessageInvalidationPublisher>(messagePublisher ?? new FakeGameMessageInvalidationPublisher());
        services.AddSingleton<IGameMessagePersistence, StubGameMessagePersistence>();
        return services.BuildServiceProvider();
    }

    private static GameSessionView CreateSession(GamePhase phase = GamePhase.Acquire, bool canLockBatch = false, GameResolvedBatchView? lastResolvedBatch = null)
    {
        var currentPlayer = new GameSessionParticipantView(Guid.NewGuid(), "user-1");
        var opponentPlayer = new GameSessionParticipantView(Guid.NewGuid(), "user-2");

        return new GameSessionView(
            Guid.NewGuid(),
            1,
            phase,
            new GamePlayerStateView(currentPlayer, [], false, 0, 0, false, phase == GamePhase.Acquire, []),
            new GamePlayerStateView(opponentPlayer, [], false, 0, 0, false, phase != GamePhase.Acquire, []),
            [],
            0,
            false,
            phase == GamePhase.Acquire,
            canLockBatch,
            GameCardCatalog.MaxBatchSize,
                lastResolvedBatch,
            null);
    }

    private sealed class FakeGameSessionService : IGameSessionService
    {
        public GameSessionCommandOutcome AcquireResult { get; init; } = new GameSessionCommandFailed("Acquire failed.");

        public GameSessionCommandOutcome PlayBatchResult { get; init; } = new GameSessionCommandFailed("Play batch failed.");

        public Task<bool> EnsureSessionAsync(Guid gameId, IReadOnlyList<GameSessionParticipantView> players, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<GameSessionView?> GetSessionViewAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GameSessionCommandOutcome> SubmitAcquireAsync(Guid gameId, SubmitAcquireCardCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(AcquireResult);

        public Task<GameSessionCommandOutcome> SubmitPlayBatchAsync(Guid gameId, SubmitPlayBatchCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(PlayBatchResult);

        public Task<GameSessionView> AbandonAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class StubGamePersistence : IGamePersistence
    {
        public Task<Guid?> TryCreateGameAsync(CreateGamePersistenceRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<JoinGamePersistenceResult> JoinGameAsync(JoinGamePersistenceRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<LeaveGamePersistenceResult> LeaveGameAsync(Guid gameId, string userId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<GameSessionParticipantView>?> GetActiveSessionPlayersAsync(Guid gameId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<GameLobbyView?> GetLobbyAsync(Guid gameId, string userId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<UserGamesView> GetUserGamesAsync(string userId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class StubGameLobbyEventsPublisher : IGameLobbyInvalidationPublisher
    {
        public Task PublishLobbyInvalidatedAsync(Guid gameId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeGameSessionInvalidationPublisher : IGameSessionInvalidationPublisher
    {
        public Guid? PublishedGameId { get; private set; }

        public Task PublishSessionInvalidatedAsync(Guid gameId, CancellationToken cancellationToken = default)
        {
            PublishedGameId = gameId;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGameMessageInvalidationPublisher : IGameMessageInvalidationPublisher
    {
        public Guid? PublishedGameId { get; private set; }

        public Task PublishMessagesInvalidatedAsync(Guid gameId, CancellationToken cancellationToken = default)
        {
            PublishedGameId = gameId;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGameplayEventMessageWriter : IGameplayEventMessageWriter
    {
        public int PersistResult { get; init; }

        public GameSessionView? LastSession { get; private set; }

        public IReadOnlyList<string>? LastGameplayEvents { get; private set; }

        public Task<int> PersistResolvedBatchAsync(GameSessionView session, IReadOnlyList<string> gameplayEvents, CancellationToken cancellationToken = default)
        {
            LastSession = session;
            LastGameplayEvents = gameplayEvents;
            return Task.FromResult(PersistResult);
        }
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
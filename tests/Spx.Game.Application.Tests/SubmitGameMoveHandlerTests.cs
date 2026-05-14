using Microsoft.Extensions.DependencyInjection;
using Spx.Contracts;
using Spx.Game.Application;
using Spx.Game.Application.Features.SubmitGameMove;
using Xunit;

namespace Spx.Game.Application.Tests;

public sealed class SubmitGameMoveHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_failure_when_session_service_rejects_move()
    {
        var sessionService = new FakeGameSessionService
        {
            SubmitResult = new SubmitGameMoveFailed("The submitted move does not match the current round.")
        };
        using var services = CreateServices(sessionService);

        var handler = services.GetRequiredService<ISubmitGameMoveHandler>();
        var result = await handler.HandleAsync(Guid.NewGuid(), "user-1", 2, GameMove.Bluon);

        var failed = Assert.IsType<SubmitGameMoveFailed>(result);
        Assert.Equal("The submitted move does not match the current round.", failed.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_returns_session_on_success()
    {
        var session = new GameSessionView(
            Guid.NewGuid(),
            4,
            new GameSessionParticipantView(Guid.NewGuid(), "user-1"),
            new GameSessionParticipantView(Guid.NewGuid(), "user-2"),
            true,
            true,
            null);

        var sessionService = new FakeGameSessionService
        {
            SubmitResult = new SubmitGameMoveSucceeded(session)
        };
        using var services = CreateServices(sessionService);

        var handler = services.GetRequiredService<ISubmitGameMoveHandler>();
        var result = await handler.HandleAsync(session.GameId, "user-1", expectedRoundNumber: 4, GameMove.Redite);

        var succeeded = Assert.IsType<SubmitGameMoveSucceeded>(result);
        Assert.Equal(session, succeeded.Session);
    }

    private static ServiceProvider CreateServices(FakeGameSessionService sessionService)
    {
        var services = new ServiceCollection();
        services.AddGameApplication();
        services.AddSingleton<IGameSessionService>(sessionService);
        services.AddSingleton<IGamePersistence, StubGamePersistence>();
        services.AddSingleton<IGameLobbyEventsPublisher, StubGameLobbyEventsPublisher>();
        services.AddSingleton<IGameMessageEventsPublisher, StubGameMessageEventsPublisher>();
        services.AddSingleton<IGameMessagePersistence, StubGameMessagePersistence>();
        return services.BuildServiceProvider();
    }

    private sealed class FakeGameSessionService : IGameSessionService
    {
        public SubmitGameMoveOutcome SubmitResult { get; init; }
            = new SubmitGameMoveSucceeded(
                new GameSessionView(
                    Guid.NewGuid(),
                    1,
                    new GameSessionParticipantView(Guid.NewGuid(), "user-1"),
                    new GameSessionParticipantView(Guid.NewGuid(), "user-2"),
                    false,
                    false,
                    null));

        public Task<bool> EnsureSessionAsync(Guid gameId, IReadOnlyList<GameSessionParticipantView> players, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<GameSessionView?> GetSessionViewAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<SubmitGameMoveOutcome> SubmitMoveAsync(Guid gameId, SubmitGameMoveCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(SubmitResult);

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

    private sealed class StubGameLobbyEventsPublisher : IGameLobbyEventsPublisher
    {
        public Task PublishLobbyChangedAsync(Guid gameId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class StubGameMessageEventsPublisher : IGameMessageEventsPublisher
    {
        public Task PublishMessagesChangedAsync(Guid gameId, CancellationToken cancellationToken = default)
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
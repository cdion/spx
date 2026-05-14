using Microsoft.Extensions.DependencyInjection;
using Spx.Contracts;
using Spx.Game.Application;
using Spx.Game.Application.Features.GetGameSession;
using Xunit;

namespace Spx.Game.Application.Tests;

public sealed class GetGameSessionHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_session_when_available()
    {
        var session = new GameSessionView(
            Guid.NewGuid(),
            2,
            new GameSessionParticipantView(Guid.NewGuid(), "user-1"),
            new GameSessionParticipantView(Guid.NewGuid(), "user-2"),
            false,
            true,
            null);

        var sessionService = new FakeGameSessionService { Session = session };
        using var services = CreateServices(sessionService);

        var handler = services.GetRequiredService<IGetGameSessionHandler>();
        var result = await handler.HandleAsync(session.GameId, "user-1");

        Assert.Equal(session, result);
    }

    [Fact]
    public async Task HandleAsync_returns_null_when_session_is_unavailable()
    {
        using var services = CreateServices(new FakeGameSessionService());

        var handler = services.GetRequiredService<IGetGameSessionHandler>();
        var result = await handler.HandleAsync(Guid.NewGuid(), "user-1");

        Assert.Null(result);
    }

    private static ServiceProvider CreateServices(FakeGameSessionService sessionService)
    {
        var services = new ServiceCollection();
        services.AddGameApplication();
        services.AddSingleton<IGameSessionService>(sessionService);
        services.AddSingleton<IGamePersistence, StubGamePersistence>();
        services.AddSingleton<IGameLobbyInvalidationPublisher, StubGameLobbyInvalidationPublisher>();
        services.AddSingleton<IGameSessionInvalidationPublisher, StubGameSessionInvalidationPublisher>();
        services.AddSingleton<IGameMessageInvalidationPublisher, StubGameMessageInvalidationPublisher>();
        services.AddSingleton<IGameMessagePersistence, StubGameMessagePersistence>();
        return services.BuildServiceProvider();
    }

    private sealed class FakeGameSessionService : IGameSessionService
    {
        public GameSessionView? Session { get; init; }

        public Task<bool> EnsureSessionAsync(Guid gameId, IReadOnlyList<GameSessionParticipantView> players, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GameSessionView?> GetSessionViewAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(Session);

        public Task<SubmitGameMoveOutcome> SubmitMoveAsync(Guid gameId, SubmitGameMoveCommand command, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

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

    private sealed class StubGameLobbyInvalidationPublisher : IGameLobbyInvalidationPublisher
    {
        public Task PublishLobbyInvalidatedAsync(Guid gameId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class StubGameSessionInvalidationPublisher : IGameSessionInvalidationPublisher
    {
        public Task PublishSessionInvalidatedAsync(Guid gameId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class StubGameMessageInvalidationPublisher : IGameMessageInvalidationPublisher
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
using Microsoft.Extensions.DependencyInjection;
using Spx.Contracts;
using Spx.Games;
using Spx.Games.Features.GetGamePage;
using Xunit;

namespace Spx.Games.Tests;

public sealed class GetGamePageHandlerTests
{
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
        var lobby = new GameLobbyView(
            gameId,
            "Arena",
            "ABC123",
            GameStatus.Open,
            2,
            DateTime.UtcNow,
            null,
            "Captain Red",
            [new GamePlayerView(Guid.NewGuid(), "Captain Red", DateTime.UtcNow, true), new GamePlayerView(Guid.NewGuid(), "Captain Blue", DateTime.UtcNow, false)],
            true);

        var session = new GameSessionView(
            gameId,
            3,
            new GameSessionParticipantView(Guid.NewGuid(), "user-1", "Captain Red"),
            new GameSessionParticipantView(Guid.NewGuid(), "user-2", "Captain Blue"),
            true,
            true,
            null);

        var persistence = new FakeGamePersistence { Lobby = lobby };
        var sessionService = new FakeGameSessionService { Session = session };
        using var services = CreateServices(persistence, sessionService);

        var handler = services.GetRequiredService<IGetGamePageHandler>();
        var result = await handler.HandleAsync(gameId, "user-1");

        Assert.NotNull(result);
        Assert.Equal(lobby, result!.Lobby);
        Assert.Equal(session, result.Session);
    }

    private static ServiceProvider CreateServices(FakeGamePersistence persistence, FakeGameSessionService sessionService)
    {
        var services = new ServiceCollection();
        services.AddGameApplication();
        services.AddSingleton<IGamePersistence>(persistence);
        services.AddSingleton<IGameSessionService>(sessionService);
        services.AddSingleton<IGameLobbyEventsPublisher, StubGameLobbyEventsPublisher>();
        services.AddSingleton<IGameMessageEventsPublisher, StubGameMessageEventsPublisher>();
        services.AddSingleton<IGameMessagePersistence, StubGameMessagePersistence>();
        return services.BuildServiceProvider();
    }

    private sealed class FakeGamePersistence : IGamePersistence
    {
        public GameLobbyView? Lobby { get; init; }

        public Task<Guid?> TryCreateGameAsync(CreateGamePersistenceRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<JoinGamePersistenceResult> JoinGameAsync(JoinGamePersistenceRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<LeaveGamePersistenceResult> LeaveGameAsync(Guid gameId, string userId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<GameSessionParticipantView>?> GetActiveSessionPlayersAsync(Guid gameId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<GameLobbyView?> GetLobbyAsync(Guid gameId, string userId, CancellationToken cancellationToken)
            => Task.FromResult(Lobby);

        public Task<UserGamesView> GetUserGamesAsync(string userId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class FakeGameSessionService : IGameSessionService
    {
        public GameSessionView? Session { get; init; }

        public Task<bool> TryInitializeAsync(Guid gameId, IReadOnlyList<GameSessionParticipantView> players, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<GameSessionView?> GetSessionViewAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(Session);

        public Task<GameSessionView> SubmitMoveAsync(Guid gameId, SubmitGameMoveCommand command, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GameSessionView> AbandonAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
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

        public Task<GameMessageCommandResult> SendPublicMessageAsync(Guid gameId, string userId, string body, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<GameMessageCommandResult> SendPrivateMessageAsync(Guid gameId, string userId, Guid recipientPlayerId, string body, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<GameMessageCommandResult> EditMessageAsync(Guid gameId, string userId, Guid messageId, string body, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<GameMessageCommandResult> DeleteMessageAsync(Guid gameId, string userId, Guid messageId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}
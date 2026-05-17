using Microsoft.Extensions.DependencyInjection;
using Spx.Contracts;
using Spx.Game.Application;
using Spx.Game.Application.Features.GetGamePresence;
using Xunit;

namespace Spx.Game.Application.Tests;

public sealed class GetGamePresenceHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_presence_from_service()
    {
        var presence = new GamePresenceView([Guid.NewGuid()]);
        using var services = CreateServices(new FakeGamePresenceService { Presence = presence });

        var handler = services.GetRequiredService<IGetGamePresenceHandler>();
        var result = await handler.HandleAsync(Guid.NewGuid());

        Assert.Equal(presence, result);
    }

    private static ServiceProvider CreateServices(FakeGamePresenceService presenceService)
    {
        var services = new ServiceCollection();
        services.AddGameApplication();
        services.AddSingleton<IGamePresenceService>(presenceService);
        services.AddSingleton<IGameSessionService, StubGameSessionService>();
        services.AddSingleton<IGamePersistence, StubGamePersistence>();
        services.AddSingleton<IGameLobbyInvalidationPublisher, StubGameLobbyInvalidationPublisher>();
        services.AddSingleton<IGameSessionInvalidationPublisher, StubGameSessionInvalidationPublisher>();
        services.AddSingleton<IGameMessageInvalidationPublisher, StubGameMessageInvalidationPublisher>();
        services.AddSingleton<IGameMessagePersistence, StubGameMessagePersistence>();
        return services.BuildServiceProvider();
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

    private sealed class StubGameSessionService : IGameSessionService
    {
        public Task<bool> EnsureSessionAsync(Guid gameId, IReadOnlyList<GameSessionParticipant> players, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task AcknowledgeGameplayEventBatchAsync(Guid gameId, Guid gameplayEventBatchId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GameSessionSnapshot?> GetSessionAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GameSessionCommandOutcome> SubmitAcquireAsync(Guid gameId, SubmitAcquireRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GameSessionCommandOutcome> SubmitPlayBatchAsync(Guid gameId, SubmitPlayBatchRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GameSessionSnapshot> AbandonAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
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

        public Task<IReadOnlyList<GameSessionParticipant>?> GetActiveSessionPlayersAsync(Guid gameId, CancellationToken cancellationToken)
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
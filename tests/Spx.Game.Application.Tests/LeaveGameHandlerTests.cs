using Microsoft.Extensions.DependencyInjection;
using Spx.Game.Application;
using Spx.Game.Application.Features.LeaveGame;
using Xunit;

namespace Spx.Game.Application.Tests;

public sealed class LeaveGameHandlerTests
{
    [Fact]
    public async Task HandleAsync_publishes_lobby_and_messages_when_leave_changes_state()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var persistence = Substitute.For<IGamePersistence>();
        persistence
            .LeaveGameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LeaveGamePersistenceResult(new GameCommandSucceeded(gameId), playerId));
        var sessionService = Substitute.For<INexusSessionService>();
        var lobbyPublisher = Substitute.For<ILobbyInvalidationPublisher>();
        var messagePublisher = Substitute.For<IGameMessageInvalidationPublisher>();
        using var services = CreateServices(
            persistence,
            sessionService,
            lobbyPublisher,
            messagePublisher
        );

        var handler = services.GetRequiredService<ILeaveGameHandler>();
        var result = await handler.HandleAsync(gameId, "user-1");

        Assert.IsType<GameCommandSucceeded>(result);
        await persistence
            .Received(1)
            .LeaveGameAsync(gameId, "user-1", Arg.Any<CancellationToken>());
        await sessionService
            .Received(1)
            .AbandonAsync(gameId, playerId, Arg.Any<CancellationToken>());
        await lobbyPublisher
            .Received(1)
            .PublishLobbyInvalidatedAsync(gameId, Arg.Any<CancellationToken>());
        await messagePublisher
            .Received(1)
            .PublishMessagesInvalidatedAsync(gameId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_still_publishes_invalidations_when_abandon_throws()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var persistence = Substitute.For<IGamePersistence>();
        persistence
            .LeaveGameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LeaveGamePersistenceResult(new GameCommandSucceeded(gameId), playerId));
        var sessionService = Substitute.For<INexusSessionService>();
        sessionService
            .AbandonAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("boom")));
        var lobbyPublisher = Substitute.For<ILobbyInvalidationPublisher>();
        var messagePublisher = Substitute.For<IGameMessageInvalidationPublisher>();
        using var services = CreateServices(
            persistence,
            sessionService,
            lobbyPublisher,
            messagePublisher
        );

        var handler = services.GetRequiredService<ILeaveGameHandler>();
        var result = await handler.HandleAsync(gameId, "user-1");

        Assert.IsType<GameCommandSucceeded>(result);
        await sessionService
            .Received(1)
            .AbandonAsync(gameId, playerId, Arg.Any<CancellationToken>());
        await lobbyPublisher
            .Received(1)
            .PublishLobbyInvalidatedAsync(gameId, Arg.Any<CancellationToken>());
        await messagePublisher
            .Received(1)
            .PublishMessagesInvalidatedAsync(gameId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_does_not_abandon_or_publish_when_leave_result_is_unchanged()
    {
        var gameId = Guid.NewGuid();
        var persistence = Substitute.For<IGamePersistence>();
        persistence
            .LeaveGameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(
                new LeaveGamePersistenceResult(
                    new GameCommandFailed("You are not an active player in this game.")
                )
            );
        var sessionService = Substitute.For<INexusSessionService>();
        var lobbyPublisher = Substitute.For<ILobbyInvalidationPublisher>();
        var messagePublisher = Substitute.For<IGameMessageInvalidationPublisher>();
        using var services = CreateServices(
            persistence,
            sessionService,
            lobbyPublisher,
            messagePublisher
        );

        var handler = services.GetRequiredService<ILeaveGameHandler>();
        var result = await handler.HandleAsync(gameId, "user-1");

        var failed = Assert.IsType<GameCommandFailed>(result);
        Assert.Equal("You are not an active player in this game.", failed.ErrorMessage);
        await sessionService
            .DidNotReceive()
            .AbandonAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await lobbyPublisher
            .DidNotReceive()
            .PublishLobbyInvalidatedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await messagePublisher
            .DidNotReceive()
            .PublishMessagesInvalidatedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_does_not_abandon_or_publish_when_player_id_is_absent()
    {
        var gameId = Guid.NewGuid();
        var persistence = Substitute.For<IGamePersistence>();
        persistence
            .LeaveGameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new LeaveGamePersistenceResult(new GameCommandSucceeded(gameId)));
        var sessionService = Substitute.For<INexusSessionService>();
        var lobbyPublisher = Substitute.For<ILobbyInvalidationPublisher>();
        var messagePublisher = Substitute.For<IGameMessageInvalidationPublisher>();
        using var services = CreateServices(
            persistence,
            sessionService,
            lobbyPublisher,
            messagePublisher
        );

        var handler = services.GetRequiredService<ILeaveGameHandler>();
        var result = await handler.HandleAsync(gameId, "user-1");

        Assert.IsType<GameCommandSucceeded>(result);
        await sessionService
            .DidNotReceive()
            .AbandonAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await lobbyPublisher
            .DidNotReceive()
            .PublishLobbyInvalidatedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await messagePublisher
            .DidNotReceive()
            .PublishMessagesInvalidatedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static ServiceProvider CreateServices(
        IGamePersistence persistence,
        INexusSessionService sessionService,
        ILobbyInvalidationPublisher lobbyPublisher,
        IGameMessageInvalidationPublisher messagePublisher
    )
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationServices();
        services.AddSingleton(persistence);
        services.AddSingleton(sessionService);
        services.AddSingleton(lobbyPublisher);
        services.AddSingleton(Substitute.For<INexusSessionInvalidationPublisher>());
        services.AddSingleton(messagePublisher);
        services.AddSingleton(Substitute.For<IGameMessagePersistence>());
        return services.BuildServiceProvider();
    }
}

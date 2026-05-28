using Microsoft.Extensions.DependencyInjection;
using Spx.Nexus.Application;
using Spx.Nexus.Application.Features.JoinGame;
using Xunit;

namespace Spx.Nexus.Application.Tests;

public sealed class JoinGameHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_validation_failure_for_short_invite_code()
    {
        var persistence = Substitute.For<IGamePersistence>();
        var lobbyPublisher = Substitute.For<ILobbyInvalidationPublisher>();
        var messagePublisher = Substitute.For<IGameMessageInvalidationPublisher>();
        var sessionService = Substitute.For<INexusSessionService>();
        using var services = CreateServices(
            persistence,
            lobbyPublisher,
            messagePublisher,
            sessionService
        );

        var handler = services.GetRequiredService<IJoinGameHandler>();
        var result = await handler.HandleAsync("user-1", new JoinGameRequest("abc", "Captain Red"));

        var failed = Assert.IsType<GameCommandFailed>(result);
        Assert.Equal("Invite codes must be six characters long.", failed.ErrorMessage);
        await persistence
            .DidNotReceive()
            .JoinGameAsync(Arg.Any<JoinGamePersistenceRequest>(), Arg.Any<CancellationToken>());
        await sessionService
            .DidNotReceive()
            .EnsureSessionAsync(
                Arg.Any<Guid>(),
                Arg.Any<IReadOnlyList<GameSessionParticipant>>(),
                Arg.Any<CancellationToken>()
            );
        await lobbyPublisher
            .DidNotReceive()
            .PublishLobbyInvalidatedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await messagePublisher
            .DidNotReceive()
            .PublishMessagesInvalidatedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_publishes_lobby_and_messages_when_persistence_requests_both()
    {
        var gameId = Guid.NewGuid();
        var activePlayers = new GameSessionParticipant[]
        {
            new(Guid.NewGuid()),
            new(Guid.NewGuid()),
        };
        var persistence = Substitute.For<IGamePersistence>();
        persistence
            .JoinGameAsync(Arg.Any<JoinGamePersistenceRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                new JoinGamePersistenceResult(
                    new GameCommandSucceeded(gameId),
                    LobbyGameId: gameId,
                    MessagesGameId: gameId
                )
            );
        persistence
            .GetActiveSessionPlayersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<GameSessionParticipant>?>(activePlayers);
        var lobbyPublisher = Substitute.For<ILobbyInvalidationPublisher>();
        var messagePublisher = Substitute.For<IGameMessageInvalidationPublisher>();
        var sessionService = Substitute.For<INexusSessionService>();
        sessionService
            .EnsureSessionAsync(
                Arg.Any<Guid>(),
                Arg.Any<IReadOnlyList<GameSessionParticipant>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(true);
        using var services = CreateServices(
            persistence,
            lobbyPublisher,
            messagePublisher,
            sessionService
        );

        var handler = services.GetRequiredService<IJoinGameHandler>();
        var result = await handler.HandleAsync(
            "user-1",
            new JoinGameRequest(" abc123 ", " Captain Red ")
        );

        Assert.IsType<GameCommandSucceeded>(result);
        await persistence
            .Received(1)
            .JoinGameAsync(
                Arg.Is<JoinGamePersistenceRequest>(r =>
                    r.InviteCode == "ABC123"
                    && r.PlayerName == "Captain Red"
                    && r.PlayerNameLookup == "CAPTAIN RED"
                ),
                Arg.Any<CancellationToken>()
            );
        await sessionService
            .Received(1)
            .EnsureSessionAsync(
                gameId,
                Arg.Any<IReadOnlyList<GameSessionParticipant>>(),
                Arg.Any<CancellationToken>()
            );
        await lobbyPublisher
            .Received(1)
            .PublishLobbyInvalidatedAsync(gameId, Arg.Any<CancellationToken>());
        await messagePublisher
            .Received(1)
            .PublishMessagesInvalidatedAsync(gameId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_keeps_join_success_and_publishes_when_session_initialization_fails()
    {
        var gameId = Guid.NewGuid();
        var activePlayers = new GameSessionParticipant[]
        {
            new(Guid.NewGuid()),
            new(Guid.NewGuid()),
        };
        var persistence = Substitute.For<IGamePersistence>();
        persistence
            .JoinGameAsync(Arg.Any<JoinGamePersistenceRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                new JoinGamePersistenceResult(
                    new GameCommandSucceeded(gameId),
                    LobbyGameId: gameId,
                    MessagesGameId: gameId
                )
            );
        persistence
            .GetActiveSessionPlayersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<GameSessionParticipant>?>(activePlayers);
        var lobbyPublisher = Substitute.For<ILobbyInvalidationPublisher>();
        var messagePublisher = Substitute.For<IGameMessageInvalidationPublisher>();
        var sessionService = Substitute.For<INexusSessionService>();
        sessionService
            .EnsureSessionAsync(
                Arg.Any<Guid>(),
                Arg.Any<IReadOnlyList<GameSessionParticipant>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(false);
        using var services = CreateServices(
            persistence,
            lobbyPublisher,
            messagePublisher,
            sessionService
        );

        var handler = services.GetRequiredService<IJoinGameHandler>();
        var result = await handler.HandleAsync(
            "user-1",
            new JoinGameRequest("ABC123", "Captain Red")
        );

        var succeeded = Assert.IsType<GameCommandSucceeded>(result);
        Assert.Equal(gameId, succeeded.GameId);
        await sessionService
            .Received(1)
            .EnsureSessionAsync(
                gameId,
                Arg.Any<IReadOnlyList<GameSessionParticipant>>(),
                Arg.Any<CancellationToken>()
            );
        await lobbyPublisher
            .Received(1)
            .PublishLobbyInvalidatedAsync(gameId, Arg.Any<CancellationToken>());
        await messagePublisher
            .Received(1)
            .PublishMessagesInvalidatedAsync(gameId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_does_not_publish_when_persistence_returns_failure_without_game_id()
    {
        var persistence = Substitute.For<IGamePersistence>();
        persistence
            .JoinGameAsync(Arg.Any<JoinGamePersistenceRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                new JoinGamePersistenceResult(
                    new GameCommandFailed("That player name is already taken in this game.")
                )
            );
        var lobbyPublisher = Substitute.For<ILobbyInvalidationPublisher>();
        var messagePublisher = Substitute.For<IGameMessageInvalidationPublisher>();
        var sessionService = Substitute.For<INexusSessionService>();
        using var services = CreateServices(
            persistence,
            lobbyPublisher,
            messagePublisher,
            sessionService
        );

        var handler = services.GetRequiredService<IJoinGameHandler>();
        var result = await handler.HandleAsync(
            "user-3",
            new JoinGameRequest("ABC123", "Captain Red")
        );

        var failed = Assert.IsType<GameCommandFailed>(result);
        Assert.Equal("That player name is already taken in this game.", failed.ErrorMessage);
        await sessionService
            .DidNotReceive()
            .EnsureSessionAsync(
                Arg.Any<Guid>(),
                Arg.Any<IReadOnlyList<GameSessionParticipant>>(),
                Arg.Any<CancellationToken>()
            );
        await lobbyPublisher
            .DidNotReceive()
            .PublishLobbyInvalidatedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await messagePublisher
            .DidNotReceive()
            .PublishMessagesInvalidatedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static ServiceProvider CreateServices(
        IGamePersistence persistence,
        ILobbyInvalidationPublisher lobbyPublisher,
        IGameMessageInvalidationPublisher messagePublisher,
        INexusSessionService sessionService
    )
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddSingleton(persistence);
        services.AddSingleton(sessionService);
        services.AddSingleton(lobbyPublisher);
        services.AddSingleton(messagePublisher);
        services.AddSingleton(Substitute.For<IGameMessagePersistence>());
        return services.BuildServiceProvider();
    }
}

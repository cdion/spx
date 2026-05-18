using Microsoft.Extensions.DependencyInjection;
using Spx.Game.Application;
using Spx.Game.Application.Features.CreateGame;
using Xunit;

namespace Spx.Game.Application.Tests;

public sealed class CreateGameHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_validation_failure_for_short_game_name()
    {
        var persistence = Substitute.For<IGamePersistence>();
        var lobbyPublisher = Substitute.For<IGameLobbyInvalidationPublisher>();
        var messagePublisher = Substitute.For<IGameMessageInvalidationPublisher>();
        using var services = CreateServices(persistence, lobbyPublisher, messagePublisher);

        var handler = services.GetRequiredService<ICreateGameHandler>();
        var result = await handler.HandleAsync("user-1", new CreateGameRequest("A", "Captain Red"));

        var failed = Assert.IsType<GameCommandFailed>(result);
        Assert.Equal("Game names must be at least 2 characters long.", failed.ErrorMessage);
        await persistence.DidNotReceive().TryCreateGameAsync(Arg.Any<CreateGamePersistenceRequest>(), Arg.Any<CancellationToken>());
        await lobbyPublisher.DidNotReceive().PublishLobbyInvalidatedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await messagePublisher.DidNotReceive().PublishMessagesInvalidatedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_publishes_events_when_game_is_created()
    {
        var gameId = Guid.NewGuid();
        var persistence = Substitute.For<IGamePersistence>();
        persistence.TryCreateGameAsync(Arg.Any<CreateGamePersistenceRequest>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)gameId);
        var lobbyPublisher = Substitute.For<IGameLobbyInvalidationPublisher>();
        var messagePublisher = Substitute.For<IGameMessageInvalidationPublisher>();
        using var services = CreateServices(persistence, lobbyPublisher, messagePublisher);

        var handler = services.GetRequiredService<ICreateGameHandler>();
        var result = await handler.HandleAsync("user-1", new CreateGameRequest("  Weekend Match  ", "  Captain Red  "));

        var succeeded = Assert.IsType<GameCommandSucceeded>(result);
        Assert.Equal(gameId, succeeded.GameId);
        await persistence.Received(1).TryCreateGameAsync(
            Arg.Is<CreateGamePersistenceRequest>(r => r.GameName == "Weekend Match" && r.PlayerName == "Captain Red" && r.PlayerNameLookup == "CAPTAIN RED"),
            Arg.Any<CancellationToken>());
        await lobbyPublisher.Received(1).PublishLobbyInvalidatedAsync(gameId, Arg.Any<CancellationToken>());
        await messagePublisher.Received(1).PublishMessagesInvalidatedAsync(gameId, Arg.Any<CancellationToken>());
    }

    private static ServiceProvider CreateServices(
        IGamePersistence persistence,
        IGameLobbyInvalidationPublisher lobbyPublisher,
        IGameMessageInvalidationPublisher messagePublisher)
    {
        var services = new ServiceCollection();
        services.AddGameApplication();
        services.AddSingleton(persistence);
        services.AddSingleton(Substitute.For<IGameSessionService>());
        services.AddSingleton(lobbyPublisher);
        services.AddSingleton(Substitute.For<IGameSessionInvalidationPublisher>());
        services.AddSingleton(messagePublisher);
        services.AddSingleton(Substitute.For<IGameMessagePersistence>());
        return services.BuildServiceProvider();
    }
}
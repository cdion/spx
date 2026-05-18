using Microsoft.Extensions.DependencyInjection;
using Spx.Game.Application;
using Spx.Game.Application.Features.GetGameSession;
using Xunit;

namespace Spx.Game.Application.Tests;

public sealed class GetGameSessionHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_session_when_available()
    {
        var session = CreateSession(Guid.NewGuid(), 2, waitingForOpponent: true);
        var sessionService = Substitute.For<IGameSessionService>();
        sessionService.GetSessionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(session);
        using var services = CreateServices(sessionService);

        var handler = services.GetRequiredService<IGetGameSessionHandler>();
        var result = await handler.HandleAsync(session.GameId, Guid.NewGuid());

        Assert.Equal(session, result);
    }

    [Fact]
    public async Task HandleAsync_returns_null_when_session_is_unavailable()
    {
        var sessionService = Substitute.For<IGameSessionService>();
        sessionService.GetSessionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((GameSessionView?)null);
        using var services = CreateServices(sessionService);

        var handler = services.GetRequiredService<IGetGameSessionHandler>();
        var result = await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(result);
    }

    private static ServiceProvider CreateServices(IGameSessionService sessionService)
    {
        var services = new ServiceCollection();
        services.AddGameApplication();
        services.AddSingleton(sessionService);
        services.AddSingleton(Substitute.For<IGamePersistence>());
        services.AddSingleton(Substitute.For<IGameLobbyInvalidationPublisher>());
        services.AddSingleton(Substitute.For<IGameSessionInvalidationPublisher>());
        services.AddSingleton(Substitute.For<IGameMessageInvalidationPublisher>());
        services.AddSingleton(Substitute.For<IGameMessagePersistence>());
        return services.BuildServiceProvider();
    }

    private static GameSessionView CreateSession(Guid gameId, int roundNumber, bool waitingForOpponent)
    {
        var currentPlayer = new GameSessionParticipant(Guid.NewGuid());
        var opponentPlayer = new GameSessionParticipant(Guid.NewGuid());

        return new GameSessionView(
            gameId,
            roundNumber,
            GamePhase.Play,
            new GamePlayerStateView(currentPlayer, [], false, 0, 0, false, false, []),
            new GamePlayerStateView(opponentPlayer, [], false, 0, 0, false, true, []),
            [],
            0,
            waitingForOpponent,
            false,
            false,
            GameCardCatalog.MaxBatchSize,
            null,
            null);
    }
}
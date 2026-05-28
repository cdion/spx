using Microsoft.Extensions.DependencyInjection;
using Spx.Nexus.Application;
using Spx.Nexus.Application.Features.EnsureNexusSession;
using Xunit;

namespace Spx.Nexus.Application.Tests;

public sealed class EnsureGameSessionHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_false_when_active_roster_is_not_two_players()
    {
        var persistence = Substitute.For<IGamePersistence>();
        persistence
            .GetActiveSessionPlayersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<GameSessionParticipant>?>([
                new GameSessionParticipant(Guid.NewGuid()),
            ]);
        var sessionService = Substitute.For<INexusSessionService>();
        using var services = CreateServices(persistence, sessionService);

        var handler = services.GetRequiredService<IEnsureNexusSessionHandler>();
        var result = await handler.HandleAsync(Guid.NewGuid());

        Assert.False(result);
        await sessionService
            .DidNotReceive()
            .EnsureSessionAsync(
                Arg.Any<Guid>(),
                Arg.Any<IReadOnlyList<GameSessionParticipant>>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleAsync_calls_session_service_when_active_roster_has_two_players()
    {
        var gameId = Guid.NewGuid();
        var persistence = Substitute.For<IGamePersistence>();
        persistence
            .GetActiveSessionPlayersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<GameSessionParticipant>?>([
                new GameSessionParticipant(Guid.NewGuid()),
                new GameSessionParticipant(Guid.NewGuid()),
            ]);
        var sessionService = Substitute.For<INexusSessionService>();
        sessionService
            .EnsureSessionAsync(
                Arg.Any<Guid>(),
                Arg.Any<IReadOnlyList<GameSessionParticipant>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(true);
        using var services = CreateServices(persistence, sessionService);

        var handler = services.GetRequiredService<IEnsureNexusSessionHandler>();
        var result = await handler.HandleAsync(gameId);

        Assert.True(result);
        await sessionService
            .Received(1)
            .EnsureSessionAsync(
                gameId,
                Arg.Any<IReadOnlyList<GameSessionParticipant>>(),
                Arg.Any<CancellationToken>()
            );
    }

    private static ServiceProvider CreateServices(
        IGamePersistence persistence,
        INexusSessionService sessionService
    )
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddSingleton(persistence);
        services.AddSingleton(sessionService);
        services.AddSingleton(Substitute.For<IGamePresenceService>());
        services.AddSingleton(Substitute.For<ILobbyInvalidationPublisher>());
        services.AddSingleton(Substitute.For<INexusSessionInvalidationPublisher>());
        services.AddSingleton(Substitute.For<IGameMessageInvalidationPublisher>());
        services.AddSingleton(Substitute.For<IGameMessagePersistence>());
        return services.BuildServiceProvider();
    }
}

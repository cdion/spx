using Microsoft.Extensions.DependencyInjection;
using Spx.Game.Application;
using Spx.Game.Application.Nexus.Features.EnsureNexusSession;
using Xunit;

namespace Spx.Game.Application.Tests;

public sealed class EnsureGameSessionHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_false_when_active_roster_is_not_two_players()
    {
        var sessionRosterProvider = Substitute.For<INexusSessionRosterProvider>();
        sessionRosterProvider
            .GetActiveSessionPlayersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<Guid>?>([Guid.NewGuid()]);
        var sessionService = Substitute.For<INexusSessionService>();
        using var services = CreateServices(sessionRosterProvider, sessionService);

        var handler = services.GetRequiredService<IEnsureNexusSessionHandler>();
        var result = await handler.HandleAsync(Guid.NewGuid());

        Assert.False(result);
        await sessionService
            .DidNotReceive()
            .EnsureSessionAsync(
                Arg.Any<Guid>(),
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleAsync_calls_session_service_when_active_roster_has_two_players()
    {
        var gameId = Guid.NewGuid();
        var sessionRosterProvider = Substitute.For<INexusSessionRosterProvider>();
        sessionRosterProvider
            .GetActiveSessionPlayersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<Guid>?>([Guid.NewGuid(), Guid.NewGuid()]);
        var sessionService = Substitute.For<INexusSessionService>();
        sessionService
            .EnsureSessionAsync(
                Arg.Any<Guid>(),
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(true);
        using var services = CreateServices(sessionRosterProvider, sessionService);

        var handler = services.GetRequiredService<IEnsureNexusSessionHandler>();
        var result = await handler.HandleAsync(gameId);

        Assert.True(result);
        await sessionService
            .Received(1)
            .EnsureSessionAsync(
                gameId,
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>()
            );
    }

    private static ServiceProvider CreateServices(
        INexusSessionRosterProvider sessionRosterProvider,
        INexusSessionService sessionService
    )
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddSingleton(sessionRosterProvider);
        services.AddSingleton(sessionService);
        services.AddSingleton(Substitute.For<IGamePresenceService>());
        services.AddSingleton(Substitute.For<IGamePersistence>());
        services.AddSingleton(Substitute.For<ILobbyInvalidationPublisher>());
        services.AddSingleton(Substitute.For<INexusSessionInvalidationPublisher>());
        services.AddSingleton(Substitute.For<IGameMessageInvalidationPublisher>());
        services.AddSingleton(Substitute.For<IGameMessagePersistence>());
        return services.BuildServiceProvider();
    }
}

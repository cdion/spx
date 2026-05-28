using Microsoft.Extensions.DependencyInjection;
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
        var presenceService = Substitute.For<IGamePresenceService>();
        presenceService
            .GetPresenceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(presence);
        using var services = CreateServices(presenceService);

        var handler = services.GetRequiredService<IGetGamePresenceHandler>();
        var result = await handler.HandleAsync(Guid.NewGuid());

        Assert.Equal(presence, result);
    }

    private static ServiceProvider CreateServices(IGamePresenceService presenceService)
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddSingleton(presenceService);
        services.AddSingleton(Substitute.For<INexusSessionService>());
        services.AddSingleton(Substitute.For<IGamePersistence>());
        services.AddSingleton(Substitute.For<ILobbyInvalidationPublisher>());
        services.AddSingleton(Substitute.For<INexusSessionInvalidationPublisher>());
        services.AddSingleton(Substitute.For<IGameMessageInvalidationPublisher>());
        services.AddSingleton(Substitute.For<IGameMessagePersistence>());
        return services.BuildServiceProvider();
    }
}

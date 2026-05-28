using Microsoft.Extensions.DependencyInjection;
using Spx.Game.Application;

namespace Spx.Game.Application.Tests;

internal static class GameMessageHandlerTestServices
{
    public static ServiceProvider Create(
        IGameMessagePersistence persistence,
        IGameMessageInvalidationPublisher publisher
    )
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddSingleton(Substitute.For<IGamePersistence>());
        services.AddSingleton(Substitute.For<ILobbyInvalidationPublisher>());
        services.AddSingleton(Substitute.For<INexusSessionInvalidationPublisher>());
        services.AddSingleton(publisher);
        services.AddSingleton(persistence);
        return services.BuildServiceProvider();
    }
}

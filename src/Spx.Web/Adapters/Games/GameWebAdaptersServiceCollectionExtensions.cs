using Microsoft.Extensions.DependencyInjection;
using Spx.Game.Application;
using Spx.Web.Adapters.Games;

namespace Microsoft.Extensions.DependencyInjection;

public static class GameWebAdaptersServiceCollectionExtensions
{
    public static IServiceCollection AddGameWebAdapters(this IServiceCollection services)
    {
        services.AddSingleton<OrleansGameRuntimeClient>();
        services.AddSingleton<IGameLobbyInvalidationPublisher>(serviceProvider =>
            serviceProvider.GetRequiredService<OrleansGameRuntimeClient>()
        );
        services.AddSingleton<IGameSessionInvalidationPublisher>(serviceProvider =>
            serviceProvider.GetRequiredService<OrleansGameRuntimeClient>()
        );
        services.AddSingleton<IGameMessageInvalidationPublisher>(serviceProvider =>
            serviceProvider.GetRequiredService<OrleansGameRuntimeClient>()
        );
        services.AddSingleton<IGamePresenceService>(serviceProvider =>
            serviceProvider.GetRequiredService<OrleansGameRuntimeClient>()
        );
        services.AddSingleton<IGameSessionService>(serviceProvider =>
            serviceProvider.GetRequiredService<OrleansGameRuntimeClient>()
        );

        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using Spx.Games;
using Spx.Web.Adapters.Games;

namespace Microsoft.Extensions.DependencyInjection;

public static class GameWebAdaptersServiceCollectionExtensions
{
    public static IServiceCollection AddGameWebAdapters(this IServiceCollection services)
    {
        services.AddSingleton<OrleansGameRuntimeClient>();
        services.AddSingleton<IGameLobbyEventsPublisher>(serviceProvider =>
            serviceProvider.GetRequiredService<OrleansGameRuntimeClient>());
        services.AddSingleton<IGameMessageEventsPublisher>(serviceProvider =>
            serviceProvider.GetRequiredService<OrleansGameRuntimeClient>());
        services.AddSingleton<IGameSessionService>(serviceProvider =>
            serviceProvider.GetRequiredService<OrleansGameRuntimeClient>());

        return services;
    }
}
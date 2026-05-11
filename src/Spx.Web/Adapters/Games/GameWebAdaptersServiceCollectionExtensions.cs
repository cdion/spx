using Microsoft.Extensions.DependencyInjection;
using Spx.Games;
using Spx.Web.Adapters.Games;

namespace Microsoft.Extensions.DependencyInjection;

public static class GameWebAdaptersServiceCollectionExtensions
{
    public static IServiceCollection AddGameWebAdapters(this IServiceCollection services)
    {
        services.AddSingleton<OrleansGameLobbyAdapter>();
        services.AddSingleton<IGameLobbySubscriptionService>(serviceProvider =>
            serviceProvider.GetRequiredService<OrleansGameLobbyAdapter>());
        services.AddSingleton<IGameMessageSubscriptionService>(serviceProvider =>
            serviceProvider.GetRequiredService<OrleansGameLobbyAdapter>());
        services.AddSingleton<IGameLobbyEventsPublisher>(serviceProvider =>
            serviceProvider.GetRequiredService<OrleansGameLobbyAdapter>());
        services.AddSingleton<IGameMessageEventsPublisher>(serviceProvider =>
            serviceProvider.GetRequiredService<OrleansGameLobbyAdapter>());

        return services;
    }
}
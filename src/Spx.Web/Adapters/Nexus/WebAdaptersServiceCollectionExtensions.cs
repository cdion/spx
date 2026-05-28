using Microsoft.Extensions.DependencyInjection;
using Spx.Nexus.Application;
using Spx.Web.Adapters;

namespace Microsoft.Extensions.DependencyInjection;

public static class WebAdaptersServiceCollectionExtensions
{
    public static IServiceCollection AddWebAdapters(this IServiceCollection services)
    {
        services.AddSingleton<OrleansNexusRuntimeClient>();
        services.AddSingleton<ILobbyInvalidationPublisher>(serviceProvider =>
            serviceProvider.GetRequiredService<OrleansNexusRuntimeClient>()
        );
        services.AddSingleton<INexusSessionInvalidationPublisher>(serviceProvider =>
            serviceProvider.GetRequiredService<OrleansNexusRuntimeClient>()
        );
        services.AddSingleton<IGameMessageInvalidationPublisher>(serviceProvider =>
            serviceProvider.GetRequiredService<OrleansNexusRuntimeClient>()
        );
        services.AddSingleton<IGamePresenceService>(serviceProvider =>
            serviceProvider.GetRequiredService<OrleansNexusRuntimeClient>()
        );
        services.AddSingleton<INexusSessionService>(serviceProvider =>
            serviceProvider.GetRequiredService<OrleansNexusRuntimeClient>()
        );

        return services;
    }
}

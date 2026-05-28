using Spx.Game.Application;
using Spx.Game.Application.Nexus;
using Spx.Web.Playground.Nexus;

namespace Microsoft.Extensions.DependencyInjection;

public static class PlaygroundNexusServiceCollectionExtensions
{
    public static IServiceCollection AddPlaygroundNexusServices(this IServiceCollection services)
    {
        services.AddScoped<PlaygroundNexusHarness>();
        services.AddScoped<INexusSessionService>(serviceProvider =>
            serviceProvider.GetRequiredService<PlaygroundNexusHarness>()
        );
        services.AddScoped<INexusSessionRosterProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<PlaygroundNexusHarness>()
        );
        services.AddScoped<IGamePresenceService>(serviceProvider =>
            serviceProvider.GetRequiredService<PlaygroundNexusHarness>()
        );
        services.AddScoped<IGamePersistence>(serviceProvider =>
            serviceProvider.GetRequiredService<PlaygroundNexusHarness>()
        );
        services.AddScoped<IGameMessagePersistence>(serviceProvider =>
            serviceProvider.GetRequiredService<PlaygroundNexusHarness>()
        );
        services.AddScoped<ILobbyInvalidationPublisher>(serviceProvider =>
            serviceProvider.GetRequiredService<PlaygroundNexusHarness>()
        );
        services.AddScoped<INexusSessionInvalidationPublisher>(serviceProvider =>
            serviceProvider.GetRequiredService<PlaygroundNexusHarness>()
        );
        services.AddScoped<IGameMessageInvalidationPublisher>(serviceProvider =>
            serviceProvider.GetRequiredService<PlaygroundNexusHarness>()
        );

        return services;
    }
}

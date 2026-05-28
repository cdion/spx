using Microsoft.Extensions.DependencyInjection;
using Spx.Data;
using Spx.Game.Application;
using Spx.Game.Application.Nexus;

namespace Microsoft.Extensions.DependencyInjection;

public static class GameDataAdaptersServiceCollectionExtensions
{
    public static IServiceCollection AddGameDataAdapters(this IServiceCollection services)
    {
        services.AddScoped<IGamePersistence, EfGamePersistence>();
        services.AddScoped<IGameMessagePersistence, EfGameMessagePersistence>();
        services.AddScoped<INexusSessionRosterProvider, EfGamePersistence>();

        return services;
    }
}

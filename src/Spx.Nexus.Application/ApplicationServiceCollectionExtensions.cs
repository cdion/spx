using Microsoft.Extensions.DependencyInjection;
using Spx.Nexus.Application;
using Spx.Nexus.Application.Features.CreateGame;
using Spx.Nexus.Application.Features.DeleteMessage;
using Spx.Nexus.Application.Features.EditMessage;
using Spx.Nexus.Application.Features.EnsureNexusSession;
using Spx.Nexus.Application.Features.GetGamePresence;
using Spx.Nexus.Application.Features.GetMessages;
using Spx.Nexus.Application.Features.GetMessageUpdates;
using Spx.Nexus.Application.Features.GetNexusPage;
using Spx.Nexus.Application.Features.GetUserGames;
using Spx.Nexus.Application.Features.JoinGame;
using Spx.Nexus.Application.Features.LeaveGame;
using Spx.Nexus.Application.Features.SendPrivateMessage;
using Spx.Nexus.Application.Features.SendPublicMessage;
using Spx.Nexus.Application.Features.SubmitOrders;

namespace Microsoft.Extensions.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICreateGameHandler, CreateGameHandler>();
        services.AddScoped<IEnsureNexusSessionHandler, EnsureNexusSessionHandler>();
        services.AddScoped<IJoinGameHandler, JoinGameHandler>();
        services.AddScoped<ILeaveGameHandler, LeaveGameHandler>();
        services.AddScoped<IGetNexusPageHandler, GetNexusPageHandler>();
        services.AddScoped<IGetGamePresenceHandler, GetGamePresenceHandler>();
        services.AddScoped<IGetUserGamesHandler, GetUserGamesHandler>();
        services.AddScoped<ISubmitOrdersHandler, SubmitOrdersHandler>();

        services.AddScoped<IGetMessagesHandler, GetMessagesHandler>();
        services.AddScoped<IGetMessageUpdatesHandler, GetMessageUpdatesHandler>();
        services.AddScoped<ISendPublicMessageHandler, SendPublicMessageHandler>();
        services.AddScoped<ISendPrivateMessageHandler, SendPrivateMessageHandler>();
        services.AddScoped<IEditMessageHandler, EditMessageHandler>();
        services.AddScoped<IDeleteMessageHandler, DeleteMessageHandler>();

        return services;
    }
}

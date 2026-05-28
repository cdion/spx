using Microsoft.Extensions.DependencyInjection;
using Spx.Game.Application;
using Spx.Game.Application.Features.CreateGame;
using Spx.Game.Application.Features.DeleteMessage;
using Spx.Game.Application.Features.EditMessage;
using Spx.Game.Application.Features.GetGamePresence;
using Spx.Game.Application.Features.GetMessages;
using Spx.Game.Application.Features.GetMessageUpdates;
using Spx.Game.Application.Features.GetUserGames;
using Spx.Game.Application.Features.JoinGame;
using Spx.Game.Application.Features.LeaveGame;
using Spx.Game.Application.Features.SendPrivateMessage;
using Spx.Game.Application.Features.SendPublicMessage;
using Spx.Game.Application.Nexus.Features.EnsureNexusSession;
using Spx.Game.Application.Nexus.Features.GetNexusPage;
using Spx.Game.Application.Nexus.Features.SubmitOrders;

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

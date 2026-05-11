using Microsoft.Extensions.DependencyInjection;
using Spx.Games;
using Spx.Games.Features.CreateGame;
using Spx.Games.Features.DeleteMessage;
using Spx.Games.Features.EditMessage;
using Spx.Games.Features.GetLobby;
using Spx.Games.Features.GetMessageUpdates;
using Spx.Games.Features.GetMessages;
using Spx.Games.Features.GetUserGames;
using Spx.Games.Features.JoinGame;
using Spx.Games.Features.LeaveGame;
using Spx.Games.Features.SendPrivateMessage;
using Spx.Games.Features.SendPublicMessage;

namespace Microsoft.Extensions.DependencyInjection;

public static class GameApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddGameApplication(this IServiceCollection services)
    {
        services.AddScoped<ICreateGameHandler, CreateGameHandler>();
        services.AddScoped<IJoinGameHandler, JoinGameHandler>();
        services.AddScoped<ILeaveGameHandler, LeaveGameHandler>();
        services.AddScoped<IGetLobbyHandler, GetLobbyHandler>();
        services.AddScoped<IGetUserGamesHandler, GetUserGamesHandler>();

        services.AddScoped<IGetMessagesHandler, GetMessagesHandler>();
        services.AddScoped<IGetMessageUpdatesHandler, GetMessageUpdatesHandler>();
        services.AddScoped<ISendPublicMessageHandler, SendPublicMessageHandler>();
        services.AddScoped<ISendPrivateMessageHandler, SendPrivateMessageHandler>();
        services.AddScoped<IEditMessageHandler, EditMessageHandler>();
        services.AddScoped<IDeleteMessageHandler, DeleteMessageHandler>();

        return services;
    }
}
using Microsoft.Extensions.DependencyInjection;
using Spx.Game.Application;
using Spx.Game.Application.Features.CreateGame;
using Spx.Game.Application.Features.DeleteMessage;
using Spx.Game.Application.Features.EditMessage;
using Spx.Game.Application.Features.GetGamePage;
using Spx.Game.Application.Features.GetGameLobby;
using Spx.Game.Application.Features.GetMessageUpdates;
using Spx.Game.Application.Features.GetMessages;
using Spx.Game.Application.Features.GetUserGames;
using Spx.Game.Application.Features.JoinGame;
using Spx.Game.Application.Features.LeaveGame;
using Spx.Game.Application.Features.SendPrivateMessage;
using Spx.Game.Application.Features.SendPublicMessage;
using Spx.Game.Application.Features.SubmitGameMove;

namespace Microsoft.Extensions.DependencyInjection;

public static class GameApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddGameApplication(this IServiceCollection services)
    {
        services.AddScoped<ICreateGameHandler, CreateGameHandler>();
        services.AddScoped<IJoinGameHandler, JoinGameHandler>();
        services.AddScoped<ILeaveGameHandler, LeaveGameHandler>();
        services.AddScoped<IGetGameLobbyHandler, GetGameLobbyHandler>();
        services.AddScoped<IGetGamePageHandler, GetGamePageHandler>();
        services.AddScoped<IGetUserGamesHandler, GetUserGamesHandler>();
        services.AddScoped<ISubmitGameMoveHandler, SubmitGameMoveHandler>();

        services.AddScoped<IGetMessagesHandler, GetMessagesHandler>();
        services.AddScoped<IGetMessageUpdatesHandler, GetMessageUpdatesHandler>();
        services.AddScoped<ISendPublicMessageHandler, SendPublicMessageHandler>();
        services.AddScoped<ISendPrivateMessageHandler, SendPrivateMessageHandler>();
        services.AddScoped<IEditMessageHandler, EditMessageHandler>();
        services.AddScoped<IDeleteMessageHandler, DeleteMessageHandler>();

        return services;
    }
}
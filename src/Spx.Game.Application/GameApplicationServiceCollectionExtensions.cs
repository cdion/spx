using Microsoft.Extensions.DependencyInjection;
using Spx.Game.Application;
using Spx.Game.Application.Features.CreateGame;
using Spx.Game.Application.Features.DeleteMessage;
using Spx.Game.Application.Features.EditMessage;
using Spx.Game.Application.Features.EnsureGameSession;
using Spx.Game.Application.Features.GetGamePage;
using Spx.Game.Application.Features.GetGamePresence;
using Spx.Game.Application.Features.GetGameSession;
using Spx.Game.Application.Features.GetMessages;
using Spx.Game.Application.Features.GetMessageUpdates;
using Spx.Game.Application.Features.GetUserGames;
using Spx.Game.Application.Features.JoinGame;
using Spx.Game.Application.Features.LeaveGame;
using Spx.Game.Application.Features.SendPrivateMessage;
using Spx.Game.Application.Features.SendPublicMessage;
using Spx.Game.Application.Features.SubmitAcquireCard;
using Spx.Game.Application.Features.SubmitPlayBatch;

namespace Microsoft.Extensions.DependencyInjection;

public static class GameApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddGameApplication(this IServiceCollection services)
    {
        services.AddSingleton<IGameplayEventMessageFormatter, GameplayEventMessageFormatter>();

        services.AddScoped<ICreateGameHandler, CreateGameHandler>();
        services.AddScoped<IEnsureGameSessionHandler, EnsureGameSessionHandler>();
        services.AddScoped<IJoinGameHandler, JoinGameHandler>();
        services.AddScoped<ILeaveGameHandler, LeaveGameHandler>();
        services.AddScoped<IGetGamePageHandler, GetGamePageHandler>();
        services.AddScoped<IGetGamePresenceHandler, GetGamePresenceHandler>();
        services.AddScoped<IGetGameSessionHandler, GetGameSessionHandler>();
        services.AddScoped<IGetUserGamesHandler, GetUserGamesHandler>();
        services.AddScoped<ISubmitAcquireCardHandler, SubmitAcquireCardHandler>();
        services.AddScoped<ISubmitPlayBatchHandler, SubmitPlayBatchHandler>();

        services.AddScoped<IGetMessagesHandler, GetMessagesHandler>();
        services.AddScoped<IGetMessageUpdatesHandler, GetMessageUpdatesHandler>();
        services.AddScoped<ISendPublicMessageHandler, SendPublicMessageHandler>();
        services.AddScoped<ISendPrivateMessageHandler, SendPrivateMessageHandler>();
        services.AddScoped<IEditMessageHandler, EditMessageHandler>();
        services.AddScoped<IDeleteMessageHandler, DeleteMessageHandler>();

        return services;
    }
}

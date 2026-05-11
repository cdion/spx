using Microsoft.Extensions.Logging.Abstractions;
using Spx.Data;
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

namespace Spx.Games.IntegrationTests;

internal static class GameFeatureTestFactory
{
    public static GameFeatureSet Create(
        ApplicationDbContext dbContext,
        IGameLobbyEventsPublisher? notifier = null,
        IGameMessageEventsPublisher? messagePublisher = null)
    {
        notifier ??= new FakeGameLobbyNotifier();
        messagePublisher ??= new FakeGameMessagePublisher();
        var gamePersistence = new EfGamePersistence(dbContext, NullLogger<EfGamePersistence>.Instance);
        var messageSupport = new GameMessagePersistenceSupport(dbContext);
        var gameMessagePersistence = new EfGameMessagePersistence(dbContext, messageSupport);

        return new GameFeatureSet(
            new CreateGameHandler(gamePersistence, notifier, messagePublisher),
            new JoinGameHandler(gamePersistence, notifier, messagePublisher),
            new LeaveGameHandler(gamePersistence, notifier, messagePublisher),
            new GetLobbyHandler(gamePersistence),
            new GetUserGamesHandler(gamePersistence),
            new GetMessagesHandler(gameMessagePersistence),
            new GetMessageUpdatesHandler(gameMessagePersistence),
            new SendPublicMessageHandler(messagePublisher, gameMessagePersistence),
            new SendPrivateMessageHandler(messagePublisher, gameMessagePersistence),
            new EditMessageHandler(messagePublisher, gameMessagePersistence),
            new DeleteMessageHandler(messagePublisher, gameMessagePersistence));
    }
}

internal sealed record GameFeatureSet(
    ICreateGameHandler CreateGame,
    IJoinGameHandler JoinGame,
    ILeaveGameHandler LeaveGame,
    IGetLobbyHandler GetLobby,
    IGetUserGamesHandler GetUserGames,
    IGetMessagesHandler GetMessages,
    IGetMessageUpdatesHandler GetMessageUpdates,
    ISendPublicMessageHandler SendPublicMessage,
    ISendPrivateMessageHandler SendPrivateMessage,
    IEditMessageHandler EditMessage,
    IDeleteMessageHandler DeleteMessage);
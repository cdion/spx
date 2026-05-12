using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Spx.Contracts;
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
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IGameLobbyEventsPublisher? notifier = null,
        IGameMessageEventsPublisher? messagePublisher = null)
    {
        notifier ??= new FakeGameLobbyNotifier();
        messagePublisher ??= new FakeGameMessagePublisher();
        var sessionService = new FakeGameSessionService();
        var gamePersistence = new EfGamePersistence(contextFactory, NullLogger<EfGamePersistence>.Instance);
        var gameMessagePersistence = new EfGameMessagePersistence(contextFactory);

        return new GameFeatureSet(
            new CreateGameHandler(gamePersistence, notifier, messagePublisher),
            new JoinGameHandler(gamePersistence, sessionService, notifier, messagePublisher),
            new LeaveGameHandler(gamePersistence, sessionService, notifier, messagePublisher),
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

internal sealed class FakeGameSessionService : IGameSessionService
{
    public Task<bool> TryInitializeAsync(Guid gameId, IReadOnlyList<GameSessionPlayer> players, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<GameSessionPlayerView?> GetPlayerViewAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
        => Task.FromResult<GameSessionPlayerView?>(null);

    public Task<GameSessionPlayerView> SubmitMoveAsync(Guid gameId, SubmitGameMoveCommand command, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<GameSessionPlayerView> AbandonAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Spx.Contracts;
using Spx.Data;
using Spx.Game.Application;
using Spx.Game.Application.Features.CreateGame;
using Spx.Game.Application.Features.DeleteMessage;
using Spx.Game.Application.Features.EditMessage;
using Spx.Game.Application.Features.GetMessageUpdates;
using Spx.Game.Application.Features.GetMessages;
using Spx.Game.Application.Features.GetGameLobby;
using Spx.Game.Application.Features.GetUserGames;
using Spx.Game.Application.Features.JoinGame;
using Spx.Game.Application.Features.LeaveGame;
using Spx.Game.Application.Features.SendPrivateMessage;
using Spx.Game.Application.Features.SendPublicMessage;

namespace Spx.Game.Application.IntegrationTests;

internal static class GameFeatureTestFactory
{
    public static GameFeatureSet Create(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IGameLobbyEventsPublisher? notifier = null,
        IGameMessageEventsPublisher? messagePublisher = null,
        IGameSessionService? sessionService = null)
    {
        notifier ??= new FakeGameLobbyNotifier();
        messagePublisher ??= new FakeGameMessagePublisher();
        sessionService ??= new FakeGameSessionService();
        var gamePersistence = new EfGamePersistence(contextFactory, NullLogger<EfGamePersistence>.Instance);
        var gameMessagePersistence = new EfGameMessagePersistence(contextFactory);

        return new GameFeatureSet(
            new CreateGameHandler(gamePersistence, notifier, messagePublisher),
            new JoinGameHandler(gamePersistence, sessionService, notifier, messagePublisher),
            new LeaveGameHandler(gamePersistence, sessionService, notifier, messagePublisher),
            new GetGameLobbyHandler(gamePersistence),
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
    IGetGameLobbyHandler GetGameLobby,
    IGetUserGamesHandler GetUserGames,
    IGetMessagesHandler GetMessages,
    IGetMessageUpdatesHandler GetMessageUpdates,
    ISendPublicMessageHandler SendPublicMessage,
    ISendPrivateMessageHandler SendPrivateMessage,
    IEditMessageHandler EditMessage,
    IDeleteMessageHandler DeleteMessage);

internal sealed class FakeGameSessionService : IGameSessionService
{
    public bool TryInitializeResult { get; init; } = true;

    public Task<bool> EnsureSessionAsync(Guid gameId, IReadOnlyList<GameSessionParticipantView> players, CancellationToken cancellationToken = default)
        => Task.FromResult(TryInitializeResult);

    public Task<GameSessionView?> GetSessionViewAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
        => Task.FromResult<GameSessionView?>(null);

    public Task<SubmitGameMoveOutcome> SubmitMoveAsync(Guid gameId, SubmitGameMoveCommand command, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<GameSessionView> AbandonAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
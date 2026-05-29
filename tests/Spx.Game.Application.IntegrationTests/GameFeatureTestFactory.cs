using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Spx.Data;
using Spx.Game.Application;
using Spx.Game.Application.Features.CreateGame;
using Spx.Game.Application.Features.DeleteMessage;
using Spx.Game.Application.Features.EditMessage;
using Spx.Game.Application.Features.GetMessages;
using Spx.Game.Application.Features.GetMessageUpdates;
using Spx.Game.Application.Features.GetUserGames;
using Spx.Game.Application.Features.JoinGame;
using Spx.Game.Application.Features.LeaveGame;
using Spx.Game.Application.Features.SendPrivateMessage;
using Spx.Game.Application.Features.SendPublicMessage;
using Spx.Game.Application.Nexus.Features.EnsureNexusSession;
using Spx.Nexus.Domain;

namespace Spx.Game.Application.IntegrationTests;

internal static class GameFeatureTestFactory
{
    public static GameFeatureSet Create(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILobbyInvalidationPublisher? notifier = null,
        IGameMessageInvalidationPublisher? messagePublisher = null,
        INexusSessionService? sessionService = null
    )
    {
        notifier ??= new FakeGameLobbyNotifier();
        messagePublisher ??= new FakeGameMessagePublisher();
        sessionService ??= new FakeGameSessionService();
        var gamePersistence = new EfGamePersistence(
            contextFactory,
            NullLogger<EfGamePersistence>.Instance
        );
        var gameMessagePersistence = new EfGameMessagePersistence(contextFactory);
        var ensureGameSession = new EnsureNexusSessionHandler(gamePersistence, sessionService);

        return new GameFeatureSet(
            new CreateGameHandler(gamePersistence, notifier, messagePublisher),
            new JoinGameHandler(
                gamePersistence,
                ensureGameSession,
                notifier,
                messagePublisher,
                NullLogger<JoinGameHandler>.Instance
            ),
            new LeaveGameHandler(
                gamePersistence,
                sessionService,
                notifier,
                messagePublisher,
                NullLogger<LeaveGameHandler>.Instance
            ),
            gamePersistence,
            new GetUserGamesHandler(gamePersistence),
            new GetMessagesHandler(gameMessagePersistence),
            new GetMessageUpdatesHandler(gameMessagePersistence),
            new SendPublicMessageHandler(messagePublisher, gameMessagePersistence),
            new SendPrivateMessageHandler(messagePublisher, gameMessagePersistence),
            new EditMessageHandler(messagePublisher, gameMessagePersistence),
            new DeleteMessageHandler(messagePublisher, gameMessagePersistence)
        );
    }
}

internal sealed record GameFeatureSet(
    ICreateGameHandler CreateGame,
    IJoinGameHandler JoinGame,
    ILeaveGameHandler LeaveGame,
    IGamePersistence Persistence,
    IGetUserGamesHandler GetUserGames,
    IGetMessagesHandler GetMessages,
    IGetMessageUpdatesHandler GetMessageUpdates,
    ISendPublicMessageHandler SendPublicMessage,
    ISendPrivateMessageHandler SendPrivateMessage,
    IEditMessageHandler EditMessage,
    IDeleteMessageHandler DeleteMessage
);

internal sealed class FakeGameSessionService : INexusSessionService
{
    public bool TryInitializeResult { get; init; } = true;

    public Task<bool> EnsureSessionAsync(
        Guid gameId,
        IReadOnlyList<Guid> players,
        CancellationToken cancellationToken = default
    ) => Task.FromResult(TryInitializeResult);

    public Task<GameSessionOutcome> GetSessionAsync(
        Guid gameId,
        Guid playerId,
        CancellationToken cancellationToken = default
    ) => Task.FromResult<GameSessionOutcome>(new GameSessionUnavailable());

    public Task<GameSessionCommandOutcome> SubmitOrdersAsync(
        Guid gameId,
        NexusTurnOrdersCommand command,
        CancellationToken cancellationToken = default
    ) =>
        Task.FromResult<GameSessionCommandOutcome>(
            new GameSessionCommandFailed("Not implemented in integration test factory.")
        );

    public Task AbandonAsync(
        Guid gameId,
        Guid playerId,
        CancellationToken cancellationToken = default
    ) => throw new NotSupportedException();
}

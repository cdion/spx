using Orleans;
using Orleans.Runtime;
using Spx.Contracts;
using Spx.Game.Application;
using Spx.Game.Domain;

namespace Spx.Web.Adapters.Games;

public sealed partial class OrleansGameRuntimeClient(
    IClusterClient clusterClient,
    IServiceScopeFactory scopeFactory,
    ILogger<OrleansGameRuntimeClient> logger
)
    : IGameLobbyInvalidationPublisher,
        IGameSessionInvalidationPublisher,
        IGameMessageInvalidationPublisher,
        IGamePresenceInvalidationPublisher,
        IGamePresenceService,
        IGameSessionService
{
    public async Task PublishLobbyInvalidatedAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await clusterClient.GetGrain<IGameInvalidationGrain>(gameId).PublishLobbyInvalidated();
        }
        catch (OrleansException exception)
        {
            LogPublishLobbyUpdateFailed(logger, exception, gameId);
        }
        catch (TimeoutException exception)
        {
            LogPublishLobbyUpdateFailed(logger, exception, gameId);
        }
    }

    public async Task PublishSessionInvalidatedAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await clusterClient
                .GetGrain<IGameInvalidationGrain>(gameId)
                .PublishSessionInvalidated();
        }
        catch (OrleansException exception)
        {
            LogPublishSessionUpdateFailed(logger, exception, gameId);
        }
        catch (TimeoutException exception)
        {
            LogPublishSessionUpdateFailed(logger, exception, gameId);
        }
    }

    public async Task PublishMessagesInvalidatedAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await clusterClient
                .GetGrain<IGameInvalidationGrain>(gameId)
                .PublishMessagesInvalidated();
        }
        catch (OrleansException exception)
        {
            LogPublishMessageUpdateFailed(logger, exception, gameId);
        }
        catch (TimeoutException exception)
        {
            LogPublishMessageUpdateFailed(logger, exception, gameId);
        }
    }

    public async Task PublishPresenceInvalidatedAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await clusterClient
                .GetGrain<IGameInvalidationGrain>(gameId)
                .PublishPresenceInvalidated();
        }
        catch (OrleansException exception)
        {
            LogPublishPresenceUpdateFailed(logger, exception, gameId);
        }
        catch (TimeoutException exception)
        {
            LogPublishPresenceUpdateFailed(logger, exception, gameId);
        }
    }

    public async Task<GamePresenceView> GetPresenceAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var snapshot = await clusterClient
                .GetGrain<IGamePresenceGrain>(gameId)
                .GetSnapshotAsync();
            return new GamePresenceView(snapshot.OnlinePlayerIds);
        }
        catch (OrleansException exception)
        {
            LogFetchPresenceFailed(logger, exception, gameId);
            return GamePresenceView.Empty;
        }
        catch (TimeoutException exception)
        {
            LogFetchPresenceFailed(logger, exception, gameId);
            return GamePresenceView.Empty;
        }
    }

    public Task UpsertPresenceLeaseAsync(
        Guid gameId,
        Guid playerId,
        Guid connectionId,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default
    ) =>
        clusterClient
            .GetGrain<IGamePresenceGrain>(gameId)
            .UpsertLeaseAsync(
                new UpsertGamePresenceLeaseCommand(playerId, connectionId, expiresAtUtc)
            );

    public Task RemovePresenceLeaseAsync(
        Guid gameId,
        Guid playerId,
        Guid connectionId,
        CancellationToken cancellationToken = default
    ) =>
        clusterClient
            .GetGrain<IGamePresenceGrain>(gameId)
            .RemoveLeaseAsync(new RemoveGamePresenceLeaseCommand(playerId, connectionId));

    public async Task<bool> EnsureSessionAsync(
        Guid gameId,
        IReadOnlyList<GameSessionParticipant> players,
        CancellationToken cancellationToken = default
    )
    {
        if (players.Count != 2)
        {
            return false;
        }

        try
        {
            await clusterClient
                .GetGrain<IGameSessionGrain>(gameId)
                .InitializeAsync(
                    new InitializeGameSessionCommand(
                        new GameSessionParticipant(players[0].PlayerId),
                        new GameSessionParticipant(players[1].PlayerId)
                    )
                );
            return true;
        }
        catch (OrleansException exception)
        {
            LogEnsureSessionFailed(logger, exception, gameId);
            return false;
        }
        catch (TimeoutException exception)
        {
            LogEnsureSessionFailed(logger, exception, gameId);
            return false;
        }
    }

    public async Task<GameSessionView?> GetSessionAsync(
        Guid gameId,
        Guid playerId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await TryPersistPendingGameplayEventBatchesAsync(gameId, cancellationToken);
            return await clusterClient
                .GetGrain<IGameSessionGrain>(gameId)
                .GetPlayerViewAsync(new GetGameSessionQuery(playerId));
        }
        catch (OrleansException exception)
        {
            LogFetchPlayerViewFailed(logger, exception, gameId, playerId);
            return null;
        }
        catch (TimeoutException exception)
        {
            LogFetchPlayerViewFailed(logger, exception, gameId, playerId);
            return null;
        }
    }

    public async Task<GameSessionCommandOutcome> SubmitAcquireAsync(
        Guid gameId,
        SubmitAcquireRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var result = await clusterClient
                .GetGrain<IGameSessionGrain>(gameId)
                .SubmitAcquireAsync(
                    new SubmitAcquireCommand(
                        request.PlayerId,
                        request.ExpectedRoundNumber,
                        request.MarketCardInstanceId
                    )
                );
            return MapSessionCommandResult(gameId, request.PlayerId, result);
        }
        catch (OrleansException exception)
        {
            LogSubmitAcquireFailed(logger, exception, gameId, request.PlayerId);
            throw;
        }
        catch (TimeoutException exception)
        {
            LogSubmitAcquireFailed(logger, exception, gameId, request.PlayerId);
            throw;
        }
    }

    public async Task<GameSessionCommandOutcome> SubmitPlayBatchAsync(
        Guid gameId,
        SubmitPlayBatchRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var result = await clusterClient
                .GetGrain<IGameSessionGrain>(gameId)
                .SubmitPlayBatchAsync(
                    new SubmitPlayBatchCommand(
                        request.PlayerId,
                        request.ExpectedRoundNumber,
                        request.Cards.Select(MapBatchCardSelection).ToArray()
                    )
                );
            return MapSessionCommandResult(gameId, request.PlayerId, result);
        }
        catch (OrleansException exception)
        {
            LogSubmitPlayBatchFailed(logger, exception, gameId, request.PlayerId);
            throw;
        }
        catch (TimeoutException exception)
        {
            LogSubmitPlayBatchFailed(logger, exception, gameId, request.PlayerId);
            throw;
        }
    }

    public async Task AbandonAsync(
        Guid gameId,
        Guid playerId,
        CancellationToken cancellationToken = default
    )
    {
        await clusterClient
            .GetGrain<IGameSessionGrain>(gameId)
            .AbandonAsync(new AbandonGameSessionCommand(playerId));
    }

    public Task AcknowledgeGameplayEventBatchAsync(
        Guid gameId,
        Guid gameplayEventBatchId,
        CancellationToken cancellationToken = default
    ) =>
        clusterClient
            .GetGrain<IGameSessionGrain>(gameId)
            .AcknowledgeGameplayEventBatchesAsync(
                new AcknowledgeGameplayEventBatchesGrainCommand([gameplayEventBatchId])
            );

    private GameSessionCommandOutcome MapSessionCommandResult(
        Guid gameId,
        Guid playerId,
        GameSessionGrainCommandResult result
    ) =>
        result switch
        {
            GameSessionGrainCommandSucceededResult succeeded => new GameSessionCommandSucceeded(
                succeeded.Session,
                succeeded.GameplayEvents,
                succeeded.PendingGameplayEventBatchId
            ),
            GameSessionGrainCommandRejectedResult rejected => LogRejectedCommand(
                gameId,
                playerId,
                rejected
            ),
            _ => throw new InvalidOperationException("Unknown game session command result type."),
        };

    private static GameCardReferenceCommand MapCardReferenceSelection(
        GameCardReferenceSelection selection
    ) =>
        new(
            selection.CardInstanceId,
            selection.ProducedByCardInstanceId,
            selection.ProducedCardDefinition
        );

    private static GameBatchCardCommand MapBatchCardSelection(GameBatchCardSelection selection) =>
        new(
            selection.CardInstanceId,
            selection.ChosenResourceColor,
            selection.CraftedCardDefinition,
            selection.TargetResourceColor,
            selection.TargetCardInstanceId,
            selection.ConsumedCards.Select(MapCardReferenceSelection).ToArray()
        );

    private GameSessionCommandFailed LogRejectedCommand(
        Guid gameId,
        Guid playerId,
        GameSessionGrainCommandRejectedResult rejected
    )
    {
        LogGameCommandRejected(logger, gameId, playerId, rejected.ErrorMessage);
        return new GameSessionCommandFailed(rejected.ErrorMessage);
    }

    private async Task TryPersistPendingGameplayEventBatchesAsync(
        Guid gameId,
        CancellationToken cancellationToken
    )
    {
        var grain = clusterClient.GetGrain<IGameSessionGrain>(gameId);
        var batches = await grain.GetPendingGameplayEventBatchesAsync();
        if (batches.Count == 0)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var gameplayEventMessageWriter =
            scope.ServiceProvider.GetRequiredService<IGameplayEventMessageWriter>();
        var acknowledgedBatchIds = new List<Guid>(batches.Count);
        var persistedGameplayMessageCount = 0;

        foreach (var batch in batches)
        {
            try
            {
                persistedGameplayMessageCount +=
                    await gameplayEventMessageWriter.PersistResolvedBatchAsync(
                        batch.GameId,
                        batch.LastResolvedBatch,
                        batch.Completion,
                        batch.GameplayEvents,
                        cancellationToken
                    );
                acknowledgedBatchIds.Add(batch.BatchId);
            }
            catch (Exception exception)
            {
                LogPersistPendingBatchFailed(logger, exception, batch.BatchId, gameId);
            }
        }

        if (acknowledgedBatchIds.Count > 0)
        {
            try
            {
                await grain.AcknowledgeGameplayEventBatchesAsync(
                    new AcknowledgeGameplayEventBatchesGrainCommand(acknowledgedBatchIds)
                );
            }
            catch (Exception exception)
            {
                LogAcknowledgePendingBatchesFailed(logger, exception, gameId);
            }
        }

        if (persistedGameplayMessageCount > 0)
        {
            await PublishMessagesInvalidatedAsync(gameId, cancellationToken);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to publish lobby update for game {GameId}."
    )]
    private static partial void LogPublishLobbyUpdateFailed(
        ILogger logger,
        Exception exception,
        Guid gameId
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to publish session update for game {GameId}."
    )]
    private static partial void LogPublishSessionUpdateFailed(
        ILogger logger,
        Exception exception,
        Guid gameId
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to publish message update for game {GameId}."
    )]
    private static partial void LogPublishMessageUpdateFailed(
        ILogger logger,
        Exception exception,
        Guid gameId
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to publish presence update for game {GameId}."
    )]
    private static partial void LogPublishPresenceUpdateFailed(
        ILogger logger,
        Exception exception,
        Guid gameId
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to fetch presence for game {GameId}."
    )]
    private static partial void LogFetchPresenceFailed(
        ILogger logger,
        Exception exception,
        Guid gameId
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to ensure a game session for game {GameId}."
    )]
    private static partial void LogEnsureSessionFailed(
        ILogger logger,
        Exception exception,
        Guid gameId
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to fetch player view for game {GameId} player {PlayerId}. Session data unavailable."
    )]
    private static partial void LogFetchPlayerViewFailed(
        ILogger logger,
        Exception exception,
        Guid gameId,
        Guid playerId
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to submit acquire choice for game {GameId} player {PlayerId}."
    )]
    private static partial void LogSubmitAcquireFailed(
        ILogger logger,
        Exception exception,
        Guid gameId,
        Guid playerId
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to submit play batch for game {GameId} player {PlayerId}."
    )]
    private static partial void LogSubmitPlayBatchFailed(
        ILogger logger,
        Exception exception,
        Guid gameId,
        Guid playerId
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Gameplay command was rejected for game {GameId} player {PlayerId}: {ErrorMessage}"
    )]
    private static partial void LogGameCommandRejected(
        ILogger logger,
        Guid gameId,
        Guid playerId,
        string errorMessage
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to persist pending gameplay event batch {BatchId} for game {GameId}."
    )]
    private static partial void LogPersistPendingBatchFailed(
        ILogger logger,
        Exception exception,
        Guid batchId,
        Guid gameId
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to acknowledge persisted gameplay event batches for game {GameId}."
    )]
    private static partial void LogAcknowledgePendingBatchesFailed(
        ILogger logger,
        Exception exception,
        Guid gameId
    );
}

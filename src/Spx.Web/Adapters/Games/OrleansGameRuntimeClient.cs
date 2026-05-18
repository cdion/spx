using Orleans;
using Orleans.Runtime;
using Spx.Contracts;
using Spx.Game.Application;

namespace Spx.Web.Adapters.Games;

public sealed class OrleansGameRuntimeClient(
    IClusterClient clusterClient,
    IServiceScopeFactory scopeFactory,
    ILogger<OrleansGameRuntimeClient> logger)
    : IGameLobbyInvalidationPublisher,
      IGameSessionInvalidationPublisher,
      IGameMessageInvalidationPublisher,
      IGamePresenceInvalidationPublisher,
            IGamePresenceService,
      IGameSessionService
{
    public async Task PublishLobbyInvalidatedAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        try
        {
            await clusterClient.GetGrain<IGameInvalidationGrain>(gameId).PublishLobbyInvalidated();
        }
        catch (OrleansException exception)
        {
            logger.LogWarning(exception, "Failed to publish lobby update for game {GameId}.", gameId);
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(exception, "Failed to publish lobby update for game {GameId}.", gameId);
        }
    }

    public async Task PublishSessionInvalidatedAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        try
        {
            await clusterClient.GetGrain<IGameInvalidationGrain>(gameId).PublishSessionInvalidated();
        }
        catch (OrleansException exception)
        {
            logger.LogWarning(exception, "Failed to publish session update for game {GameId}.", gameId);
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(exception, "Failed to publish session update for game {GameId}.", gameId);
        }
    }

    public async Task PublishMessagesInvalidatedAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        try
        {
            await clusterClient.GetGrain<IGameInvalidationGrain>(gameId).PublishMessagesInvalidated();
        }
        catch (OrleansException exception)
        {
            logger.LogWarning(exception, "Failed to publish message update for game {GameId}.", gameId);
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(exception, "Failed to publish message update for game {GameId}.", gameId);
        }
    }

    public async Task PublishPresenceInvalidatedAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        try
        {
            await clusterClient.GetGrain<IGameInvalidationGrain>(gameId).PublishPresenceInvalidated();
        }
        catch (OrleansException exception)
        {
            logger.LogWarning(exception, "Failed to publish presence update for game {GameId}.", gameId);
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(exception, "Failed to publish presence update for game {GameId}.", gameId);
        }
    }

    public async Task<GamePresenceView> GetPresenceAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await clusterClient.GetGrain<IGamePresenceGrain>(gameId).GetSnapshotAsync();
            return new GamePresenceView(snapshot.OnlinePlayerIds);
        }
        catch (OrleansException exception)
        {
            logger.LogWarning(exception, "Failed to fetch presence for game {GameId}.", gameId);
            return GamePresenceView.Empty;
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(exception, "Failed to fetch presence for game {GameId}.", gameId);
            return GamePresenceView.Empty;
        }
    }

    public Task UpsertPresenceLeaseAsync(Guid gameId, Guid playerId, Guid connectionId, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
        => clusterClient.GetGrain<IGamePresenceGrain>(gameId).UpsertLeaseAsync(new UpsertGamePresenceLeaseCommand(playerId, connectionId, expiresAtUtc));

    public Task RemovePresenceLeaseAsync(Guid gameId, Guid playerId, Guid connectionId, CancellationToken cancellationToken = default)
        => clusterClient.GetGrain<IGamePresenceGrain>(gameId).RemoveLeaseAsync(new RemoveGamePresenceLeaseCommand(playerId, connectionId));

    public async Task<bool> EnsureSessionAsync(Guid gameId, IReadOnlyList<GameSessionParticipant> players, CancellationToken cancellationToken = default)
    {
        if (players.Count != 2)
        {
            return false;
        }

        try
        {
            await clusterClient.GetGrain<IGameSessionGrain>(gameId).InitializeAsync(new InitializeGameSessionGrainCommand(
                new GameSessionParticipantGrainView(players[0].PlayerId),
                new GameSessionParticipantGrainView(players[1].PlayerId)));
            return true;
        }
        catch (OrleansException exception)
        {
            logger.LogWarning(exception, "Failed to ensure a game session for game {GameId}.", gameId);
            return false;
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(exception, "Failed to ensure a game session for game {GameId}.", gameId);
            return false;
        }
    }

    public async Task<GameSessionView?> GetSessionAsync(Guid gameId, Guid playerId, CancellationToken cancellationToken = default)
    {
        try
        {
            await TryPersistPendingGameplayEventBatchesAsync(gameId, cancellationToken);
            var session = await clusterClient.GetGrain<IGameSessionGrain>(gameId).GetPlayerViewAsync(new GetGameSessionGrainQuery(playerId));
            return session is null ? null : MapSession(session);
        }
        catch (OrleansException exception)
        {
            logger.LogWarning(exception, "Failed to fetch player view for game {GameId} player {PlayerId}. Session data unavailable.", gameId, playerId);
            return null;
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(exception, "Failed to fetch player view for game {GameId} player {PlayerId}. Session data unavailable.", gameId, playerId);
            return null;
        }
    }

    public async Task<GameSessionCommandOutcome> SubmitAcquireAsync(Guid gameId, SubmitAcquireRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await clusterClient.GetGrain<IGameSessionGrain>(gameId).SubmitAcquireAsync(
                new SubmitAcquireGrainCommand(request.PlayerId, request.ExpectedRoundNumber, request.MarketCardInstanceId));
            return MapSessionCommandResult(gameId, request.PlayerId, result);
        }
        catch (OrleansException exception)
        {
            logger.LogWarning(exception, "Failed to submit acquire choice for game {GameId} player {PlayerId}.", gameId, request.PlayerId);
            throw;
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(exception, "Failed to submit acquire choice for game {GameId} player {PlayerId}.", gameId, request.PlayerId);
            throw;
        }
    }

    public async Task<GameSessionCommandOutcome> SubmitPlayBatchAsync(Guid gameId, SubmitPlayBatchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await clusterClient.GetGrain<IGameSessionGrain>(gameId).SubmitPlayBatchAsync(new SubmitPlayBatchGrainCommand(
                request.PlayerId,
                request.ExpectedRoundNumber,
                request.Cards.Select(MapBatchCardSelection).ToArray()));
            return MapSessionCommandResult(gameId, request.PlayerId, result);
        }
        catch (OrleansException exception)
        {
            logger.LogWarning(exception, "Failed to submit play batch for game {GameId} player {PlayerId}.", gameId, request.PlayerId);
            throw;
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(exception, "Failed to submit play batch for game {GameId} player {PlayerId}.", gameId, request.PlayerId);
            throw;
        }
    }

    public async Task AbandonAsync(Guid gameId, Guid playerId, CancellationToken cancellationToken = default)
    {
        await clusterClient.GetGrain<IGameSessionGrain>(gameId).AbandonAsync(new AbandonGameSessionGrainCommand(playerId));
    }

    public Task AcknowledgeGameplayEventBatchAsync(Guid gameId, Guid gameplayEventBatchId, CancellationToken cancellationToken = default)
        => clusterClient.GetGrain<IGameSessionGrain>(gameId).AcknowledgeGameplayEventBatchesAsync(new AcknowledgeGameplayEventBatchesGrainCommand([gameplayEventBatchId]));

    private GameSessionCommandOutcome MapSessionCommandResult(Guid gameId, Guid playerId, GameSessionGrainCommandResult result)
        => result switch
        {
            GameSessionGrainCommandSucceededResult succeeded => new GameSessionCommandSucceeded(MapSession(succeeded.Session), succeeded.GameplayEvents, succeeded.PendingGameplayEventBatchId),
            GameSessionGrainCommandRejectedResult rejected => LogRejectedCommand(gameId, playerId, rejected),
            _ => throw new InvalidOperationException("Unknown game session command result type.")
        };

    private static GameSessionView MapSession(GameSessionGrainView session)
        => new(
            session.GameId,
            session.RoundNumber,
            session.Phase,
            MapPlayer(session.CurrentPlayer),
            MapPlayer(session.OpponentPlayer),
            session.VisibleMarketCards.Select(MapCard).ToArray(),
            session.MarketDeckCount,
            session.WaitingForOpponent,
            session.CanAcquireCard,
            session.CanLockBatch,
            session.MaxBatchSize,
            session.LastResolvedBatch is null ? null : MapResolvedBatch(session.LastResolvedBatch),
            session.Completion is null ? null : MapCompletion(session.Completion));

    private static GameSessionParticipant MapParticipant(GameSessionParticipantGrainView participant)
        => new(participant.PlayerId);

    private static GamePlayerStateView MapPlayer(GamePlayerStateGrainView player)
        => new(
            MapParticipant(player.Participant),
            player.Hand.Select(MapCard).ToArray(),
            player.HasLockedBatch,
            player.LockedBatchCount,
            player.InitiativeScore,
            player.HasScoutOverride,
            player.PicksFirstInAcquirePhase,
            player.VisibleLockedCards.Select(MapBatchCard).ToArray());

    private static GameCardView MapCard(GameCardInstanceGrainView card)
        => new(card.CardInstanceId, card.Definition, card.DisplayName, card.Category, card.ResourceColor);

    private static GameCardReferenceView MapReference(GameCardReferenceGrainView reference)
        => new(reference.CardInstanceId, reference.ProducedByCardInstanceId, reference.ProducedCardDefinition);

    private static GameBatchCardView MapBatchCard(GameBatchCardGrainView card)
        => new(
            MapCard(card.Card),
            card.ChosenResourceColor,
            card.CraftedCardDefinition,
            card.TargetResourceColor,
            card.TargetCardInstanceId,
            card.ConsumedCards.Select(MapReference).ToArray());

    private static GameResolvedPlayerBatchView MapResolvedPlayerBatch(GameResolvedPlayerBatchGrainView batch)
        => new(
            MapParticipant(batch.Participant),
            batch.PlayedCards.Select(MapBatchCard).ToArray(),
            batch.ProducedVictory);

    private static GameResolvedBatchView MapResolvedBatch(GameResolvedBatchGrainView batch)
        => new(
            batch.RoundNumber,
            batch.Players.Select(MapResolvedPlayerBatch).ToArray(),
            batch.ResolvedAtUtc);

    private static GameCompletionView MapCompletion(GameCompletionGrainView completion)
        => new(
            completion.Reason,
            completion.Winner is null ? null : MapParticipant(completion.Winner),
            completion.CompletedAtUtc);

    private static GameCardReferenceGrainCommand MapCardReferenceSelection(GameCardReferenceSelection selection)
        => new(selection.CardInstanceId, selection.ProducedByCardInstanceId, selection.ProducedCardDefinition);

    private static GameBatchCardGrainCommand MapBatchCardSelection(GameBatchCardSelection selection)
        => new(
            selection.CardInstanceId,
            selection.ChosenResourceColor,
            selection.CraftedCardDefinition,
            selection.TargetResourceColor,
            selection.TargetCardInstanceId,
            selection.ConsumedCards.Select(MapCardReferenceSelection).ToArray());

    private GameSessionCommandFailed LogRejectedCommand(Guid gameId, Guid playerId, GameSessionGrainCommandRejectedResult rejected)
    {
        logger.LogInformation("Gameplay command was rejected for game {GameId} player {PlayerId}: {ErrorMessage}", gameId, playerId, rejected.ErrorMessage);
        return new GameSessionCommandFailed(rejected.ErrorMessage);
    }

    private async Task TryPersistPendingGameplayEventBatchesAsync(Guid gameId, CancellationToken cancellationToken)
    {
        var grain = clusterClient.GetGrain<IGameSessionGrain>(gameId);
        var batches = await grain.GetPendingGameplayEventBatchesAsync();
        if (batches.Count == 0)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var gameplayEventMessageWriter = scope.ServiceProvider.GetRequiredService<IGameplayEventMessageWriter>();
        var acknowledgedBatchIds = new List<Guid>(batches.Count);
        var persistedGameplayMessageCount = 0;

        foreach (var batch in batches)
        {
            try
            {
                persistedGameplayMessageCount += await gameplayEventMessageWriter.PersistResolvedBatchAsync(
                    batch.GameId,
                    batch.LastResolvedBatch is null ? null : MapResolvedBatch(batch.LastResolvedBatch),
                    batch.Completion is null ? null : MapCompletion(batch.Completion),
                    batch.GameplayEvents,
                    cancellationToken);
                acknowledgedBatchIds.Add(batch.BatchId);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to persist pending gameplay event batch {BatchId} for game {GameId}.", batch.BatchId, gameId);
            }
        }

        if (acknowledgedBatchIds.Count > 0)
        {
            try
            {
                await grain.AcknowledgeGameplayEventBatchesAsync(new AcknowledgeGameplayEventBatchesGrainCommand(acknowledgedBatchIds));
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to acknowledge persisted gameplay event batches for game {GameId}.", gameId);
            }
        }

        if (persistedGameplayMessageCount > 0)
        {
            await PublishMessagesInvalidatedAsync(gameId, cancellationToken);
        }
    }
}
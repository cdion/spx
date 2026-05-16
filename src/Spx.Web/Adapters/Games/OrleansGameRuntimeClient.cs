using Orleans;
using Orleans.Runtime;
using Spx.Contracts;
using Spx.Game.Application;

namespace Spx.Web.Adapters.Games;

public sealed class OrleansGameRuntimeClient(
    IClusterClient clusterClient,
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

    public async Task<bool> EnsureSessionAsync(Guid gameId, IReadOnlyList<GameSessionParticipantView> players, CancellationToken cancellationToken = default)
    {
        if (players.Count != 2)
        {
            return false;
        }

        try
        {
            await clusterClient.GetGrain<IGameSessionGrain>(gameId).InitializeAsync(new InitializeGameSessionCommand(players[0], players[1]));
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

    public async Task<GameSessionView?> GetSessionViewAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await clusterClient.GetGrain<IGameSessionGrain>(gameId).GetPlayerViewAsync(new GetGameSessionViewQuery(userId));
        }
        catch (OrleansException exception)
        {
            logger.LogWarning(exception, "Failed to fetch player view for game {GameId} user {UserId}. Session data unavailable.", gameId, userId);
            return null;
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(exception, "Failed to fetch player view for game {GameId} user {UserId}. Session data unavailable.", gameId, userId);
            return null;
        }
    }

    public async Task<GameSessionCommandOutcome> SubmitAcquireAsync(Guid gameId, SubmitAcquireCardCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await clusterClient.GetGrain<IGameSessionGrain>(gameId).SubmitAcquireAsync(command);
            return new GameSessionCommandSucceeded(session);
        }
        catch (InvalidOperationException exception)
        {
            logger.LogInformation(exception, "Acquire submission was rejected for game {GameId} user {UserId}.", gameId, command.UserId);
            return new GameSessionCommandFailed(exception.Message);
        }
        catch (OrleansException exception)
        {
            logger.LogWarning(exception, "Failed to submit acquire choice for game {GameId} user {UserId}.", gameId, command.UserId);
            throw;
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(exception, "Failed to submit acquire choice for game {GameId} user {UserId}.", gameId, command.UserId);
            throw;
        }
    }

    public async Task<GameSessionCommandOutcome> SubmitPlayBatchAsync(Guid gameId, SubmitPlayBatchCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await clusterClient.GetGrain<IGameSessionGrain>(gameId).SubmitPlayBatchAsync(command);
            return new GameSessionCommandSucceeded(result.Session, result.GameplayEvents);
        }
        catch (InvalidOperationException exception)
        {
            logger.LogInformation(exception, "Play batch submission was rejected for game {GameId} user {UserId}.", gameId, command.UserId);
            return new GameSessionCommandFailed(exception.Message);
        }
        catch (OrleansException exception)
        {
            logger.LogWarning(exception, "Failed to submit play batch for game {GameId} user {UserId}.", gameId, command.UserId);
            throw;
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(exception, "Failed to submit play batch for game {GameId} user {UserId}.", gameId, command.UserId);
            throw;
        }
    }

    public Task<GameSessionView> AbandonAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
        => clusterClient.GetGrain<IGameSessionGrain>(gameId).AbandonAsync(new AbandonGameSessionCommand(userId));
}
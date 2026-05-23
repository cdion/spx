using System.Collections.Immutable;
using Orleans;
using Orleans.Runtime;
using Spx.Contracts;
using Spx.Game.Application;
using Spx.Game.Domain;

namespace Spx.Web.Adapters.Games;

public sealed partial class OrleansGameRuntimeClient(
    IClusterClient clusterClient,
    ILogger<OrleansGameRuntimeClient> logger
)
    : IGameLobbyInvalidationPublisher,
        IGameSessionInvalidationPublisher,
        IGameMessageInvalidationPublisher,
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

    public async Task<bool> EnsureSessionAsync(
        Guid gameId,
        IReadOnlyList<GameSessionParticipant> players,
        CancellationToken cancellationToken = default
    )
    {
        if (players.Count is < 2 or > 4)
        {
            return false;
        }

        try
        {
            await clusterClient
                .GetGrain<IGameSessionGrain>(gameId)
                .InitializeAsync(
                    new InitializeNexusGameCommand(
                        players
                            .Select(p => new GameSessionParticipant(p.PlayerId))
                            .ToImmutableArray()
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

    public async Task<NexusGameView?> GetSessionAsync(
        Guid gameId,
        Guid playerId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await clusterClient.GetGrain<IGameSessionGrain>(gameId).GetViewAsync(playerId);
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

    public async Task<GameSessionCommandOutcome> SubmitOrdersAsync(
        Guid gameId,
        NexusTurnOrdersCommand command,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var result = await clusterClient
                .GetGrain<IGameSessionGrain>(gameId)
                .SubmitOrdersAsync(command);

            if (result is NexusTurnOrdersRejected rejected)
            {
                LogSubmitOrdersRejected(logger, gameId, command.PlayerId, rejected.ErrorMessage);
                return new GameSessionCommandFailed(rejected.ErrorMessage);
            }

            var view = await GetSessionAsync(gameId, command.PlayerId, cancellationToken);
            return new GameSessionCommandSucceeded(view!);
        }
        catch (OrleansException exception)
        {
            LogSubmitOrdersFailed(logger, exception, gameId, command.PlayerId);
            throw;
        }
        catch (TimeoutException exception)
        {
            LogSubmitOrdersFailed(logger, exception, gameId, command.PlayerId);
            throw;
        }
    }

    public async Task AbandonAsync(
        Guid gameId,
        Guid playerId,
        CancellationToken cancellationToken = default
    )
    {
        await clusterClient.GetGrain<IGameSessionGrain>(gameId).AbandonAsync(playerId);
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
        Level = LogLevel.Information,
        Message = "Orders rejected for game {GameId} player {PlayerId}: {ErrorMessage}"
    )]
    private static partial void LogSubmitOrdersRejected(
        ILogger logger,
        Guid gameId,
        Guid playerId,
        string errorMessage
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to submit orders for game {GameId} player {PlayerId}."
    )]
    private static partial void LogSubmitOrdersFailed(
        ILogger logger,
        Exception exception,
        Guid gameId,
        Guid playerId
    );
}

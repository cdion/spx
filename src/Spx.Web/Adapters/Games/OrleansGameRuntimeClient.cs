using Orleans;
using Spx.Contracts;
using Spx.Games;

namespace Spx.Web.Adapters.Games;

public sealed class OrleansGameRuntimeClient(
    IClusterClient clusterClient,
    ILogger<OrleansGameRuntimeClient> logger)
    : IGameLobbyEventsPublisher, IGameMessageEventsPublisher, IGameSessionService
{
    public async Task PublishLobbyChangedAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        try
        {
            await clusterClient.GetGrain<IGameLobbyEventsGrain>(gameId).PublishLobbyChanged();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to publish lobby update for game {GameId}.", gameId);
        }
    }

    public async Task PublishMessagesChangedAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        try
        {
            await clusterClient.GetGrain<IGameLobbyEventsGrain>(gameId).PublishMessagesChanged();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to publish message update for game {GameId}.", gameId);
        }
    }

    public async Task<bool> TryInitializeAsync(Guid gameId, IReadOnlyList<GameSessionParticipantView> players, CancellationToken cancellationToken = default)
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
        catch (Exception exception)
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
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to fetch player view for game {GameId} user {UserId}. Session data unavailable.", gameId, userId);
            return null;
        }
    }

    public async Task<GameSessionView> SubmitMoveAsync(Guid gameId, SubmitGameMoveCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            return await clusterClient.GetGrain<IGameSessionGrain>(gameId).SubmitMoveAsync(command);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to submit move for game {GameId} user {UserId}.", gameId, command.UserId);
            throw;
        }
    }

    public Task<GameSessionView> AbandonAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
        => clusterClient.GetGrain<IGameSessionGrain>(gameId).AbandonAsync(new AbandonGameSessionCommand(userId));
}
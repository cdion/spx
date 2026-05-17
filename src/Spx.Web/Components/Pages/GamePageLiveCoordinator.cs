using Microsoft.Extensions.Logging;
using Spx.Game.Application;

namespace Spx.Web.Components.Pages;

internal sealed class GamePageLiveCoordinator(
    IClusterClient clusterClient,
    IGamePresenceService gamePresenceService,
    Spx.Web.Circuits.CircuitConnectionEvents circuitConnectionEvents,
    ILogger<GamePageLiveCoordinator> logger,
    ILogger<GamePageSubscription> subscriptionLogger,
    Func<Task> onLobbyInvalidated,
    Func<Task> onSessionInvalidated,
    Func<Task> onMessagesInvalidated,
    Func<Task> onPresenceChanged) : IAsyncDisposable
{
    private const int PresenceLeaseSeconds = 10;
    private const int PresenceRenewalSeconds = 5;

    private GamePageSubscription? subscription;
    private Guid? trackedPresenceGameId;
    private Guid? trackedPresencePlayerId;
    private Guid? presenceConnectionId;
    private CancellationTokenSource? presenceRenewalCts;
    private PeriodicTimer? presenceRenewalTimer;
    private Task? presenceRenewalTask;

    public Guid? GameId => subscription?.GameId;

    public void Initialize()
    {
        circuitConnectionEvents.ConnectionDown += HandleConnectionDownAsync;
        circuitConnectionEvents.ConnectionUp += HandleConnectionUpAsync;
    }

    public async Task StartAsync(GameLobbyView lobby, CancellationToken cancellationToken = default)
    {
        if (subscription?.GameId != lobby.GameId)
        {
            await StopAsync();
            subscription = new GamePageSubscription(
                clusterClient,
                subscriptionLogger,
                lobby.GameId,
                onLobbyInvalidated,
                onSessionInvalidated,
                onMessagesInvalidated,
                onPresenceChanged);
            await subscription.SubscribeAsync(cancellationToken);
        }

        await EnsurePresenceTrackingAsync(lobby);
    }

    public async Task StopAsync()
    {
        await StopPresenceTrackingAsync();
        await DisposeSubscriptionAsync();
    }

    public async ValueTask DisposeAsync()
    {
        circuitConnectionEvents.ConnectionDown -= HandleConnectionDownAsync;
        circuitConnectionEvents.ConnectionUp -= HandleConnectionUpAsync;
        await StopAsync();
    }

    private async Task EnsurePresenceTrackingAsync(GameLobbyView lobby)
    {
        if (!lobby.IsCurrentUserActive)
        {
            await StopPresenceTrackingAsync();
            return;
        }

        var currentPlayerId = lobby.Players.SingleOrDefault(player => player.IsCurrentUser)?.PlayerId;
        if (!currentPlayerId.HasValue)
        {
            await StopPresenceTrackingAsync();
            return;
        }

        if (trackedPresenceGameId == lobby.GameId
            && trackedPresencePlayerId == currentPlayerId
            && presenceConnectionId.HasValue)
        {
            if (presenceRenewalTask is not null)
            {
                return;
            }

            await ResumePresenceTrackingAsync(lobby.GameId, currentPlayerId.Value, presenceConnectionId.Value);
            return;
        }

        await StopPresenceTrackingAsync();

        trackedPresenceGameId = lobby.GameId;
        trackedPresencePlayerId = currentPlayerId;
        presenceConnectionId = Guid.NewGuid();
        presenceRenewalCts = new CancellationTokenSource();
        presenceRenewalTimer = new PeriodicTimer(TimeSpan.FromSeconds(PresenceRenewalSeconds));

        await PublishPresenceLeaseAsync(lobby.GameId, currentPlayerId.Value, presenceConnectionId.Value, presenceRenewalCts.Token);
        await onPresenceChanged();
        presenceRenewalTask = RenewPresenceAsync(lobby.GameId, currentPlayerId.Value, presenceConnectionId.Value, presenceRenewalCts.Token);
    }

    private async Task ResumePresenceTrackingAsync(Guid gameId, Guid playerId, Guid connectionId)
    {
        presenceRenewalCts = new CancellationTokenSource();
        presenceRenewalTimer = new PeriodicTimer(TimeSpan.FromSeconds(PresenceRenewalSeconds));

        await PublishPresenceLeaseAsync(gameId, playerId, connectionId, presenceRenewalCts.Token);
        await onPresenceChanged();
        presenceRenewalTask = RenewPresenceAsync(gameId, playerId, connectionId, presenceRenewalCts.Token);
    }

    private async Task PausePresenceTrackingAsync()
    {
        var timer = presenceRenewalTimer;
        var cancellationSource = presenceRenewalCts;
        var renewalTask = presenceRenewalTask;

        presenceRenewalTimer = null;
        presenceRenewalCts = null;
        presenceRenewalTask = null;

        cancellationSource?.Cancel();

        if (renewalTask is not null)
        {
            try
            {
                await renewalTask;
            }
            catch (OperationCanceledException exception)
            {
                logger.LogDebug(exception, "Presence renewal task finished after cancellation.");
            }
        }

        timer?.Dispose();
        cancellationSource?.Dispose();
    }

    private async Task StopPresenceTrackingAsync()
    {
        var gameId = trackedPresenceGameId;
        var playerId = trackedPresencePlayerId;
        var connectionId = presenceConnectionId;

        trackedPresenceGameId = null;
        trackedPresencePlayerId = null;
        presenceConnectionId = null;

        await PausePresenceTrackingAsync();

        if (!gameId.HasValue || !playerId.HasValue || !connectionId.HasValue)
        {
            return;
        }

        try
        {
            await gamePresenceService.RemovePresenceLeaseAsync(gameId.Value, playerId.Value, connectionId.Value);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to remove presence lease for game {GameId} player {PlayerId}.", gameId.Value, playerId.Value);
        }
    }

    private async Task RenewPresenceAsync(Guid gameId, Guid playerId, Guid connectionId, CancellationToken cancellationToken)
    {
        if (presenceRenewalTimer is null)
        {
            return;
        }

        try
        {
            while (await presenceRenewalTimer.WaitForNextTickAsync(cancellationToken))
            {
                await PublishPresenceLeaseAsync(gameId, playerId, connectionId, cancellationToken);
            }
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Presence renewal loop stopped for game {GameId} player {PlayerId} because tracking was canceled.", gameId, playerId);
        }
    }

    private async Task PublishPresenceLeaseAsync(Guid gameId, Guid playerId, Guid connectionId, CancellationToken cancellationToken)
    {
        try
        {
            await gamePresenceService.UpsertPresenceLeaseAsync(
                gameId,
                playerId,
                connectionId,
                DateTime.UtcNow.AddSeconds(PresenceLeaseSeconds),
                cancellationToken);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Ignoring canceled presence lease renewal for game {GameId} player {PlayerId}.", gameId, playerId);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to renew presence for game {GameId} player {PlayerId}.", gameId, playerId);
        }
    }

    private async Task DisposeSubscriptionAsync()
    {
        if (subscription is null)
        {
            return;
        }

        await subscription.DisposeAsync();
        subscription = null;
    }

    private Task HandleConnectionDownAsync()
        => PausePresenceTrackingAsync();

    private async Task HandleConnectionUpAsync()
    {
        if (trackedPresenceGameId.HasValue && trackedPresencePlayerId.HasValue && presenceConnectionId.HasValue && presenceRenewalTask is null)
        {
            await ResumePresenceTrackingAsync(trackedPresenceGameId.Value, trackedPresencePlayerId.Value, presenceConnectionId.Value);
        }
    }
}
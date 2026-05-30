using Spx.Game.Application.Features.GetGamePresence;
using Spx.Web.Hubs;
using Spx.Web.Presence;

namespace Spx.Web.Components.Pages.Nexus;

internal sealed partial class NexusPagePresenceCoordinator(
    IGetGamePresenceHandler getGamePresenceHandler,
    IGameInvalidationNotifier invalidationNotifier,
    NexusPresenceLeaseCoordinator presenceLeaseCoordinator,
    ILogger<NexusPagePresenceCoordinator> logger,
    NexusPageDataState data,
    NexusPagePresenceState state
)
{
    private static readonly TimeSpan PresenceLeaseDuration = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan PresenceLeaseRenewInterval = TimeSpan.FromSeconds(15);

    public bool HasConnectedGame => state.HasConnectedGame;

    public bool IsConnectedTo(Guid gameId) => state.IsConnectedTo(gameId);

    public async Task LoadPresenceAsync(CancellationToken cancellationToken = default)
    {
        if (data.Lobby is null)
        {
            data.ClearPresence();
            return;
        }

        try
        {
            data.ApplyPresence(
                await getGamePresenceHandler.HandleAsync(data.Lobby.GameId, cancellationToken)
            );
        }
        catch (Exception exception)
        {
            LogRefreshPresenceFailed(logger, exception, data.Lobby.GameId);
        }
    }

    public async Task ConnectAsync(
        Guid gameId,
        IGameInvalidationSubscriber subscriber,
        CancellationToken cancellationToken = default
    )
    {
        await invalidationNotifier.SubscribeAsync(gameId, subscriber);
        state.SetConnectedGame(gameId);
        await SyncAsync(cancellationToken);
    }

    public async Task ChangeGameAsync(
        Guid gameId,
        IGameInvalidationSubscriber subscriber,
        CancellationToken cancellationToken = default
    )
    {
        if (state.IsConnectedTo(gameId))
        {
            await SyncAsync(cancellationToken);
            return;
        }

        await DisconnectAsync(subscriber, cancellationToken);
        await ConnectAsync(gameId, subscriber, cancellationToken);
    }

    public async Task DisconnectAsync(
        IGameInvalidationSubscriber subscriber,
        CancellationToken cancellationToken = default
    )
    {
        if (!state.HasConnectedGame)
        {
            return;
        }

        var connectedGameId = state.ConnectedGameId;
        await invalidationNotifier.UnsubscribeAsync(connectedGameId, subscriber);
        await StopPresenceLeaseAsync(cancellationToken);
        state.ClearConnectedGame();
    }

    public Task DisposeAsync(
        IGameInvalidationSubscriber subscriber,
        CancellationToken cancellationToken = default
    ) => DisconnectAsync(subscriber, cancellationToken);

    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        if (data.Lobby is null || !state.HasConnectedGame)
        {
            return;
        }

        var shouldBeOnline = data.Lobby.IsCurrentUserActive;
        var nextPlayerId = data.Lobby.CurrentPlayerId;

        if (
            state.IsPresenceRegistered
            && (!shouldBeOnline || state.ConnectedPresencePlayerId != nextPlayerId)
        )
        {
            await StopPresenceLeaseAsync(cancellationToken);
        }

        if (!shouldBeOnline)
        {
            return;
        }

        await EnsurePresenceLeaseAsync(nextPlayerId, cancellationToken);
    }

    private async Task EnsurePresenceLeaseAsync(
        Guid playerId,
        CancellationToken cancellationToken = default
    )
    {
        var isNewLease = !state.IsPresenceRegistered;
        var leaseId = state.EnsurePresenceLease(playerId);
        if (isNewLease)
        {
            EnsurePresenceLeaseRenewalLoop();
        }

        await presenceLeaseCoordinator.RenewAsync(
            state.ConnectedGameId,
            playerId,
            leaseId,
            PresenceLeaseDuration,
            cancellationToken
        );

        if (isNewLease)
        {
            await LoadPresenceAsync(cancellationToken);
        }
    }

    private async Task StopPresenceLeaseAsync(CancellationToken cancellationToken = default)
    {
        if (!state.IsPresenceRegistered)
        {
            await StopPresenceLeaseRenewalLoopAsync();
            return;
        }

        var leaseId = state.ClearPresenceLease();
        await StopPresenceLeaseRenewalLoopAsync();
        await presenceLeaseCoordinator.RevokeAsync(leaseId, cancellationToken);
    }

    private void EnsurePresenceLeaseRenewalLoop()
    {
        if (!state.TryBeginRenewalLoop(out var cancellationToken))
        {
            return;
        }

        state.SetRenewalLoopTask(
            Task.Run(
                async () =>
                {
                    try
                    {
                        using var timer = new PeriodicTimer(PresenceLeaseRenewInterval);
                        while (await timer.WaitForNextTickAsync(cancellationToken))
                        {
                            if (!state.IsPresenceRegistered || !state.HasConnectedGame)
                            {
                                continue;
                            }

                            try
                            {
                                await presenceLeaseCoordinator.RenewAsync(
                                    state.ConnectedGameId,
                                    state.ConnectedPresencePlayerId,
                                    state.ConnectedPresenceLeaseId,
                                    PresenceLeaseDuration,
                                    cancellationToken
                                );
                            }
                            catch (OperationCanceledException)
                                when (cancellationToken.IsCancellationRequested)
                            {
                                return;
                            }
                            catch (Exception exception)
                            {
                                LogRenewPresenceLeaseFailed(
                                    logger,
                                    exception,
                                    state.ConnectedGameId,
                                    state.ConnectedPresencePlayerId
                                );
                            }
                        }
                    }
                    catch (OperationCanceledException)
                        when (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                },
                cancellationToken
            )
        );
    }

    private async Task StopPresenceLeaseRenewalLoopAsync()
    {
        var (cancellationTokenSource, renewalTask) = state.StopRenewalLoop();
        if (cancellationTokenSource is null)
        {
            return;
        }

        cancellationTokenSource.Cancel();

        if (renewalTask is not null)
        {
            try
            {
                await renewalTask;
            }
            catch (OperationCanceledException) { }
        }

        cancellationTokenSource.Dispose();
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to refresh presence for game {GameId}."
    )]
    private static partial void LogRefreshPresenceFailed(
        ILogger logger,
        Exception exception,
        Guid gameId
    );

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to renew presence lease for game {GameId} player {PlayerId}."
    )]
    private static partial void LogRenewPresenceLeaseFailed(
        ILogger logger,
        Exception exception,
        Guid gameId,
        Guid playerId
    );
}

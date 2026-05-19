using Orleans;
using Orleans.Runtime;
using Spx.Contracts;

namespace Spx.Grains;

public sealed class GamePresenceGrain : Grain, IGamePresenceGrain
{
    private static readonly TimeSpan ExpiryTimerInterval = TimeSpan.FromSeconds(5);

    private readonly Dictionary<Guid, Dictionary<Guid, DateTime>> leasesByPlayerId = [];
    private IGrainTimer? expiryTimer;

    public Task UpsertLeaseAsync(UpsertGamePresenceLeaseCommand command) =>
        ApplyMutationAsync(nowUtc =>
            GamePresenceTracker.UpsertLease(
                leasesByPlayerId,
                command.PlayerId,
                command.ConnectionId,
                command.ExpiresAtUtc,
                nowUtc
            )
        );

    public Task RemoveLeaseAsync(RemoveGamePresenceLeaseCommand command) =>
        ApplyMutationAsync(nowUtc =>
            GamePresenceTracker.RemoveLease(
                leasesByPlayerId,
                command.PlayerId,
                command.ConnectionId,
                nowUtc
            )
        );

    public Task<GamePresenceSnapshot> GetSnapshotAsync()
    {
        var nowUtc = DateTime.UtcNow;
        GamePresenceTracker.PruneExpiredLeases(leasesByPlayerId, nowUtc);
        EnsureExpiryTimer(nowUtc);
        return Task.FromResult(GamePresenceTracker.CreateSnapshot(leasesByPlayerId));
    }

    public override Task OnDeactivateAsync(
        DeactivationReason reason,
        CancellationToken cancellationToken
    )
    {
        expiryTimer?.Dispose();
        expiryTimer = null;
        return Task.CompletedTask;
    }

    private async Task ApplyMutationAsync(Func<DateTime, bool> mutate)
    {
        var nowUtc = DateTime.UtcNow;
        var changed = mutate(nowUtc);
        EnsureExpiryTimer(nowUtc);

        if (!changed)
        {
            return;
        }

        await GrainFactory
            .GetGrain<IGameInvalidationGrain>(this.GetPrimaryKey())
            .PublishPresenceInvalidated();
    }

    private void EnsureExpiryTimer(DateTime nowUtc)
    {
        if (leasesByPlayerId.Count == 0)
        {
            expiryTimer?.Dispose();
            expiryTimer = null;
            DelayDeactivation(TimeSpan.FromSeconds(-1));
            return;
        }

        var latestExpiryUtc = leasesByPlayerId.Values.SelectMany(leases => leases.Values).Max();
        var minimumLifetime = latestExpiryUtc - nowUtc + ExpiryTimerInterval;
        DelayDeactivation(minimumLifetime > TimeSpan.Zero ? minimumLifetime : ExpiryTimerInterval);

        expiryTimer ??= this.RegisterGrainTimer(
            static (state, cancellationToken) => state.PruneExpiredLeasesAsync(cancellationToken),
            this,
            new GrainTimerCreationOptions
            {
                DueTime = ExpiryTimerInterval,
                Period = ExpiryTimerInterval,
                KeepAlive = true,
            }
        );
    }

    private async Task PruneExpiredLeasesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var nowUtc = DateTime.UtcNow;
        var changed = GamePresenceTracker.PruneExpiredLeases(leasesByPlayerId, nowUtc);
        EnsureExpiryTimer(nowUtc);

        if (!changed)
        {
            return;
        }

        await GrainFactory
            .GetGrain<IGameInvalidationGrain>(this.GetPrimaryKey())
            .PublishPresenceInvalidated();
    }
}

internal static class GamePresenceTracker
{
    public static bool UpsertLease(
        IDictionary<Guid, Dictionary<Guid, DateTime>> leasesByPlayerId,
        Guid playerId,
        Guid connectionId,
        DateTime expiresAtUtc,
        DateTime nowUtc
    )
    {
        var before = CaptureOnlinePlayerIds(leasesByPlayerId, nowUtc);
        PruneExpiredLeasesInternal(leasesByPlayerId, nowUtc);

        if (!leasesByPlayerId.TryGetValue(playerId, out var leases))
        {
            leases = [];
            leasesByPlayerId[playerId] = leases;
        }

        leases[connectionId] = expiresAtUtc;
        return !before.SetEquals(CaptureOnlinePlayerIds(leasesByPlayerId, nowUtc));
    }

    public static bool RemoveLease(
        IDictionary<Guid, Dictionary<Guid, DateTime>> leasesByPlayerId,
        Guid playerId,
        Guid connectionId,
        DateTime nowUtc
    )
    {
        var before = CaptureOnlinePlayerIds(leasesByPlayerId, nowUtc);
        PruneExpiredLeasesInternal(leasesByPlayerId, nowUtc);

        if (!leasesByPlayerId.TryGetValue(playerId, out var leases))
        {
            return false;
        }

        leases.Remove(connectionId);
        if (leases.Count == 0)
        {
            leasesByPlayerId.Remove(playerId);
        }

        return !before.SetEquals(CaptureOnlinePlayerIds(leasesByPlayerId, nowUtc));
    }

    public static bool PruneExpiredLeases(
        IDictionary<Guid, Dictionary<Guid, DateTime>> leasesByPlayerId,
        DateTime nowUtc
    )
    {
        var before = CaptureRepresentedPlayerIds(leasesByPlayerId);
        PruneExpiredLeasesInternal(leasesByPlayerId, nowUtc);
        return !before.SetEquals(CaptureRepresentedPlayerIds(leasesByPlayerId));
    }

    public static GamePresenceSnapshot CreateSnapshot(
        IDictionary<Guid, Dictionary<Guid, DateTime>> leasesByPlayerId
    ) => new(CaptureRepresentedPlayerIds(leasesByPlayerId).OrderBy(playerId => playerId).ToArray());

    private static void PruneExpiredLeasesInternal(
        IDictionary<Guid, Dictionary<Guid, DateTime>> leasesByPlayerId,
        DateTime nowUtc
    )
    {
        List<Guid>? emptyPlayers = null;

        foreach (var (playerId, leases) in leasesByPlayerId)
        {
            var expiredConnectionIds = leases
                .Where(entry => entry.Value <= nowUtc)
                .Select(entry => entry.Key)
                .ToList();

            foreach (var connectionId in expiredConnectionIds)
            {
                leases.Remove(connectionId);
            }

            if (leases.Count == 0)
            {
                emptyPlayers ??= [];
                emptyPlayers.Add(playerId);
            }
        }

        if (emptyPlayers is null)
        {
            return;
        }

        foreach (var playerId in emptyPlayers)
        {
            leasesByPlayerId.Remove(playerId);
        }
    }

    private static HashSet<Guid> CaptureRepresentedPlayerIds(
        IDictionary<Guid, Dictionary<Guid, DateTime>> leasesByPlayerId
    ) => leasesByPlayerId.Keys.ToHashSet();

    private static HashSet<Guid> CaptureOnlinePlayerIds(
        IDictionary<Guid, Dictionary<Guid, DateTime>> leasesByPlayerId,
        DateTime nowUtc
    ) =>
        leasesByPlayerId
            .Where(entry => entry.Value.Any(lease => lease.Value > nowUtc))
            .Select(entry => entry.Key)
            .ToHashSet();
}

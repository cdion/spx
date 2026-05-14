using Orleans;
using Spx.Contracts;

namespace Spx.Grains;

public sealed class GamePresenceGrain : Grain, IGamePresenceGrain
{
    private readonly Dictionary<Guid, Dictionary<Guid, DateTime>> leasesByPlayerId = [];
    private IDisposable? expiryTimer;

    public Task UpsertLeaseAsync(UpsertGamePresenceLeaseCommand command)
        => ApplyMutationAsync(nowUtc => GamePresenceTracker.UpsertLease(leasesByPlayerId, command.PlayerId, command.ConnectionId, command.ExpiresAtUtc, nowUtc));

    public Task RemoveLeaseAsync(RemoveGamePresenceLeaseCommand command)
        => ApplyMutationAsync(nowUtc => GamePresenceTracker.RemoveLease(leasesByPlayerId, command.PlayerId, command.ConnectionId, nowUtc));

    public Task<GamePresenceSnapshot> GetSnapshotAsync()
    {
        GamePresenceTracker.PruneExpiredLeases(leasesByPlayerId, DateTime.UtcNow);
        EnsureExpiryTimer();
        return Task.FromResult(GamePresenceTracker.CreateSnapshot(leasesByPlayerId));
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        expiryTimer?.Dispose();
        expiryTimer = null;
        return Task.CompletedTask;
    }

    private async Task ApplyMutationAsync(Func<DateTime, bool> mutate)
    {
        var nowUtc = DateTime.UtcNow;
        var changed = mutate(nowUtc);
        EnsureExpiryTimer();

        if (!changed)
        {
            return;
        }

        await GrainFactory.GetGrain<IGameInvalidationGrain>(this.GetPrimaryKey()).PublishPresenceInvalidated();
    }

    private void EnsureExpiryTimer()
    {
        if (leasesByPlayerId.Count == 0)
        {
            expiryTimer?.Dispose();
            expiryTimer = null;
            return;
        }

#pragma warning disable CS0618
        expiryTimer ??= RegisterTimer(
            _ => PruneExpiredLeasesAsync(),
            state: null,
            dueTime: TimeSpan.FromSeconds(5),
            period: TimeSpan.FromSeconds(5));
#pragma warning restore CS0618
    }

    private async Task PruneExpiredLeasesAsync()
    {
        var nowUtc = DateTime.UtcNow;
        var changed = GamePresenceTracker.PruneExpiredLeases(leasesByPlayerId, nowUtc);
        EnsureExpiryTimer();

        if (!changed)
        {
            return;
        }

        await GrainFactory.GetGrain<IGameInvalidationGrain>(this.GetPrimaryKey()).PublishPresenceInvalidated();
    }
}

internal static class GamePresenceTracker
{
    public static bool UpsertLease(
        IDictionary<Guid, Dictionary<Guid, DateTime>> leasesByPlayerId,
        Guid playerId,
        Guid connectionId,
        DateTime expiresAtUtc,
        DateTime nowUtc)
    {
        var before = CaptureOnlinePlayerIds(leasesByPlayerId, nowUtc);
        PruneExpiredLeasesInternal(leasesByPlayerId, nowUtc);

        if (!leasesByPlayerId.TryGetValue(playerId, out var leases))
        {
            leases = [];
            leasesByPlayerId[playerId] = leases;
        }

        leases[connectionId] = expiresAtUtc;
        return !before.SetEquals(CaptureRepresentedPlayerIds(leasesByPlayerId));
    }

    public static bool RemoveLease(
        IDictionary<Guid, Dictionary<Guid, DateTime>> leasesByPlayerId,
        Guid playerId,
        Guid connectionId,
        DateTime nowUtc)
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

        return !before.SetEquals(CaptureRepresentedPlayerIds(leasesByPlayerId));
    }

    public static bool PruneExpiredLeases(IDictionary<Guid, Dictionary<Guid, DateTime>> leasesByPlayerId, DateTime nowUtc)
    {
        var before = CaptureRepresentedPlayerIds(leasesByPlayerId);
        PruneExpiredLeasesInternal(leasesByPlayerId, nowUtc);
        return !before.SetEquals(CaptureRepresentedPlayerIds(leasesByPlayerId));
    }

    public static GamePresenceSnapshot CreateSnapshot(IDictionary<Guid, Dictionary<Guid, DateTime>> leasesByPlayerId)
        => new(CaptureRepresentedPlayerIds(leasesByPlayerId).OrderBy(playerId => playerId).ToArray());

    private static void PruneExpiredLeasesInternal(IDictionary<Guid, Dictionary<Guid, DateTime>> leasesByPlayerId, DateTime nowUtc)
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

    private static HashSet<Guid> CaptureRepresentedPlayerIds(IDictionary<Guid, Dictionary<Guid, DateTime>> leasesByPlayerId)
        => leasesByPlayerId.Keys.ToHashSet();

    private static HashSet<Guid> CaptureOnlinePlayerIds(
        IDictionary<Guid, Dictionary<Guid, DateTime>> leasesByPlayerId,
        DateTime nowUtc)
        => leasesByPlayerId
            .Where(entry => entry.Value.Any(lease => lease.Value > nowUtc))
            .Select(entry => entry.Key)
            .ToHashSet();
}
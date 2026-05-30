using System.Collections.Immutable;
using Orleans;
using Orleans.Runtime;
using Spx.Contracts;

namespace Spx.Grains;

public sealed class GamePresenceGrain : Grain, IGamePresenceGrain
{
    private static readonly TimeSpan LeaseSweepInterval = TimeSpan.FromSeconds(5);
    private readonly Dictionary<Guid, PresenceLease> presenceLeases = [];
    private ImmutableArray<Guid> lastPublishedOnlinePlayers = [];
    private IGrainTimer? leaseSweepTimer;

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        leaseSweepTimer = this.RegisterGrainTimer(
            static (state, cancellationToken) =>
                ((GamePresenceGrain)state!).SweepExpiredLeasesAsync(),
            this,
            new()
            {
                DueTime = LeaseSweepInterval,
                Period = LeaseSweepInterval,
                Interleave = true,
                KeepAlive = true,
            }
        );

        return Task.CompletedTask;
    }

    public async Task RenewLeaseAsync(Guid playerId, Guid leaseId, TimeSpan ttl)
    {
        var now = DateTime.UtcNow;

        DelayDeactivation(ttl + LeaseSweepInterval);
        PruneExpiredLeases(now);
        presenceLeases[leaseId] = new PresenceLease(playerId, now.Add(ttl));

        await PublishPresenceInvalidatedIfChangedAsync(now);
    }

    public async Task RevokeLeaseAsync(Guid leaseId)
    {
        var now = DateTime.UtcNow;

        PruneExpiredLeases(now);
        presenceLeases.Remove(leaseId);

        if (presenceLeases.Count == 0)
        {
            DeactivateOnIdle();
        }

        await PublishPresenceInvalidatedIfChangedAsync(now);
    }

    public Task<GamePresenceSnapshot> GetSnapshotAsync() =>
        Task.FromResult(BuildSnapshot(DateTime.UtcNow));

    private async Task SweepExpiredLeasesAsync()
    {
        var now = DateTime.UtcNow;

        PruneExpiredLeases(now);

        if (presenceLeases.Count == 0)
        {
            DeactivateOnIdle();
        }

        await PublishPresenceInvalidatedIfChangedAsync(now);
    }

    private GamePresenceSnapshot BuildSnapshot(DateTime now)
    {
        PruneExpiredLeases(now);
        return new GamePresenceSnapshot(GetOnlinePlayerIds(now));
    }

    private ImmutableArray<Guid> GetOnlinePlayerIds(DateTime now) =>
        presenceLeases
            .Values.Where(lease => lease.ExpiresAtUtc > now)
            .Select(lease => lease.PlayerId)
            .Distinct()
            .OrderBy(id => id)
            .ToImmutableArray();

    private void PruneExpiredLeases(DateTime now)
    {
        if (presenceLeases.Count == 0)
        {
            return;
        }

        foreach (
            var leaseId in presenceLeases
                .Where(entry => entry.Value.ExpiresAtUtc <= now)
                .Select(entry => entry.Key)
                .ToArray()
        )
        {
            presenceLeases.Remove(leaseId);
        }
    }

    private async Task PublishPresenceInvalidatedIfChangedAsync(DateTime now)
    {
        var currentOnlinePlayers = GetOnlinePlayerIds(now);
        if (lastPublishedOnlinePlayers.SequenceEqual(currentOnlinePlayers))
        {
            return;
        }

        lastPublishedOnlinePlayers = currentOnlinePlayers;

        await GrainFactory
            .GetGrain<ILobbyInvalidationGrain>(this.GetPrimaryKey())
            .PublishPresenceInvalidated();
    }

    private sealed record PresenceLease(Guid PlayerId, DateTime ExpiresAtUtc);
}

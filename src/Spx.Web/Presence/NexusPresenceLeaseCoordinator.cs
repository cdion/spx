using Microsoft.Extensions.Logging;
using Orleans;
using Spx.Contracts;

namespace Spx.Web.Presence;

internal sealed partial class NexusPresenceLeaseCoordinator(IClusterClient clusterClient)
    : IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly Dictionary<Guid, ActivePresenceLease> activeLeases = [];
    private bool canRenew = true;

    public async Task RenewAsync(
        Guid gameId,
        Guid playerId,
        Guid leaseId,
        TimeSpan ttl,
        CancellationToken cancellationToken = default
    )
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (!canRenew)
            {
                return;
            }

            activeLeases[leaseId] = new ActivePresenceLease(gameId, leaseId);
        }
        finally
        {
            gate.Release();
        }

        try
        {
            await clusterClient
                .GetGrain<IGamePresenceGrain>(gameId)
                .RenewLeaseAsync(playerId, leaseId, ttl);
        }
        catch
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                activeLeases.Remove(leaseId);
            }
            finally
            {
                gate.Release();
            }

            throw;
        }
    }

    public async Task RevokeAsync(Guid leaseId, CancellationToken cancellationToken = default)
    {
        ActivePresenceLease? lease;

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (!activeLeases.Remove(leaseId, out lease))
            {
                return;
            }
        }
        finally
        {
            gate.Release();
        }

        await clusterClient
            .GetGrain<IGamePresenceGrain>(lease.GameId)
            .RevokeLeaseAsync(lease.LeaseId);
    }

    public Task OnConnectionUpAsync(CancellationToken cancellationToken = default)
    {
        canRenew = true;
        return Task.CompletedTask;
    }

    public Task OnConnectionDownAsync(CancellationToken cancellationToken = default) =>
        RevokeAllAsync(allowRenewAfterward: false, cancellationToken);

    public Task OnCircuitClosedAsync(CancellationToken cancellationToken = default) =>
        RevokeAllAsync(allowRenewAfterward: false, cancellationToken);

    public void Dispose() { }

    private async Task RevokeAllAsync(bool allowRenewAfterward, CancellationToken cancellationToken)
    {
        ActivePresenceLease[] leasesToRevoke;

        await gate.WaitAsync(cancellationToken);
        try
        {
            canRenew = allowRenewAfterward;
            leasesToRevoke = activeLeases.Values.ToArray();
            activeLeases.Clear();
        }
        finally
        {
            gate.Release();
        }

        foreach (var lease in leasesToRevoke)
        {
            await clusterClient
                .GetGrain<IGamePresenceGrain>(lease.GameId)
                .RevokeLeaseAsync(lease.LeaseId);
        }
    }

    private sealed record ActivePresenceLease(Guid GameId, Guid LeaseId);
}

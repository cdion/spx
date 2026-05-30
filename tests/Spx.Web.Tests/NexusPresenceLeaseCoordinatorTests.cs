using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Orleans;
using Spx.Contracts;
using Spx.Web.Presence;
using Xunit;

namespace Spx.Web.Tests;

public sealed class NexusPresenceLeaseCoordinatorTests
{
    [Fact]
    public async Task RenewAsync_publishes_lease_to_presence_grain()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var leaseId = Guid.NewGuid();
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IGamePresenceGrain>();
        clusterClient.GetGrain<IGamePresenceGrain>(gameId).Returns(grain);
        var coordinator = new NexusPresenceLeaseCoordinator(clusterClient);

        await coordinator.RenewAsync(gameId, playerId, leaseId, TimeSpan.FromMinutes(1));

        await grain.Received(1).RenewLeaseAsync(playerId, leaseId, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task RevokeAsync_removes_tracked_lease_from_presence_grain()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var leaseId = Guid.NewGuid();
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IGamePresenceGrain>();
        clusterClient.GetGrain<IGamePresenceGrain>(gameId).Returns(grain);
        var coordinator = new NexusPresenceLeaseCoordinator(clusterClient);

        await coordinator.RenewAsync(gameId, playerId, leaseId, TimeSpan.FromMinutes(1));
        await coordinator.RevokeAsync(leaseId);

        await grain.Received(1).RevokeLeaseAsync(leaseId);
    }

    [Fact]
    public async Task RevokeAsync_still_revokes_when_coordinator_was_disposed_first()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var leaseId = Guid.NewGuid();
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IGamePresenceGrain>();
        clusterClient.GetGrain<IGamePresenceGrain>(gameId).Returns(grain);
        var coordinator = new NexusPresenceLeaseCoordinator(clusterClient);

        await coordinator.RenewAsync(gameId, playerId, leaseId, TimeSpan.FromMinutes(1));

        coordinator.Dispose();

        await coordinator.RevokeAsync(leaseId);

        await grain.Received(1).RevokeLeaseAsync(leaseId);
    }

    [Fact]
    public async Task RevokeAsync_is_a_noop_after_circuit_closed_already_revoked_the_lease()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var leaseId = Guid.NewGuid();
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IGamePresenceGrain>();
        clusterClient.GetGrain<IGamePresenceGrain>(gameId).Returns(grain);
        var coordinator = new NexusPresenceLeaseCoordinator(clusterClient);

        await coordinator.RenewAsync(gameId, playerId, leaseId, TimeSpan.FromMinutes(1));

        await coordinator.OnCircuitClosedAsync();
        await coordinator.RevokeAsync(leaseId);

        await grain.Received(1).RevokeLeaseAsync(leaseId);
    }

    [Fact]
    public async Task OnConnectionDownAsync_revokes_all_active_leases_and_blocks_renewals()
    {
        var firstGameId = Guid.NewGuid();
        var secondGameId = Guid.NewGuid();
        var firstPlayerId = Guid.NewGuid();
        var secondPlayerId = Guid.NewGuid();
        var firstLeaseId = Guid.NewGuid();
        var secondLeaseId = Guid.NewGuid();
        var clusterClient = Substitute.For<IClusterClient>();
        var firstGrain = Substitute.For<IGamePresenceGrain>();
        var secondGrain = Substitute.For<IGamePresenceGrain>();
        clusterClient.GetGrain<IGamePresenceGrain>(firstGameId).Returns(firstGrain);
        clusterClient.GetGrain<IGamePresenceGrain>(secondGameId).Returns(secondGrain);
        var coordinator = new NexusPresenceLeaseCoordinator(clusterClient);

        await coordinator.RenewAsync(
            firstGameId,
            firstPlayerId,
            firstLeaseId,
            TimeSpan.FromMinutes(1)
        );
        await coordinator.RenewAsync(
            secondGameId,
            secondPlayerId,
            secondLeaseId,
            TimeSpan.FromMinutes(1)
        );

        await coordinator.OnConnectionDownAsync();
        await coordinator.RenewAsync(
            firstGameId,
            firstPlayerId,
            firstLeaseId,
            TimeSpan.FromMinutes(1)
        );

        await firstGrain.Received(1).RevokeLeaseAsync(firstLeaseId);
        await secondGrain.Received(1).RevokeLeaseAsync(secondLeaseId);
        await firstGrain
            .Received(1)
            .RenewLeaseAsync(firstPlayerId, firstLeaseId, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task OnConnectionUpAsync_allows_renewals_again()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var leaseId = Guid.NewGuid();
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IGamePresenceGrain>();
        clusterClient.GetGrain<IGamePresenceGrain>(gameId).Returns(grain);
        var coordinator = new NexusPresenceLeaseCoordinator(clusterClient);

        await coordinator.OnConnectionDownAsync();
        await coordinator.RenewAsync(gameId, playerId, leaseId, TimeSpan.FromMinutes(1));
        await coordinator.OnConnectionUpAsync();
        await coordinator.RenewAsync(gameId, playerId, leaseId, TimeSpan.FromMinutes(1));

        await grain.Received(1).RenewLeaseAsync(playerId, leaseId, TimeSpan.FromMinutes(1));
    }
}

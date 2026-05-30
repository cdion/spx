using System.Collections.Immutable;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Orleans;
using Spx.Contracts;
using Spx.Game.Application;
using Spx.Nexus.Domain;
using Spx.Web.Adapters;
using Xunit;

namespace Spx.Web.Tests;

public sealed class OrleansNexusRuntimeClientTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task EnsureSessionAsync_returns_false_when_player_count_is_not_two(int playerCount)
    {
        var clusterClient = Substitute.For<IClusterClient>();
        var client = new OrleansNexusRuntimeClient(
            clusterClient,
            NullLogger<OrleansNexusRuntimeClient>.Instance
        );

        var result = await client.EnsureSessionAsync(Guid.NewGuid(), CreatePlayerIds(playerCount));

        Assert.False(result);
        clusterClient.DidNotReceive().GetGrain<INexusSessionGrain>(Arg.Any<Guid>());
    }

    [Fact]
    public async Task EnsureSessionAsync_initializes_session_when_player_count_is_two()
    {
        var gameId = Guid.NewGuid();
        var playerIds = CreatePlayerIds(2);
        var clusterClient = Substitute.For<IClusterClient>();
        var sessionGrain = Substitute.For<INexusSessionGrain>();
        clusterClient.GetGrain<INexusSessionGrain>(gameId).Returns(sessionGrain);
        var client = new OrleansNexusRuntimeClient(
            clusterClient,
            NullLogger<OrleansNexusRuntimeClient>.Instance
        );

        var result = await client.EnsureSessionAsync(gameId, playerIds);

        Assert.True(result);
        await sessionGrain
            .Received(1)
            .InitializeAsync(
                Arg.Is<InitializeNexusGameCommand>(command =>
                    command.Players.Select(player => player.PlayerId).SequenceEqual(playerIds)
                )
            );
    }

    [Fact]
    public async Task EnsureSessionAsync_returns_false_when_grain_throws_unexpected_exception()
    {
        var gameId = Guid.NewGuid();
        var clusterClient = Substitute.For<IClusterClient>();
        var sessionGrain = Substitute.For<INexusSessionGrain>();
        clusterClient.GetGrain<INexusSessionGrain>(gameId).Returns(sessionGrain);
        sessionGrain
            .InitializeAsync(Arg.Any<InitializeNexusGameCommand>())
            .Returns(Task.FromException(new InvalidOperationException("boom")));
        var client = new OrleansNexusRuntimeClient(
            clusterClient,
            NullLogger<OrleansNexusRuntimeClient>.Instance
        );

        var result = await client.EnsureSessionAsync(gameId, CreatePlayerIds(2));

        Assert.False(result);
    }

    [Fact]
    public async Task GetSessionAsync_returns_unavailable_when_grain_throws_unexpected_exception()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var clusterClient = Substitute.For<IClusterClient>();
        var sessionGrain = Substitute.For<INexusSessionGrain>();
        clusterClient.GetGrain<INexusSessionGrain>(gameId).Returns(sessionGrain);
        sessionGrain
            .GetViewAsync(playerId)
            .Returns(Task.FromException<NexusGameView?>(new InvalidOperationException("boom")));
        var client = new OrleansNexusRuntimeClient(
            clusterClient,
            NullLogger<OrleansNexusRuntimeClient>.Instance
        );

        var result = await client.GetSessionAsync(gameId, playerId);

        Assert.IsType<GameSessionUnavailable>(result);
    }

    [Fact]
    public async Task SubmitOrdersAsync_throws_safe_exception_when_grain_throws_unexpected_exception()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var clusterClient = Substitute.For<IClusterClient>();
        var sessionGrain = Substitute.For<INexusSessionGrain>();
        clusterClient.GetGrain<INexusSessionGrain>(gameId).Returns(sessionGrain);
        var command = new NexusTurnOrdersCommand(
            playerId,
            1,
            ImmutableArray<NexusMoveOrder>.Empty,
            ImmutableArray<NexusBuildOrder>.Empty,
            false
        );
        sessionGrain
            .SubmitOrdersAsync(command)
            .Returns(
                Task.FromException<NexusTurnOrdersResult>(new InvalidOperationException("boom"))
            );
        var client = new OrleansNexusRuntimeClient(
            clusterClient,
            NullLogger<OrleansNexusRuntimeClient>.Instance
        );

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.SubmitOrdersAsync(gameId, command)
        );

        Assert.Equal(
            "The game session could not process those orders. Please try again.",
            exception.Message
        );
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public async Task AbandonAsync_throws_safe_exception_when_grain_throws_unexpected_exception()
    {
        var gameId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var clusterClient = Substitute.For<IClusterClient>();
        var sessionGrain = Substitute.For<INexusSessionGrain>();
        clusterClient.GetGrain<INexusSessionGrain>(gameId).Returns(sessionGrain);
        sessionGrain
            .AbandonAsync(playerId)
            .Returns(Task.FromException(new InvalidOperationException("boom")));
        var client = new OrleansNexusRuntimeClient(
            clusterClient,
            NullLogger<OrleansNexusRuntimeClient>.Instance
        );

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.AbandonAsync(gameId, playerId)
        );

        Assert.Equal("The game session could not be abandoned.", exception.Message);
        Assert.Null(exception.InnerException);
    }

    private static Guid[] CreatePlayerIds(int count) =>
        Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToArray();
}

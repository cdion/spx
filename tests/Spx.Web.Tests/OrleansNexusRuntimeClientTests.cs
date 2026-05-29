using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Orleans;
using Spx.Contracts;
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

    private static Guid[] CreatePlayerIds(int count) =>
        Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToArray();
}

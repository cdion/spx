using System.Security.Claims;
using NSubstitute;
using Spx.Game.Application;
using Spx.Web.Hubs;
using Xunit;

namespace Spx.Web.Tests;

public sealed class NexusHubAccessServiceTests
{
    [Fact]
    public async Task GetAccessAsync_returns_null_when_user_has_no_identity_id()
    {
        var gamePersistence = Substitute.For<IGamePersistence>();
        var service = new NexusHubAccessService(gamePersistence);

        var result = await service.GetAccessAsync(Guid.NewGuid(), new ClaimsPrincipal());

        Assert.Null(result);
        await gamePersistence
            .DidNotReceive()
            .GetLobbyAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAccessAsync_returns_null_when_user_cannot_access_game()
    {
        var gameId = Guid.NewGuid();
        const string userId = "user-1";
        var gamePersistence = Substitute.For<IGamePersistence>();
        gamePersistence
            .GetLobbyAsync(gameId, userId, Arg.Any<CancellationToken>())
            .Returns((GameLobbyView?)null);
        var service = new NexusHubAccessService(gamePersistence);

        var result = await service.GetAccessAsync(gameId, CreateUser(userId));

        Assert.Null(result);
        await gamePersistence
            .Received(1)
            .GetLobbyAsync(gameId, userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAccessAsync_returns_active_player_access_for_active_member()
    {
        var gameId = Guid.NewGuid();
        const string userId = "user-1";
        var playerId = Guid.NewGuid();
        var gamePersistence = Substitute.For<IGamePersistence>();
        gamePersistence
            .GetLobbyAsync(gameId, userId, Arg.Any<CancellationToken>())
            .Returns(CreateLobby(gameId, playerId, isCurrentUserActive: true));
        var service = new NexusHubAccessService(gamePersistence);

        var result = await service.GetAccessAsync(gameId, CreateUser(userId));

        Assert.Equal(new NexusHubAccess(playerId, true), result);
    }

    [Fact]
    public async Task GetAccessAsync_returns_read_only_access_for_former_player()
    {
        var gameId = Guid.NewGuid();
        const string userId = "user-1";
        var playerId = Guid.NewGuid();
        var gamePersistence = Substitute.For<IGamePersistence>();
        gamePersistence
            .GetLobbyAsync(gameId, userId, Arg.Any<CancellationToken>())
            .Returns(CreateLobby(gameId, playerId, isCurrentUserActive: false));
        var service = new NexusHubAccessService(gamePersistence);

        var result = await service.GetAccessAsync(gameId, CreateUser(userId));

        Assert.Equal(new NexusHubAccess(playerId, false), result);
    }

    private static ClaimsPrincipal CreateUser(string userId) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "test"));

    private static GameLobbyView CreateLobby(
        Guid gameId,
        Guid playerId,
        bool isCurrentUserActive
    ) =>
        new(
            gameId,
            "Arena",
            "ABC123",
            GameStatus.Open,
            2,
            DateTime.UtcNow,
            null,
            "Captain Red",
            playerId,
            [new GamePlayerView(playerId, "Captain Red", DateTime.UtcNow)],
            isCurrentUserActive
        );
}

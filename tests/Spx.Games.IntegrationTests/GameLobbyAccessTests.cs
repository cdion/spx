using Spx.Data;
using Spx.Games;
using Xunit;

namespace Spx.Games.IntegrationTests;

public sealed class GameLobbyAccessTests
{
    [Fact]
    public async Task GetLobbyAsync_ReturnsLobbyForActivePlayer()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddUserAsync("user-1", "user1@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", "user-1", "Captain Red");
        var features = GameFeatureTestFactory.Create(database.Context);

        var lobby = await features.GetLobby.HandleAsync(game.Id, "user-1");

        Assert.NotNull(lobby);
        Assert.True(lobby!.IsCurrentUserActive);
        Assert.Equal("Captain Red", lobby.CurrentPlayerName);
        Assert.Single(lobby.Players);
        Assert.True(lobby.Players[0].IsCurrentUser);
    }

    [Fact]
    public async Task GetLobbyAsync_ReturnsLobbyForFormerPlayer()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddUserAsync("user-1", "user1@example.com");
        await database.AddUserAsync("user-2", "user2@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", "user-1", "Captain Red");
        var formerPlayer = await database.AddGamePlayerAsync(game.Id, "user-2", "Captain Blue", leftAtUtc: DateTime.UtcNow.AddMinutes(-2));
        formerPlayer.VisibleThroughMessageId = Guid.Parse("019e0000-0000-7000-8000-000000000002");
        await database.Context.SaveChangesAsync();

        var features = GameFeatureTestFactory.Create(database.Context);

        var lobby = await features.GetLobby.HandleAsync(game.Id, "user-2");

        Assert.NotNull(lobby);
        Assert.False(lobby!.IsCurrentUserActive);
        Assert.Equal("Captain Blue", lobby.CurrentPlayerName);
    }

    [Fact]
    public async Task GetLobbyAsync_ReturnsNullForNonParticipant()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddUserAsync("user-1", "user1@example.com");
        await database.AddUserAsync("user-2", "user2@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", "user-1", "Captain Red");
        var features = GameFeatureTestFactory.Create(database.Context);

        var lobby = await features.GetLobby.HandleAsync(game.Id, "user-2");

        Assert.Null(lobby);
    }
}
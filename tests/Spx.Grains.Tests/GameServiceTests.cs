using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Spx.Data;
using Spx.Games;
using Xunit;

namespace Spx.Grains.Tests;

public sealed class GameServiceTests
{
    [Fact]
    public async Task CreateGameAsync_CreatesGameAndHostPlayer()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddUserAsync("user-1", "user1@example.com");
        var notifier = new FakeGameLobbyNotifier();
        var messagePublisher = new FakeGameMessagePublisher();
        var service = CreateService(database.Context, notifier, messagePublisher);

        var result = await service.CreateGameAsync("user-1", new CreateGameRequest("Weekend match", "Captain Red"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.GameId);

        var game = await database.Context.Games.SingleAsync();
        var player = await database.Context.GamePlayers.SingleAsync();
        var message = await database.Context.GameMessages.SingleAsync();

        Assert.Equal("Weekend match", game.Name);
        Assert.Equal(6, game.InviteCode.Length);
        Assert.Matches("^[A-Z0-9]{6}$", game.InviteCode);
        Assert.Equal(GameStatus.Open, game.Status);
        Assert.Equal("Captain Red", player.Name);
        Assert.Null(player.LeftAtUtc);
        Assert.Equal([game.Id], notifier.PublishedGameIds);
        Assert.Equal([game.Id], messagePublisher.PublishedGameIds);
        Assert.Equal(GameMessageKind.GameCreated, message.Kind);
        Assert.Equal("Captain Red", message.SenderDisplayName);
    }

    [Fact]
    public async Task JoinGameAsync_ReattachesAndRenamesExistingActivePlayer()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddUserAsync("user-1", "user1@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", activePlayerUserId: "user-1", activePlayerName: "Captain Red");
        var notifier = new FakeGameLobbyNotifier();
        var messagePublisher = new FakeGameMessagePublisher();
        var service = CreateService(database.Context, notifier, messagePublisher);

        var result = await service.JoinGameAsync("user-1", new JoinGameRequest(game.InviteCode, "Captain Blue"));

        Assert.True(result.Succeeded);

        var players = await database.Context.GamePlayers
            .Where(entry => entry.GameId == game.Id && entry.LeftAtUtc == null)
            .ToListAsync();

        Assert.Single(players);
        Assert.Equal("Captain Blue", players[0].Name);
        Assert.Equal([game.Id], notifier.PublishedGameIds);
        Assert.Empty(messagePublisher.PublishedGameIds);
        Assert.Empty(await database.Context.GameMessages.ToListAsync());
    }

    [Fact]
    public async Task LeaveGameAsync_LastPlayerEndsGameAndSoftDeletesMembership()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddUserAsync("user-1", "user1@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", activePlayerUserId: "user-1", activePlayerName: "Captain Red");
        var notifier = new FakeGameLobbyNotifier();
        var messagePublisher = new FakeGameMessagePublisher();
        var service = CreateService(database.Context, notifier, messagePublisher);

        var result = await service.LeaveGameAsync(game.Id, "user-1");

        Assert.True(result.Succeeded);

        var updatedGame = await database.Context.Games.SingleAsync();
        var player = await database.Context.GamePlayers.SingleAsync();
        var messages = await database.Context.GameMessages.OrderBy(entry => entry.Id).ToListAsync();

        Assert.Equal(GameStatus.Ended, updatedGame.Status);
        Assert.NotNull(updatedGame.EndedAtUtc);
        Assert.NotNull(player.LeftAtUtc);
        Assert.Equal([game.Id], notifier.PublishedGameIds);
        Assert.Equal([game.Id], messagePublisher.PublishedGameIds);
        Assert.Equal([GameMessageKind.PlayerLeft, GameMessageKind.GameEnded], messages.Select(entry => entry.Kind));
        Assert.Equal(messages[^1].Id, player.VisibleThroughMessageId);
    }

    [Fact]
    public async Task GetUserGamesAsync_SplitsOpenAndEndedGames()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddUserAsync("user-1", "user1@example.com");
        var openGame = await database.AddGameAsync("user-1", "OPEN01", "Open Game", activePlayerUserId: "user-1", activePlayerName: "Captain Red");
        var endedGame = await database.AddGameAsync("user-1", "DONE01", "Ended Game", activePlayerUserId: "user-1", activePlayerName: "Captain Red", status: GameStatus.Ended, endedAtUtc: DateTime.UtcNow.AddMinutes(-5), leftAtUtc: DateTime.UtcNow.AddMinutes(-4));
        var service = CreateService(database.Context, new FakeGameLobbyNotifier(), new FakeGameMessagePublisher());

        var games = await service.GetUserGamesAsync("user-1");

        Assert.Collection(games.OpenGames, entry => Assert.Equal(openGame.Id, entry.GameId));
        Assert.Collection(games.EndedGames, entry => Assert.Equal(endedGame.Id, entry.GameId));
    }

    private static GameService CreateService(ApplicationDbContext dbContext, IGameLobbyEventsPublisher notifier, IGameMessageEventsPublisher messagePublisher)
        => new(dbContext, notifier, messagePublisher, NullLogger<GameService>.Instance);
}
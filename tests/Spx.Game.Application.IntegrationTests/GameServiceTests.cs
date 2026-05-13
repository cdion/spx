using Microsoft.EntityFrameworkCore;
using Spx.Data;
using Spx.Game.Application;
using Xunit;

namespace Spx.Game.Application.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class GameServiceTests(PostgresDatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task CreateGameAsync_CreatesGameAndHostPlayer()
    {
        var database = Database;
        await database.AddUserAsync("user-1", "user1@example.com");
        var notifier = new FakeGameLobbyNotifier();
        var messagePublisher = new FakeGameMessagePublisher();
        var features = GameFeatureTestFactory.Create(database.ContextFactory, notifier, messagePublisher);

        var result = await features.CreateGame.HandleAsync("user-1", new CreateGameRequest("Weekend match", "Captain Red"));

        var succeeded = Assert.IsType<GameCommandSucceeded>(result);

        var game = await database.Context.Games.SingleAsync();
        var player = await database.Context.GamePlayers.SingleAsync();
        var message = await database.Context.GameMessages.SingleAsync();

        Assert.Equal("Weekend match", game.Name);
        Assert.Equal(6, game.InviteCode.Length);
        Assert.Matches("^[A-Z0-9]{6}$", game.InviteCode);
        Assert.Equal(GameStatus.Open, game.Status);
        Assert.Equal(game.Id, succeeded.GameId);
        Assert.Equal("Captain Red", player.Name);
        Assert.Null(player.LeftAtUtc);
        Assert.Equal(GameMessageKind.GameCreated, message.Kind);
        Assert.Equal("Captain Red", message.SenderDisplayName);
    }

    [Fact]
    public async Task JoinGameAsync_ReattachesAndRenamesExistingActivePlayer()
    {
        var database = Database;
        await database.AddUserAsync("user-1", "user1@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", activePlayerUserId: "user-1", activePlayerName: "Captain Red");
        var notifier = new FakeGameLobbyNotifier();
        var messagePublisher = new FakeGameMessagePublisher();
        var features = GameFeatureTestFactory.Create(database.ContextFactory, notifier, messagePublisher);

        var result = await features.JoinGame.HandleAsync("user-1", new JoinGameRequest(game.InviteCode, "Captain Blue"));

        Assert.IsType<GameCommandSucceeded>(result);

        var players = await database.Context.GamePlayers
            .Where(entry => entry.GameId == game.Id && entry.LeftAtUtc == null)
            .ToListAsync();

        Assert.Single(players);
        Assert.Equal("Captain Blue", players[0].Name);
        Assert.Empty(await database.Context.GameMessages.ToListAsync());
    }

    [Fact]
    public async Task JoinGameAsync_RejectsFullGame()
    {
        var database = Database;
        await database.AddUserAsync("user-1", "user1@example.com");
        await database.AddUserAsync("user-2", "user2@example.com");
        await database.AddUserAsync("user-3", "user3@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", activePlayerUserId: "user-1", activePlayerName: "Captain Red");
        await database.AddGamePlayerAsync(game.Id, "user-2", "Captain Blue");
        var notifier = new FakeGameLobbyNotifier();
        var messagePublisher = new FakeGameMessagePublisher();
        var features = GameFeatureTestFactory.Create(database.ContextFactory, notifier, messagePublisher);

        var result = await features.JoinGame.HandleAsync("user-3", new JoinGameRequest(game.InviteCode, "Captain Green"));

        var failed = Assert.IsType<GameCommandFailed>(result);
        Assert.Equal("That game is already full.", failed.ErrorMessage);
        Assert.Empty(notifier.PublishedGameIds);
        Assert.Empty(messagePublisher.PublishedGameIds);
    }

    [Fact]
    public async Task JoinGameAsync_RejectsDuplicatePlayerName()
    {
        var database = Database;
        await database.AddUserAsync("user-1", "user1@example.com");
        await database.AddUserAsync("user-3", "user3@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", activePlayerUserId: "user-1", activePlayerName: "Captain Red");
        var notifier = new FakeGameLobbyNotifier();
        var messagePublisher = new FakeGameMessagePublisher();
        var features = GameFeatureTestFactory.Create(database.ContextFactory, notifier, messagePublisher);

        var result = await features.JoinGame.HandleAsync("user-3", new JoinGameRequest(game.InviteCode, "Captain Red"));

        var failed = Assert.IsType<GameCommandFailed>(result);
        Assert.Equal("That player name is already taken in this game.", failed.ErrorMessage);
        Assert.Empty(notifier.PublishedGameIds);
        Assert.Empty(messagePublisher.PublishedGameIds);
    }

    [Fact]
    public async Task JoinGameAsync_KeepsJoinWhenSessionInitializationFails()
    {
        var database = Database;
        await database.AddUserAsync("user-1", "user1@example.com");
        await database.AddUserAsync("user-2", "user2@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", activePlayerUserId: "user-1", activePlayerName: "Captain Red");
        var notifier = new FakeGameLobbyNotifier();
        var messagePublisher = new FakeGameMessagePublisher();
        var sessionService = new FakeGameSessionService
        {
            TryInitializeResult = false
        };
        var features = GameFeatureTestFactory.Create(database.ContextFactory, notifier, messagePublisher, sessionService);

        var result = await features.JoinGame.HandleAsync("user-2", new JoinGameRequest(game.InviteCode, "Captain Blue"));

        Assert.IsType<GameCommandSucceeded>(result);
        Assert.Equal([game.Id], notifier.PublishedGameIds);
        Assert.Equal([game.Id], messagePublisher.PublishedGameIds);

        var players = await database.Context.GamePlayers
            .Where(entry => entry.GameId == game.Id && entry.LeftAtUtc == null)
            .OrderBy(entry => entry.JoinedAtUtc)
            .ToListAsync();
        var messages = await database.Context.GameMessages
            .Where(entry => entry.GameId == game.Id)
            .ToListAsync();

        Assert.Equal(2, players.Count);
        Assert.Equal(["user-1", "user-2"], players.Select(entry => entry.UserId));
        Assert.Single(messages);
        Assert.Equal(GameMessageKind.PlayerJoined, messages[0].Kind);
    }

    [Fact]
    public async Task LeaveGameAsync_LastPlayerEndsGameAndSoftDeletesMembership()
    {
        var database = Database;
        await database.AddUserAsync("user-1", "user1@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", activePlayerUserId: "user-1", activePlayerName: "Captain Red");
        var notifier = new FakeGameLobbyNotifier();
        var messagePublisher = new FakeGameMessagePublisher();
        var features = GameFeatureTestFactory.Create(database.ContextFactory, notifier, messagePublisher);

        var result = await features.LeaveGame.HandleAsync(game.Id, "user-1");

        Assert.IsType<GameCommandSucceeded>(result);

        var updatedGame = await database.Context.Games.SingleAsync();
        var player = await database.Context.GamePlayers.SingleAsync();
        var messages = await database.Context.GameMessages.OrderBy(entry => entry.Id).ToListAsync();

        Assert.Equal(GameStatus.Ended, updatedGame.Status);
        Assert.NotNull(updatedGame.EndedAtUtc);
        Assert.NotNull(player.LeftAtUtc);
        Assert.Equal([GameMessageKind.PlayerLeft, GameMessageKind.GameEnded], messages.Select(entry => entry.Kind));
        Assert.Equal(messages[^1].Id, player.VisibleThroughMessageId);
    }

    [Fact]
    public async Task LeaveGameAsync_WhenAnotherPlayerRemains_KeepsGameOpen()
    {
        var database = Database;
        await database.AddUserAsync("user-1", "user1@example.com");
        await database.AddUserAsync("user-2", "user2@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", activePlayerUserId: "user-1", activePlayerName: "Captain Red");
        await database.AddGamePlayerAsync(game.Id, "user-2", "Captain Blue");
        var notifier = new FakeGameLobbyNotifier();
        var messagePublisher = new FakeGameMessagePublisher();
        var features = GameFeatureTestFactory.Create(database.ContextFactory, notifier, messagePublisher);

        var result = await features.LeaveGame.HandleAsync(game.Id, "user-1");

        Assert.IsType<GameCommandSucceeded>(result);

        var updatedGame = await database.Context.Games.SingleAsync();
        var departingPlayer = await database.Context.GamePlayers.SingleAsync(entry => entry.GameId == game.Id && entry.UserId == "user-1");
        var messages = await database.Context.GameMessages.OrderBy(entry => entry.Id).ToListAsync();

        Assert.Equal(GameStatus.Open, updatedGame.Status);
        Assert.Null(updatedGame.EndedAtUtc);
        Assert.NotNull(departingPlayer.LeftAtUtc);
        Assert.Single(messages);
        Assert.Equal(GameMessageKind.PlayerLeft, messages[0].Kind);
        Assert.Equal(messages[0].Id, departingPlayer.VisibleThroughMessageId);
    }

    [Fact]
    public async Task GetUserGamesAsync_SplitsOpenAndEndedGames()
    {
        var database = Database;
        await database.AddUserAsync("user-1", "user1@example.com");
        var openGame = await database.AddGameAsync("user-1", "OPEN01", "Open Game", activePlayerUserId: "user-1", activePlayerName: "Captain Red");
        var endedGame = await database.AddGameAsync("user-1", "DONE01", "Ended Game", activePlayerUserId: "user-1", activePlayerName: "Captain Red", status: GameStatus.Ended, endedAtUtc: DateTime.UtcNow.AddMinutes(-5), leftAtUtc: DateTime.UtcNow.AddMinutes(-4));
        var features = GameFeatureTestFactory.Create(database.ContextFactory, new FakeGameLobbyNotifier(), new FakeGameMessagePublisher());

        var games = await features.GetUserGames.HandleAsync("user-1");

        Assert.Collection(games.OpenGames, entry => Assert.Equal(openGame.Id, entry.GameId));
        Assert.Collection(games.EndedGames, entry => Assert.Equal(endedGame.Id, entry.GameId));
    }
}
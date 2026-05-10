using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Spx.Web.Data;
using Spx.Web.Services;
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
        var service = CreateService(database.Context, notifier);

        var result = await service.CreateGameAsync("user-1", new CreateGameRequest("Weekend match", "Captain Red"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.GameId);

        var game = await database.Context.Games.SingleAsync();
        var player = await database.Context.GamePlayers.SingleAsync();

        Assert.Equal("Weekend match", game.Name);
        Assert.Equal(6, game.InviteCode.Length);
        Assert.Matches("^[A-Z0-9]{6}$", game.InviteCode);
        Assert.Equal(GameStatus.Open, game.Status);
        Assert.Equal("Captain Red", player.Name);
        Assert.Null(player.LeftAtUtc);
        Assert.Equal([game.Id], notifier.PublishedGameIds);
    }

    [Fact]
    public async Task JoinGameAsync_ReattachesAndRenamesExistingActivePlayer()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddUserAsync("user-1", "user1@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", activePlayerUserId: "user-1", activePlayerName: "Captain Red");
        var notifier = new FakeGameLobbyNotifier();
        var service = CreateService(database.Context, notifier);

        var result = await service.JoinGameAsync("user-1", new JoinGameRequest(game.InviteCode, "Captain Blue"));

        Assert.True(result.Succeeded);

        var players = await database.Context.GamePlayers
            .Where(entry => entry.GameId == game.Id && entry.LeftAtUtc == null)
            .ToListAsync();

        Assert.Single(players);
        Assert.Equal("Captain Blue", players[0].Name);
        Assert.Equal([game.Id], notifier.PublishedGameIds);
    }

    [Fact]
    public async Task LeaveGameAsync_LastPlayerEndsGameAndSoftDeletesMembership()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddUserAsync("user-1", "user1@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", activePlayerUserId: "user-1", activePlayerName: "Captain Red");
        var notifier = new FakeGameLobbyNotifier();
        var service = CreateService(database.Context, notifier);

        var result = await service.LeaveGameAsync(game.Id, "user-1");

        Assert.True(result.Succeeded);

        var updatedGame = await database.Context.Games.SingleAsync();
        var player = await database.Context.GamePlayers.SingleAsync();

        Assert.Equal(GameStatus.Ended, updatedGame.Status);
        Assert.NotNull(updatedGame.EndedAtUtc);
        Assert.NotNull(player.LeftAtUtc);
        Assert.Equal([game.Id], notifier.PublishedGameIds);
    }

    [Fact]
    public async Task GetUserGamesAsync_SplitsOpenAndEndedGames()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddUserAsync("user-1", "user1@example.com");
        var openGame = await database.AddGameAsync("user-1", "OPEN01", "Open Game", activePlayerUserId: "user-1", activePlayerName: "Captain Red");
        var endedGame = await database.AddGameAsync("user-1", "DONE01", "Ended Game", activePlayerUserId: "user-1", activePlayerName: "Captain Red", status: GameStatus.Ended, endedAtUtc: DateTime.UtcNow.AddMinutes(-5), leftAtUtc: DateTime.UtcNow.AddMinutes(-4));
        var service = CreateService(database.Context, new FakeGameLobbyNotifier());

        var games = await service.GetUserGamesAsync("user-1");

        Assert.Collection(games.OpenGames, entry => Assert.Equal(openGame.Id, entry.GameId));
        Assert.Collection(games.EndedGames, entry => Assert.Equal(endedGame.Id, entry.GameId));
    }

    private static GameService CreateService(ApplicationDbContext dbContext, IGameLobbyNotifier notifier)
        => new(dbContext, notifier, NullLogger<GameService>.Instance);

    private sealed class FakeGameLobbyNotifier : IGameLobbyNotifier
    {
        public List<Guid> PublishedGameIds { get; } = [];

        public Task PublishLobbyChangedAsync(Guid gameId, CancellationToken cancellationToken = default)
        {
            PublishedGameIds.Add(gameId);
            return Task.CompletedTask;
        }

        public ValueTask<IAsyncDisposable> SubscribeAsync(Guid gameId, Func<Task> onLobbyChanged, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IAsyncDisposable>(new NoOpAsyncDisposable());
    }

    private sealed class NoOpAsyncDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private TestDatabase(SqliteConnection connection, ApplicationDbContext context)
        {
            this.connection = connection;
            Context = context;
        }

        public ApplicationDbContext Context { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();

            return new TestDatabase(connection, context);
        }

        public async Task AddUserAsync(string userId, string email)
        {
            Context.Users.Add(new ApplicationUser
            {
                Id = userId,
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant()
            });

            await Context.SaveChangesAsync();
        }

        public async Task<Game> AddGameAsync(
            string createdByUserId,
            string inviteCode,
            string name,
            string activePlayerUserId,
            string activePlayerName,
            GameStatus status = GameStatus.Open,
            DateTime? endedAtUtc = null,
            DateTime? leftAtUtc = null)
        {
            var now = DateTime.UtcNow.AddMinutes(-10);
            var game = new Game
            {
                Id = Guid.NewGuid(),
                Name = name,
                InviteCode = inviteCode,
                CreatedAtUtc = now,
                CreatedByUserId = createdByUserId,
                MaxPlayers = 2,
                Status = status,
                EndedAtUtc = endedAtUtc
            };

            Context.Games.Add(game);
            Context.GamePlayers.Add(new GamePlayer
            {
                Id = Guid.NewGuid(),
                GameId = game.Id,
                UserId = activePlayerUserId,
                Name = activePlayerName,
                NormalizedName = activePlayerName.ToUpperInvariant(),
                JoinedAtUtc = now.AddMinutes(1),
                LeftAtUtc = leftAtUtc
            });

            await Context.SaveChangesAsync();
            return game;
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
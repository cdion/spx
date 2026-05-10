using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Spx.Data;
using Spx.Games;

namespace Spx.Grains.Tests;

internal sealed class TestDatabase : IAsyncDisposable
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
        DateTime? leftAtUtc = null,
        Guid? visibleThroughMessageId = null)
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
            LeftAtUtc = leftAtUtc,
            VisibleThroughMessageId = visibleThroughMessageId
        });

        await Context.SaveChangesAsync();
        return game;
    }

    public async Task<GamePlayer> AddGamePlayerAsync(
        Guid gameId,
        string userId,
        string playerName,
        DateTime? joinedAtUtc = null,
        DateTime? leftAtUtc = null,
        Guid? visibleThroughMessageId = null)
    {
        var player = new GamePlayer
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            UserId = userId,
            Name = playerName,
            NormalizedName = playerName.ToUpperInvariant(),
            JoinedAtUtc = joinedAtUtc ?? DateTime.UtcNow.AddMinutes(-5),
            LeftAtUtc = leftAtUtc,
            VisibleThroughMessageId = visibleThroughMessageId
        };

        Context.GamePlayers.Add(player);
        await Context.SaveChangesAsync();
        return player;
    }

    public async Task<GameMessage> AddMessageAsync(
        Guid gameId,
        GameMessageKind kind,
        GameMessageSenderKind senderKind,
        Guid? senderPlayerId,
        Guid? recipientPlayerId,
        string senderDisplayName,
        string recipientDisplayName,
        string body,
        DateTime? createdAtUtc = null,
        DateTime? editedAtUtc = null,
        DateTime? deletedAtUtc = null,
        Guid? id = null)
    {
        var message = new GameMessage
        {
            Id = id ?? Guid.CreateVersion7(),
            GameId = gameId,
            Kind = kind,
            SenderKind = senderKind,
            SenderPlayerId = senderPlayerId,
            RecipientPlayerId = recipientPlayerId,
            SenderDisplayName = senderDisplayName,
            RecipientDisplayName = recipientDisplayName,
            Body = body,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow,
            EditedAtUtc = editedAtUtc,
            DeletedAtUtc = deletedAtUtc
        };

        Context.GameMessages.Add(message);
        await Context.SaveChangesAsync();
        return message;
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await connection.DisposeAsync();
    }
}

internal sealed class FakeGameLobbyNotifier : IGameLobbyEventsPublisher
{
    public List<Guid> PublishedGameIds { get; } = [];

    public Task PublishLobbyChangedAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        PublishedGameIds.Add(gameId);
        return Task.CompletedTask;
    }
}

internal sealed class FakeGameMessagePublisher : IGameMessageEventsPublisher
{
    public List<Guid> PublishedGameIds { get; } = [];

    public Task PublishMessagesChangedAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        PublishedGameIds.Add(gameId);
        return Task.CompletedTask;
    }
}
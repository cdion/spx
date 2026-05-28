using Microsoft.EntityFrameworkCore;
using Spx.Data;
using Spx.Game.Application;

namespace Spx.Game.Application.IntegrationTests;

public sealed class TestDatabase : IAsyncDisposable
{
    private readonly List<ApplicationDbContext> leasedContexts = [];
    private readonly TestDbContextFactory contextFactory;

    internal TestDatabase(string connectionString)
    {
        contextFactory = new TestDbContextFactory(connectionString);
    }

    public IDbContextFactory<ApplicationDbContext> ContextFactory => contextFactory;

    public ApplicationDbContext Context => LeaseContext();

    public ApplicationDbContext CreateDbContext() => contextFactory.CreateDbContext();

    private ApplicationDbContext LeaseContext()
    {
        var context = contextFactory.CreateDbContext();
        leasedContexts.Add(context);
        return context;
    }

    public async Task AddUserAsync(string userId, string email)
    {
        await using var context = CreateDbContext();

        context.Users.Add(
            new ApplicationUser
            {
                Id = userId,
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
            }
        );

        await context.SaveChangesAsync();
    }

    public async Task<Spx.Data.Game> AddGameAsync(
        string createdByUserId,
        string inviteCode,
        string name,
        string activePlayerUserId,
        string activePlayerName,
        GameStatus status = GameStatus.Open,
        DateTime? endedAtUtc = null,
        DateTime? leftAtUtc = null,
        Guid? visibleThroughMessageId = null
    )
    {
        await using var context = CreateDbContext();

        var now = DateTime.UtcNow.AddMinutes(-10);
        var game = new Spx.Data.Game
        {
            Id = Guid.NewGuid(),
            Name = name,
            InviteCode = inviteCode,
            CreatedAtUtc = now,
            CreatedByUserId = createdByUserId,
            MaxPlayers = 2,
            Status = status,
            EndedAtUtc = endedAtUtc,
        };

        context.Games.Add(game);
        context.GamePlayers.Add(
            new GamePlayer
            {
                Id = Guid.NewGuid(),
                GameId = game.Id,
                UserId = activePlayerUserId,
                Name = activePlayerName,
                NormalizedName = activePlayerName.ToUpperInvariant(),
                JoinedAtUtc = now.AddMinutes(1),
                LeftAtUtc = leftAtUtc,
                VisibleThroughMessageId = visibleThroughMessageId,
            }
        );

        await context.SaveChangesAsync();
        return game;
    }

    public async Task<GamePlayer> AddGamePlayerAsync(
        Guid gameId,
        string userId,
        string playerName,
        DateTime? joinedAtUtc = null,
        DateTime? leftAtUtc = null,
        Guid? visibleThroughMessageId = null
    )
    {
        await using var context = CreateDbContext();

        var player = new GamePlayer
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            UserId = userId,
            Name = playerName,
            NormalizedName = playerName.ToUpperInvariant(),
            JoinedAtUtc = joinedAtUtc ?? DateTime.UtcNow.AddMinutes(-5),
            LeftAtUtc = leftAtUtc,
            VisibleThroughMessageId = visibleThroughMessageId,
        };

        context.GamePlayers.Add(player);
        await context.SaveChangesAsync();
        return player;
    }

    public async Task SetVisibleThroughMessageIdAsync(Guid playerId, Guid? visibleThroughMessageId)
    {
        await using var context = CreateDbContext();
        var player = await context.GamePlayers.SingleAsync(entry => entry.Id == playerId);
        player.VisibleThroughMessageId = visibleThroughMessageId;
        await context.SaveChangesAsync();
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
        Guid? id = null
    )
    {
        await using var context = CreateDbContext();

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
            DeletedAtUtc = deletedAtUtc,
        };

        context.GameMessages.Add(message);
        await context.SaveChangesAsync();
        return message;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var context in leasedContexts)
        {
            await context.DisposeAsync();
        }
    }
}

internal sealed class FakeGameLobbyNotifier : ILobbyInvalidationPublisher
{
    public List<Guid> PublishedGameIds { get; } = [];

    public Task PublishLobbyInvalidatedAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        PublishedGameIds.Add(gameId);
        return Task.CompletedTask;
    }
}

internal sealed class FakeGameMessagePublisher : IGameMessageInvalidationPublisher
{
    public List<Guid> PublishedGameIds { get; } = [];

    public Task PublishMessagesInvalidatedAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        PublishedGameIds.Add(gameId);
        return Task.CompletedTask;
    }
}

internal sealed class TestDbContextFactory(string connectionString)
    : IDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    public async ValueTask<ApplicationDbContext> CreateDbContextAsync(
        CancellationToken cancellationToken = default
    )
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await Task.CompletedTask;
        return new ApplicationDbContext(options);
    }
}

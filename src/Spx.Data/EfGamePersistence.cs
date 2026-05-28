using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Spx.Nexus.Application;
using Spx.Nexus.Domain;

namespace Spx.Data;

internal sealed partial class EfGamePersistence(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    ILogger<EfGamePersistence> logger
) : IGamePersistence
{
    private const int MaxPlayersPerGame = 2;

    public async Task<Guid?> TryCreateGameAsync(
        CreateGamePersistenceRequest request,
        CancellationToken cancellationToken
    )
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var executionStrategy = dbContext.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(
            async innerCancellationToken =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    innerCancellationToken
                );

                var now = DateTime.UtcNow;
                var game = new Game
                {
                    Id = Guid.NewGuid(),
                    Name = request.GameName,
                    InviteCode = request.InviteCode,
                    CreatedAtUtc = now,
                    CreatedByUserId = request.UserId,
                    MaxPlayers = MaxPlayersPerGame,
                    Status = GameStatus.Open,
                };

                var player = new GamePlayer
                {
                    Id = Guid.NewGuid(),
                    GameId = game.Id,
                    UserId = request.UserId,
                    Name = request.PlayerName,
                    NormalizedName = request.PlayerNameLookup,
                    JoinedAtUtc = now,
                };

                var createdMessage = GameMessageFactory.CreateSystemEvent(
                    game.Id,
                    GameMessageKind.GameCreated,
                    now,
                    player
                );

                dbContext.Games.Add(game);
                dbContext.GamePlayers.Add(player);
                dbContext.GameMessages.Add(createdMessage);

                try
                {
                    await dbContext.SaveChangesAsync(innerCancellationToken);
                    await transaction.CommitAsync(innerCancellationToken);
                    return game.Id;
                }
                catch (DbUpdateException exception)
                    when (GamePersistenceErrors.IsUniqueViolation(exception))
                {
                    LogGameCreationConflict(exception);
                    await transaction.RollbackAsync(innerCancellationToken);
                    dbContext.ChangeTracker.Clear();
                    return (Guid?)null;
                }
            },
            cancellationToken
        );
    }

    public async Task<JoinGamePersistenceResult> JoinGameAsync(
        JoinGamePersistenceRequest request,
        CancellationToken cancellationToken
    )
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(
            ct => ExecuteJoinTransactionAsync(dbContext, request, ct),
            cancellationToken
        );
    }

    private async Task<JoinGamePersistenceResult> ExecuteJoinTransactionAsync(
        ApplicationDbContext dbContext,
        JoinGamePersistenceRequest request,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken
        );

        var game = await dbContext.Games.SingleOrDefaultAsync(
            entry => entry.InviteCode == request.InviteCode,
            cancellationToken
        );

        if (game is null)
        {
            return new JoinGamePersistenceResult(
                new GameCommandFailed("That invite code does not match an open game.")
            );
        }

        if (game.Status != GameStatus.Open)
        {
            return new JoinGamePersistenceResult(
                new GameCommandFailed("That game is no longer open.")
            );
        }

        var activePlayers = await dbContext
            .GamePlayers.Where(entry => entry.GameId == game.Id && entry.LeftAtUtc == null)
            .OrderBy(entry => entry.JoinedAtUtc)
            .ToListAsync(cancellationToken);

        var existingPlayer = activePlayers.SingleOrDefault(entry => entry.UserId == request.UserId);

        if (existingPlayer is not null)
        {
            return await HandlePlayerRejoinAsync(
                dbContext,
                transaction,
                game,
                existingPlayer,
                activePlayers,
                request,
                cancellationToken
            );
        }

        return await AddNewPlayerAsync(
            dbContext,
            transaction,
            game,
            activePlayers,
            request,
            cancellationToken
        );
    }

    private static async Task<JoinGamePersistenceResult> HandlePlayerRejoinAsync(
        ApplicationDbContext dbContext,
        IDbContextTransaction transaction,
        Game game,
        GamePlayer existingPlayer,
        List<GamePlayer> activePlayers,
        JoinGamePersistenceRequest request,
        CancellationToken cancellationToken
    )
    {
        if (
            string.Equals(
                existingPlayer.NormalizedName,
                request.PlayerNameLookup,
                StringComparison.Ordinal
            )
        )
        {
            return new JoinGamePersistenceResult(new GameCommandSucceeded(game.Id));
        }

        if (
            activePlayers.Any(entry =>
                entry.Id != existingPlayer.Id && entry.NormalizedName == request.PlayerNameLookup
            )
        )
        {
            return new JoinGamePersistenceResult(
                new GameCommandFailed("That player name is already taken in this game.")
            );
        }

        existingPlayer.Name = request.PlayerName;
        existingPlayer.NormalizedName = request.PlayerNameLookup;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new JoinGamePersistenceResult(
            new GameCommandSucceeded(game.Id),
            LobbyGameId: game.Id
        );
    }

    private async Task<JoinGamePersistenceResult> AddNewPlayerAsync(
        ApplicationDbContext dbContext,
        IDbContextTransaction transaction,
        Game game,
        List<GamePlayer> activePlayers,
        JoinGamePersistenceRequest request,
        CancellationToken cancellationToken
    )
    {
        if (activePlayers.Count >= game.MaxPlayers)
        {
            return new JoinGamePersistenceResult(
                new GameCommandFailed("That game is already full.")
            );
        }

        if (activePlayers.Any(entry => entry.NormalizedName == request.PlayerNameLookup))
        {
            return new JoinGamePersistenceResult(
                new GameCommandFailed("That player name is already taken in this game.")
            );
        }

        var joinedPlayer = new GamePlayer
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            UserId = request.UserId,
            Name = request.PlayerName,
            NormalizedName = request.PlayerNameLookup,
            JoinedAtUtc = DateTime.UtcNow,
        };

        dbContext.GamePlayers.Add(joinedPlayer);
        dbContext.GameMessages.Add(
            GameMessageFactory.CreateSystemEvent(
                game.Id,
                GameMessageKind.PlayerJoined,
                joinedPlayer.JoinedAtUtc,
                joinedPlayer
            )
        );

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new JoinGamePersistenceResult(
                new GameCommandSucceeded(game.Id),
                LobbyGameId: game.Id,
                MessagesGameId: game.Id
            );
        }
        catch (DbUpdateException exception)
            when (GamePersistenceErrors.IsUniqueViolation(exception))
        {
            LogJoinGameConflict(exception);
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            return new JoinGamePersistenceResult(
                new GameCommandFailed(
                    "That player or seat is no longer available. Refresh and try again."
                )
            );
        }
    }

    public async Task<LeaveGamePersistenceResult> LeaveGameAsync(
        Guid gameId,
        string userId,
        CancellationToken cancellationToken
    )
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var executionStrategy = dbContext.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(
            async innerCancellationToken =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    innerCancellationToken
                );

                var game = await dbContext.Games.SingleOrDefaultAsync(
                    entry => entry.Id == gameId,
                    innerCancellationToken
                );

                if (game is null)
                {
                    return new LeaveGamePersistenceResult(
                        new GameCommandFailed("That game could not be found.")
                    );
                }

                var player = await dbContext.GamePlayers.SingleOrDefaultAsync(
                    entry =>
                        entry.GameId == gameId && entry.UserId == userId && entry.LeftAtUtc == null,
                    innerCancellationToken
                );

                if (player is null)
                {
                    return new LeaveGamePersistenceResult(
                        new GameCommandFailed("You are not an active player in that game.")
                    );
                }

                var now = DateTime.UtcNow;
                var leftMessage = GameMessageFactory.CreateSystemEvent(
                    gameId,
                    GameMessageKind.PlayerLeft,
                    now,
                    player
                );
                player.LeftAtUtc = now;
                player.VisibleThroughMessageId = leftMessage.Id;

                dbContext.GameMessages.Add(leftMessage);

                var remainingPlayers = await dbContext.GamePlayers.CountAsync(
                    entry =>
                        entry.GameId == gameId && entry.LeftAtUtc == null && entry.Id != player.Id,
                    innerCancellationToken
                );

                if (remainingPlayers == 0)
                {
                    game.Status = GameStatus.Ended;
                    game.EndedAtUtc = now;

                    var endedMessage = GameMessageFactory.CreateSystemEvent(
                        gameId,
                        GameMessageKind.GameEnded,
                        now
                    );
                    player.VisibleThroughMessageId = endedMessage.Id;
                    dbContext.GameMessages.Add(endedMessage);
                }

                await dbContext.SaveChangesAsync(innerCancellationToken);
                await transaction.CommitAsync(innerCancellationToken);
                return new LeaveGamePersistenceResult(new GameCommandSucceeded(gameId), player.Id);
            },
            cancellationToken
        );
    }

    public async Task<IReadOnlyList<GameSessionParticipant>?> GetActiveSessionPlayersAsync(
        Guid gameId,
        CancellationToken cancellationToken
    )
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var players = await dbContext
            .GamePlayers.AsNoTracking()
            .Where(entry => entry.GameId == gameId && entry.LeftAtUtc == null)
            .OrderBy(entry => entry.JoinedAtUtc)
            .Select(entry => new GameSessionParticipant(entry.Id))
            .ToListAsync(cancellationToken);

        return players.Count == 0 ? null : players;
    }

    public async Task<GameLobbyView?> GetLobbyAsync(
        Guid gameId,
        string userId,
        CancellationToken cancellationToken
    )
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var game = await dbContext
            .Games.AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.Id == gameId, cancellationToken);

        if (game is null)
        {
            return null;
        }

        var currentPlayer = await dbContext
            .GamePlayers.AsNoTracking()
            .SingleOrDefaultAsync(
                entry =>
                    entry.GameId == gameId && entry.UserId == userId && entry.LeftAtUtc == null,
                cancellationToken
            );

        var formerPlayer = currentPlayer is null
            ? await dbContext
                .GamePlayers.AsNoTracking()
                .Where(entry =>
                    entry.GameId == gameId && entry.UserId == userId && entry.LeftAtUtc != null
                )
                .OrderByDescending(entry => entry.LeftAtUtc)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var viewingPlayer = currentPlayer ?? formerPlayer;
        if (viewingPlayer is null)
        {
            return null;
        }

        var players = await dbContext
            .GamePlayers.AsNoTracking()
            .Where(entry => entry.GameId == gameId && entry.LeftAtUtc == null)
            .OrderBy(entry => entry.JoinedAtUtc)
            .Select(entry => new GamePlayerView(entry.Id, entry.Name, entry.JoinedAtUtc))
            .ToListAsync(cancellationToken);

        return new GameLobbyView(
            game.Id,
            game.Name,
            game.InviteCode,
            game.Status,
            game.MaxPlayers,
            game.CreatedAtUtc,
            game.EndedAtUtc,
            viewingPlayer.Name,
            viewingPlayer.Id,
            players,
            currentPlayer is not null
        );
    }

    public async Task<UserGamesView> GetUserGamesAsync(
        string userId,
        CancellationToken cancellationToken
    )
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var openGames = await dbContext
            .Games.AsNoTracking()
            .Where(game =>
                game.Status == GameStatus.Open
                && game.Players.Any(player => player.UserId == userId && player.LeftAtUtc == null)
            )
            .OrderByDescending(game => game.CreatedAtUtc)
            .Select(game => new GameSummaryView(
                game.Id,
                game.Name,
                game.InviteCode,
                game.Status,
                game.Players.Count(player => player.LeftAtUtc == null),
                game.MaxPlayers,
                game.CreatedAtUtc,
                game.EndedAtUtc,
                game.Players.Where(player => player.UserId == userId && player.LeftAtUtc == null)
                    .OrderByDescending(player => player.JoinedAtUtc)
                    .Select(player => player.Name)
                    .FirstOrDefault()
            ))
            .ToListAsync(cancellationToken);

        var endedGames = await dbContext
            .Games.AsNoTracking()
            .Where(game =>
                game.Status == GameStatus.Ended
                && game.Players.Any(player => player.UserId == userId)
            )
            .OrderByDescending(game => game.EndedAtUtc)
            .ThenByDescending(game => game.CreatedAtUtc)
            .Select(game => new GameSummaryView(
                game.Id,
                game.Name,
                game.InviteCode,
                game.Status,
                game.Players.Count(player => player.LeftAtUtc == null),
                game.MaxPlayers,
                game.CreatedAtUtc,
                game.EndedAtUtc,
                game.Players.Where(player => player.UserId == userId)
                    .OrderByDescending(player => player.LeftAtUtc ?? player.JoinedAtUtc)
                    .Select(player => player.Name)
                    .FirstOrDefault()
            ))
            .ToListAsync(cancellationToken);

        return new UserGamesView(openGames, endedGames);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Retrying game creation after a uniqueness conflict."
    )]
    private partial void LogGameCreationConflict(Exception exception);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "A uniqueness constraint blocked joining the game."
    )]
    private partial void LogJoinGameConflict(Exception exception);

    public async Task<IReadOnlyList<GamePlayerView>> GetActivePlayersAsync(
        Guid gameId,
        CancellationToken cancellationToken
    )
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext
            .GamePlayers.AsNoTracking()
            .Where(entry => entry.GameId == gameId && entry.LeftAtUtc == null)
            .OrderBy(entry => entry.JoinedAtUtc)
            .Select(entry => new GamePlayerView(entry.Id, entry.Name, entry.JoinedAtUtc))
            .ToListAsync(cancellationToken);
    }
}

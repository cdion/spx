using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Spx.Data;

namespace Spx.Games;

public sealed class GameService(
    ApplicationDbContext dbContext,
    IGameLobbyEventsPublisher gameLobbyEventsPublisher,
    IGameMessageEventsPublisher gameMessageEventsPublisher,
    ILogger<GameService> logger) : IGameService
{
    private const int MaxPlayersPerGame = 2;
    private const int MaxCreateAttempts = 10;

    public async Task<GameCommandResult> CreateGameAsync(string userId, CreateGameRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeGameName(request.GameName, out var gameName, out var gameNameError))
        {
            return GameCommandResult.Failure(gameNameError);
        }

        if (!TryNormalizePlayerName(request.PlayerName, out var playerName, out var playerNameLookup, out var playerNameError))
        {
            return GameCommandResult.Failure(playerNameError);
        }

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();

        for (var attempt = 0; attempt < MaxCreateAttempts; attempt++)
        {
            var gameId = await executionStrategy.ExecuteAsync(async innerCancellationToken =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, innerCancellationToken);
                var now = DateTime.UtcNow;
                var game = new Game
                {
                    Id = Guid.NewGuid(),
                    Name = gameName,
                    InviteCode = InviteCodeGenerator.Generate(),
                    CreatedAtUtc = now,
                    CreatedByUserId = userId,
                    MaxPlayers = MaxPlayersPerGame,
                    Status = GameStatus.Open
                };

                var player = new GamePlayer
                {
                    Id = Guid.NewGuid(),
                    GameId = game.Id,
                    UserId = userId,
                    Name = playerName,
                    NormalizedName = playerNameLookup,
                    JoinedAtUtc = now
                };

                var createdMessage = GameMessageFactory.CreateSystemEvent(game.Id, GameMessageKind.GameCreated, now, player);

                dbContext.Games.Add(game);
                dbContext.GamePlayers.Add(player);
                dbContext.GameMessages.Add(createdMessage);

                try
                {
                    await dbContext.SaveChangesAsync(innerCancellationToken);
                    await transaction.CommitAsync(innerCancellationToken);
                    return game.Id;
                }
                catch (DbUpdateException exception) when (IsUniqueViolation(exception))
                {
                    logger.LogInformation(exception, "Retrying game creation after a uniqueness conflict.");
                    await transaction.RollbackAsync(innerCancellationToken);
                    dbContext.ChangeTracker.Clear();
                    return (Guid?)null;
                }
            }, cancellationToken);

            if (gameId.HasValue)
            {
                await gameLobbyEventsPublisher.PublishLobbyChangedAsync(gameId.Value, cancellationToken);
                await gameMessageEventsPublisher.PublishMessagesChangedAsync(gameId.Value, cancellationToken);
                return GameCommandResult.Success(gameId.Value);
            }
        }

        return GameCommandResult.Failure("A unique invite code could not be reserved. Please try again.");
    }

    public async Task<GameCommandResult> JoinGameAsync(string userId, JoinGameRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryNormalizePlayerName(request.PlayerName, out var playerName, out var playerNameLookup, out var playerNameError))
        {
            return GameCommandResult.Failure(playerNameError);
        }

        var inviteCode = InviteCodeGenerator.NormalizeInviteCode(request.InviteCode);
        if (inviteCode.Length != 6)
        {
            return GameCommandResult.Failure("Invite codes must be six characters long.");
        }

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        var joinResult = await executionStrategy.ExecuteAsync(async innerCancellationToken =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, innerCancellationToken);

            var game = await dbContext.Games
                .SingleOrDefaultAsync(entry => entry.InviteCode == inviteCode, innerCancellationToken);

            if (game is null)
            {
                return (GameCommandResult.Failure("That invite code does not match an open game."), (Guid?)null, false);
            }

            if (game.Status != GameStatus.Open)
            {
                return (GameCommandResult.Failure("That game is no longer open."), (Guid?)null, false);
            }

            var activePlayers = await dbContext.GamePlayers
                .Where(entry => entry.GameId == game.Id && entry.LeftAtUtc == null)
                .OrderBy(entry => entry.JoinedAtUtc)
                .ToListAsync(innerCancellationToken);

            var existingPlayer = activePlayers.SingleOrDefault(entry => entry.UserId == userId);
            if (existingPlayer is not null)
            {
                if (!string.Equals(existingPlayer.NormalizedName, playerNameLookup, StringComparison.Ordinal))
                {
                    if (activePlayers.Any(entry => entry.Id != existingPlayer.Id && entry.NormalizedName == playerNameLookup))
                    {
                        return (GameCommandResult.Failure("That player name is already taken in this game."), (Guid?)null, false);
                    }

                    existingPlayer.Name = playerName;
                    existingPlayer.NormalizedName = playerNameLookup;
                    await dbContext.SaveChangesAsync(innerCancellationToken);
                    await transaction.CommitAsync(innerCancellationToken);
                    return (GameCommandResult.Success(game.Id), game.Id, false);
                }

                return (GameCommandResult.Success(game.Id), (Guid?)null, false);
            }

            if (activePlayers.Count >= game.MaxPlayers)
            {
                return (GameCommandResult.Failure("That game is already full."), (Guid?)null, false);
            }

            if (activePlayers.Any(entry => entry.NormalizedName == playerNameLookup))
            {
                return (GameCommandResult.Failure("That player name is already taken in this game."), (Guid?)null, false);
            }

            dbContext.GamePlayers.Add(new GamePlayer
            {
                Id = Guid.NewGuid(),
                GameId = game.Id,
                UserId = userId,
                Name = playerName,
                NormalizedName = playerNameLookup,
                JoinedAtUtc = DateTime.UtcNow
            });

            var joinedPlayer = dbContext.ChangeTracker.Entries<GamePlayer>()
                .Single(entry => entry.Entity.GameId == game.Id && entry.Entity.UserId == userId && entry.Entity.LeftAtUtc == null)
                .Entity;

            dbContext.GameMessages.Add(GameMessageFactory.CreateSystemEvent(game.Id, GameMessageKind.PlayerJoined, joinedPlayer.JoinedAtUtc, joinedPlayer));

            try
            {
                await dbContext.SaveChangesAsync(innerCancellationToken);
                await transaction.CommitAsync(innerCancellationToken);
                return (GameCommandResult.Success(game.Id), game.Id, true);
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                logger.LogInformation(exception, "A uniqueness constraint blocked joining the game.");
                await transaction.RollbackAsync(innerCancellationToken);
                dbContext.ChangeTracker.Clear();
                return (GameCommandResult.Failure("That player or seat is no longer available. Refresh and try again."), (Guid?)null, false);
            }
        }, cancellationToken);

        if (joinResult.Item2.HasValue)
        {
            await gameLobbyEventsPublisher.PublishLobbyChangedAsync(joinResult.Item2.Value, cancellationToken);
            if (joinResult.Item3)
            {
                await gameMessageEventsPublisher.PublishMessagesChangedAsync(joinResult.Item2.Value, cancellationToken);
            }
        }

        return joinResult.Item1;
    }

    public async Task<GameCommandResult> LeaveGameAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
    {
        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        var leaveResult = await executionStrategy.ExecuteAsync(async innerCancellationToken =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, innerCancellationToken);

            var game = await dbContext.Games
                .SingleOrDefaultAsync(entry => entry.Id == gameId, innerCancellationToken);

            if (game is null)
            {
                return (GameCommandResult.Failure("That game could not be found."), false);
            }

            var player = await dbContext.GamePlayers
                .SingleOrDefaultAsync(entry => entry.GameId == gameId && entry.UserId == userId && entry.LeftAtUtc == null, innerCancellationToken);

            if (player is null)
            {
                return (GameCommandResult.Failure("You are not an active player in that game."), false);
            }

            var now = DateTime.UtcNow;
            var leftMessage = GameMessageFactory.CreateSystemEvent(gameId, GameMessageKind.PlayerLeft, now, player);
            player.LeftAtUtc = now;
            player.VisibleThroughMessageId = leftMessage.Id;

            dbContext.GameMessages.Add(leftMessage);

            var remainingPlayers = await dbContext.GamePlayers
                .CountAsync(entry => entry.GameId == gameId && entry.LeftAtUtc == null && entry.Id != player.Id, innerCancellationToken);

            if (remainingPlayers == 0)
            {
                game.Status = GameStatus.Ended;
                game.EndedAtUtc = now;

                var endedMessage = GameMessageFactory.CreateSystemEvent(gameId, GameMessageKind.GameEnded, now);
                player.VisibleThroughMessageId = endedMessage.Id;
                dbContext.GameMessages.Add(endedMessage);
            }

            await dbContext.SaveChangesAsync(innerCancellationToken);
            await transaction.CommitAsync(innerCancellationToken);
            return (GameCommandResult.Success(gameId), true);
        }, cancellationToken);

        if (leaveResult.Item2)
        {
            await gameLobbyEventsPublisher.PublishLobbyChangedAsync(gameId, cancellationToken);
            await gameMessageEventsPublisher.PublishMessagesChangedAsync(gameId, cancellationToken);
        }

        return leaveResult.Item1;
    }

    public async Task<GameLobbyView?> GetLobbyAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
    {
        var game = await dbContext.Games
            .AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.Id == gameId, cancellationToken);

        if (game is null)
        {
            return null;
        }

        var currentPlayer = await dbContext.GamePlayers
            .AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.GameId == gameId && entry.UserId == userId && entry.LeftAtUtc == null, cancellationToken);

        var formerPlayer = currentPlayer is null
            ? await dbContext.GamePlayers
                .AsNoTracking()
                .Where(entry => entry.GameId == gameId && entry.UserId == userId && entry.LeftAtUtc != null)
                .OrderByDescending(entry => entry.LeftAtUtc)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var viewingPlayer = currentPlayer ?? formerPlayer;
        if (viewingPlayer is null)
        {
            return null;
        }

        var players = await dbContext.GamePlayers
            .AsNoTracking()
            .Where(entry => entry.GameId == gameId && entry.LeftAtUtc == null)
            .OrderBy(entry => entry.JoinedAtUtc)
            .Select(entry => new GamePlayerView(entry.Id, entry.Name, entry.JoinedAtUtc, entry.UserId == userId))
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
            players,
            currentPlayer is not null);
    }

    public async Task<UserGamesView> GetUserGamesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var openGames = await dbContext.Games
            .AsNoTracking()
            .Where(game => game.Status == GameStatus.Open
                && game.Players.Any(player => player.UserId == userId && player.LeftAtUtc == null))
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
                game.Players
                    .Where(player => player.UserId == userId && player.LeftAtUtc == null)
                    .OrderByDescending(player => player.JoinedAtUtc)
                    .Select(player => player.Name)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        var endedGames = await dbContext.Games
            .AsNoTracking()
            .Where(game => game.Status == GameStatus.Ended
                && game.Players.Any(player => player.UserId == userId))
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
                game.Players
                    .Where(player => player.UserId == userId)
                    .OrderByDescending(player => player.LeftAtUtc ?? player.JoinedAtUtc)
                    .Select(player => player.Name)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new UserGamesView(openGames, endedGames);
    }

    private static bool TryNormalizeGameName(string value, out string normalizedValue, out string errorMessage)
    {
        normalizedValue = GameInputNormalizer.NormalizeDisplayText(value);

        if (normalizedValue.Length < 2)
        {
            errorMessage = "Game names must be at least 2 characters long.";
            return false;
        }

        if (normalizedValue.Length > 100)
        {
            errorMessage = "Game names must be 100 characters or fewer.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private static bool TryNormalizePlayerName(string value, out string normalizedValue, out string normalizedLookupValue, out string errorMessage)
    {
        normalizedValue = GameInputNormalizer.NormalizeDisplayText(value);
        normalizedLookupValue = GameInputNormalizer.NormalizeLookupKey(value);

        if (normalizedValue.Length < 2)
        {
            errorMessage = "Player names must be at least 2 characters long.";
            return false;
        }

        if (normalizedValue.Length > 40)
        {
            errorMessage = "Player names must be 40 characters or fewer.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
        => exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
using Spx.Contracts;
using Spx.Game.Application;
using Spx.Game.Application.Features.LeaveGame;
using Spx.Game.Application.Features.SubmitAcquireCard;
using Spx.Game.Application.Features.SubmitPlayBatch;

namespace Spx.Web.Components.Pages;

internal sealed class GamePageActionCoordinator(
    ILeaveGameHandler leaveGameHandler,
    ISubmitAcquireCardHandler submitAcquireCardHandler,
    ISubmitPlayBatchHandler submitPlayBatchHandler,
    IGameplayEventMessageFormatter gameplayEventMessageFormatter,
    ILogger<GamePageActionCoordinator> logger,
    GamePageDataState data,
    GamePageActionState actions,
    GameTimelineState timeline)
{
    public async Task<bool> LeaveGameAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
    {
        if (!actions.TryBeginLeave())
        {
            return false;
        }

        data.ClearErrorMessage();

        try
        {
            var result = await leaveGameHandler.HandleAsync(gameId, userId, cancellationToken);

            if (result is GameCommandFailed failed)
            {
                data.SetErrorMessage(failed.ErrorMessage);
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to leave game {GameId} for user {UserId}.", gameId, userId);
            data.SetErrorMessage("We couldn't leave the game right now. Please try again.");
            return false;
        }
        finally
        {
            actions.CompleteLeave();
        }
    }

    public async Task AcquireCardAsync(Guid gameId, Guid playerId, Guid marketCardInstanceId, CancellationToken cancellationToken = default)
    {
        var lobby = data.Lobby;
        var session = data.Session;
        if (lobby is null || session is null || !lobby.IsCurrentUserActive || !actions.TryBeginGameplayAction())
        {
            return;
        }

        data.ClearGameplayError();

        try
        {
            var result = await submitAcquireCardHandler.HandleAsync(gameId, playerId, session.RoundNumber, marketCardInstanceId, cancellationToken);
            if (result is not GameSessionCommandSucceeded succeeded)
            {
                data.SetGameplayError(((GameSessionCommandFailed)result).ErrorMessage);
                return;
            }

            data.ApplySession(succeeded.Session);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to submit an acquire choice for game {GameId} player {PlayerId}.", gameId, playerId);
            data.SetGameplayError("We couldn't lock your acquire choice right now. Please try again.");
        }
        finally
        {
            actions.CompleteGameplayAction();
        }
    }

    public async Task LockBatchAsync(Guid gameId, Guid playerId, IReadOnlyList<GameBatchCardSelection> cards, CancellationToken cancellationToken = default)
    {
        var lobby = data.Lobby;
        var session = data.Session;
        if (lobby is null || session is null || !lobby.IsCurrentUserActive || !actions.TryBeginGameplayAction())
        {
            return;
        }

        data.ClearGameplayError();

        try
        {
            var result = await submitPlayBatchHandler.HandleAsync(gameId, playerId, session.RoundNumber, cards, cancellationToken);
            if (result is not GameSessionCommandSucceeded succeeded)
            {
                data.SetGameplayError(((GameSessionCommandFailed)result).ErrorMessage);
                return;
            }

            data.ApplySession(succeeded.Session);
            AddImmediateGameplayEntries(lobby, succeeded.Session, succeeded.GameplayEvents);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to submit a play batch for game {GameId} player {PlayerId}.", gameId, playerId);
            data.SetGameplayError("We couldn't lock your play batch right now. Please try again.");
        }
        finally
        {
            actions.CompleteGameplayAction();
        }
    }

    private void AddImmediateGameplayEntries(
        GameLobbyView lobby,
        GameSessionView updatedSession,
        IReadOnlyList<GameplayEvent> gameplayEvents)
    {
        if (updatedSession.LastResolvedBatch is null || gameplayEvents.Count == 0)
        {
            return;
        }

        var playerNames = lobby.Players.ToDictionary(player => player.PlayerId, player => player.Name);
        var messageBodies = gameplayEventMessageFormatter.CreateMessageBodies(updatedSession.LastResolvedBatch, updatedSession.Completion, gameplayEvents, playerNames);
        if (messageBodies.Count == 0)
        {
            return;
        }

        timeline.AddImmediateGameplayEntries(messageBodies, updatedSession.LastResolvedBatch.ResolvedAtUtc);
    }
}
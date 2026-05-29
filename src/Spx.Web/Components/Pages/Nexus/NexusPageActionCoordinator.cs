using Spx.Game.Application;
using Spx.Game.Application.Features.LeaveGame;
using Spx.Game.Application.Nexus.Features.SubmitOrders;
using Spx.Nexus.Domain;

namespace Spx.Web.Components.Pages.Nexus;

internal sealed partial class NexusPageActionCoordinator(
    ILeaveGameHandler leaveGameHandler,
    ISubmitOrdersHandler submitOrdersHandler,
    ILogger<NexusPageActionCoordinator> logger,
    NexusPageDataState data,
    NexusPageActionState actions
)
{
    public async Task<bool> LeaveGameAsync(
        Guid gameId,
        string userId,
        CancellationToken cancellationToken = default
    )
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
            LogLeaveGameFailed(logger, exception, gameId, userId);
            data.SetErrorMessage("We couldn't leave the game right now. Please try again.");
            return false;
        }
        finally
        {
            actions.CompleteLeave();
        }
    }

    public async Task SubmitOrdersAsync(
        Guid gameId,
        NexusTurnOrdersCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var lobby = data.Lobby;
        var session = data.Session;
        if (
            lobby is null
            || session is null
            || !lobby.IsCurrentUserActive
            || !actions.TryBeginGameplayAction()
        )
        {
            return;
        }

        data.ClearGameplayError();

        try
        {
            var result = await submitOrdersHandler.HandleAsync(gameId, command, cancellationToken);
            if (result is not GameSessionCommandSucceeded succeeded)
            {
                data.SetGameplayError(((GameSessionCommandFailed)result).ErrorMessage);
                return;
            }

            data.ApplySession(succeeded.Session);
        }
        catch (Exception exception)
        {
            LogSubmitOrdersFailed(logger, exception, gameId, command.PlayerId);
            data.SetGameplayError("We couldn't submit your orders right now. Please try again.");
        }
        finally
        {
            actions.CompleteGameplayAction();
        }
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to leave game {GameId} for user {UserId}."
    )]
    private static partial void LogLeaveGameFailed(
        ILogger logger,
        Exception exception,
        Guid gameId,
        string userId
    );

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to submit orders for game {GameId} player {PlayerId}."
    )]
    private static partial void LogSubmitOrdersFailed(
        ILogger logger,
        Exception exception,
        Guid gameId,
        Guid playerId
    );
}

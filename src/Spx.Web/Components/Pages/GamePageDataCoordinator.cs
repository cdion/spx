using Spx.Game.Application;
using Spx.Game.Application.Features.GetGamePage;
using Spx.Game.Application.Features.GetGamePresence;
using Spx.Game.Application.Features.GetGameSession;

namespace Spx.Web.Components.Pages;

internal sealed partial class GamePageDataCoordinator(
    IGetGamePageHandler getGamePageHandler,
    IGetGameSessionHandler getGameSessionHandler,
    IGetGamePresenceHandler getGamePresenceHandler,
    ILogger<GamePageDataCoordinator> logger,
    GamePageDataState state
)
{
    public async Task LoadPageAsync(
        Guid gameId,
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        state.BeginPageLoad();

        try
        {
            state.ApplyPage(
                await getGamePageHandler.HandleAsync(gameId, userId, cancellationToken)
            );
        }
        catch (Exception exception)
        {
            LogLoadPageFailed(logger, exception, gameId, userId);
            state.FailPageLoad("We couldn't load this game right now. Refresh and try again.");
        }
    }

    public async Task ReloadPresenceAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            state.ApplyPresence(
                await getGamePresenceHandler.HandleAsync(gameId, cancellationToken)
            );
        }
        catch (Exception exception)
        {
            LogRefreshPresenceFailed(logger, exception, gameId);
        }
    }

    public async Task ReloadSessionAsync(
        Guid gameId,
        Guid playerId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            state.ApplySession(
                await getGameSessionHandler.HandleAsync(gameId, playerId, cancellationToken)
            );
        }
        catch (Exception exception)
        {
            LogRefreshSessionFailed(logger, exception, gameId, playerId);
            state.TrySetGameplayError(
                "We couldn't refresh the game state right now. Please try again."
            );
        }
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to load game page {GameId} for user {UserId}."
    )]
    private static partial void LogLoadPageFailed(
        ILogger logger,
        Exception exception,
        Guid gameId,
        string userId
    );

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to refresh presence for game {GameId}."
    )]
    private static partial void LogRefreshPresenceFailed(
        ILogger logger,
        Exception exception,
        Guid gameId
    );

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to refresh session state for game {GameId} player {PlayerId}."
    )]
    private static partial void LogRefreshSessionFailed(
        ILogger logger,
        Exception exception,
        Guid gameId,
        Guid playerId
    );
}

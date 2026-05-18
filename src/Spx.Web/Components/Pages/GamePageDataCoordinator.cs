using Spx.Game.Application;
using Spx.Game.Application.Features.GetGamePage;
using Spx.Game.Application.Features.GetGamePresence;
using Spx.Game.Application.Features.GetGameSession;

namespace Spx.Web.Components.Pages;

internal sealed class GamePageDataCoordinator(
    IGetGamePageHandler getGamePageHandler,
    IGetGameSessionHandler getGameSessionHandler,
    IGetGamePresenceHandler getGamePresenceHandler,
    ILogger<GamePageDataCoordinator> logger,
    GamePageDataState state)
{
    public async Task LoadPageAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
    {
        state.BeginPageLoad();

        try
        {
            state.ApplyPage(await getGamePageHandler.HandleAsync(gameId, userId, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to load game page {GameId} for user {UserId}.", gameId, userId);
            state.FailPageLoad("We couldn't load this game right now. Refresh and try again.");
        }
    }

    public async Task ReloadPresenceAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        try
        {
            state.ApplyPresence(await getGamePresenceHandler.HandleAsync(gameId, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to refresh presence for game {GameId}.", gameId);
        }
    }

    public async Task ReloadSessionAsync(Guid gameId, Guid playerId, CancellationToken cancellationToken = default)
    {
        try
        {
            state.ApplySession(await getGameSessionHandler.HandleAsync(gameId, playerId, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to refresh session state for game {GameId} player {PlayerId}.", gameId, playerId);
            state.TrySetGameplayError("We couldn't refresh the game state right now. Please try again.");
        }
    }
}
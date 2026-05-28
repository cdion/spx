using Spx.Nexus.Application;
using Spx.Nexus.Application.Features.GetGamePresence;
using Spx.Nexus.Application.Features.GetNexusPage;

namespace Spx.Web.Components.Pages.Nexus;

internal sealed partial class NexusPageDataCoordinator(
    IGetNexusPageHandler getGamePageHandler,
    IGetGamePresenceHandler getGamePresenceHandler,
    ILogger<NexusPageDataCoordinator> logger,
    NexusPageDataState state
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
            state.FailPageLoad("This game could not be loaded.");
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
}

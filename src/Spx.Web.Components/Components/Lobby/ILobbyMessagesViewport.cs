namespace Spx.Web.Components.Lobby;

public interface ILobbyMessagesViewport
{
    Task<TimelineScrollMetrics> GetScrollMetricsAsync();

    Task RestoreScrollAfterPrependAsync(double previousScrollHeight, double previousScrollTop);

    Task ScrollToBottomAsync();

    Task<bool> IsNearBottomAsync();
}

using Spx.Game.Application;
using Spx.Game.Application.Nexus;

namespace Spx.Web.Components.Pages.Nexus;

internal sealed class NexusPageDataState
{
    public GameLobbyView? Lobby { get; private set; }

    public NexusSessionView? Session { get; private set; }

    public GamePresenceView Presence { get; private set; } = GamePresenceView.Empty;

    public string? ErrorMessage { get; private set; }

    public string? GameplayError { get; private set; }

    public bool IsLoading { get; private set; } = true;

    public void BeginPageLoad()
    {
        IsLoading = true;
        ErrorMessage = null;
        GameplayError = null;
    }

    public void ApplyPage(GamePageView? page)
    {
        Lobby = page?.Lobby;
        Session = page?.Session;
        Presence = page?.Presence ?? GamePresenceView.Empty;
        IsLoading = false;
    }

    public void FailPageLoad(string message)
    {
        Lobby = null;
        Session = null;
        Presence = GamePresenceView.Empty;
        ErrorMessage = message;
        IsLoading = false;
    }

    public void ApplyPresence(GamePresenceView presence) => Presence = presence;

    public void ClearPresence() => Presence = GamePresenceView.Empty;

    public void ApplySession(NexusSessionView? session) => Session = session;

    public void ClearErrorMessage() => ErrorMessage = null;

    public void SetErrorMessage(string message) => ErrorMessage = message;

    public void ClearGameplayError() => GameplayError = null;

    public void SetGameplayError(string message) => GameplayError = message;

    public void TrySetGameplayError(string message) => GameplayError ??= message;
}

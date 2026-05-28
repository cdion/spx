namespace Spx.Web.Components.Nexus;

public static class NexusGameplayPanelState
{
    public static bool ShouldResetUiState(
        Guid sessionGameId,
        int sessionRound,
        Guid? lastKnownGameId,
        int? lastKnownRound
    ) => sessionGameId != lastKnownGameId || sessionRound != lastKnownRound;
}

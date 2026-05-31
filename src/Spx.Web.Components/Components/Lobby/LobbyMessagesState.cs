using Spx.Game.Application;

namespace Spx.Web.Components.Lobby;

public sealed class LobbyMessagesState
{
    public required string CurrentUserName { get; init; }

    public IReadOnlyList<TimelineEntryState> Items { get; init; } = [];

    public string? TimelineError { get; init; }

    public string? ComposerError { get; init; }

    public bool IsTimelineLoading { get; init; }

    public bool IsLoadingOlderMessages { get; init; }

    public bool HasOlderMessages { get; init; }

    public Guid? EditingMessageId { get; init; }

    public string EditMessageText { get; init; } = string.Empty;

    public bool IsSavingEdit { get; init; }

    public int ComposerResetVersion { get; init; }

    public bool IsSendingMessage { get; init; }

    public bool IsCurrentUserActive { get; init; }

    public Guid CurrentPlayerId { get; init; }

    public IReadOnlyList<GamePlayerView> Players { get; init; } = [];
}

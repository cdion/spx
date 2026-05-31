using Microsoft.AspNetCore.Components;
using Spx.Game.Application;

namespace Spx.Web.Components.Lobby;

public sealed class LobbyMessagesCallbacks
{
    public EventCallback LoadOlderMessagesAsync { get; init; }

    public EventCallback<GameTimelineEntryView> BeginEditAsync { get; init; }

    public EventCallback<string> SetEditMessageTextAsync { get; init; }

    public EventCallback CancelEditAsync { get; init; }

    public EventCallback SaveEditAsync { get; init; }

    public EventCallback<Guid> DeleteMessageAsync { get; init; }

    public EventCallback<Guid> RetryPendingMessageAsync { get; init; }

    public EventCallback<Guid> DismissPendingMessageAsync { get; init; }

    public EventCallback<LobbyMessageComposerSubmitRequest> SendMessageAsync { get; init; }
}

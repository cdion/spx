using Spx.Game.Application;

namespace Spx.Web.Components.Lobby;

public sealed class LobbyMessagesCallbacks
{
    public Func<Task> LoadOlderMessagesAsync { get; init; } = static () => Task.CompletedTask;

    public Func<GameTimelineEntryView, Task> BeginEditAsync { get; init; } =
        static _ => Task.CompletedTask;

    public Func<string, Task> SetEditMessageTextAsync { get; init; } =
        static _ => Task.CompletedTask;

    public Func<Task> CancelEditAsync { get; init; } = static () => Task.CompletedTask;

    public Func<Task> SaveEditAsync { get; init; } = static () => Task.CompletedTask;

    public Func<Guid, Task> DeleteMessageAsync { get; init; } = static _ => Task.CompletedTask;

    public Func<Guid, Task> RetryPendingMessageAsync { get; init; } =
        static _ => Task.CompletedTask;

    public Func<Guid, Task> DismissPendingMessageAsync { get; init; } =
        static _ => Task.CompletedTask;

    public Func<LobbyMessageComposerSubmitRequest, Task> SendMessageAsync { get; init; } =
        static _ => Task.CompletedTask;
}

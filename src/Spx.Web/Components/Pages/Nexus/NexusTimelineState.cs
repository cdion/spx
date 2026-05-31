using Spx.Game.Application;
using Spx.Web.Components.Lobby;

namespace Spx.Web.Components.Pages.Nexus;

public sealed class NexusTimelineState
{
    private readonly List<TimelineEntryState> items = [];

    public IReadOnlyList<TimelineEntryState> Items => items;

    public string? TimelineError { get; private set; }

    public string? ComposerError { get; private set; }

    public bool IsTimelineLoading { get; private set; } = true;

    public bool IsLoadingOlderMessages { get; private set; }

    public bool IsSendingMessage { get; private set; }

    public bool IsSavingEdit { get; private set; }

    public int ComposerResetVersion { get; private set; }

    public Guid? OldestMessageId { get; private set; }

    public Guid? NewestMessageId { get; private set; }

    public bool HasOlderMessages { get; private set; }

    public Guid? EditingMessageId { get; private set; }

    public string EditMessageText { get; set; } = string.Empty;

    public double RestoreScrollHeight { get; private set; }

    public double RestoreScrollTop { get; private set; }

    public bool ShouldRestoreScrollAfterOlderLoad { get; private set; }

    public bool ShouldScrollTimelineToBottom { get; private set; }

    public void Reset()
    {
        items.Clear();
        OldestMessageId = null;
        NewestMessageId = null;
        HasOlderMessages = false;
        IsTimelineLoading = true;
        IsLoadingOlderMessages = false;
        TimelineError = null;
        ComposerError = null;
        ComposerResetVersion++;
        EditingMessageId = null;
        EditMessageText = string.Empty;
        RestoreScrollHeight = 0;
        RestoreScrollTop = 0;
        ShouldRestoreScrollAfterOlderLoad = false;
        ShouldScrollTimelineToBottom = false;
        IsSendingMessage = false;
        IsSavingEdit = false;
    }

    public void SetTimelineError(string message) => TimelineError = message;

    public void ClearTimelineError() => TimelineError = null;

    public void SetComposerError(string message) => ComposerError = message;

    public void ClearComposerError() => ComposerError = null;

    public void BeginInitialLoad() => IsTimelineLoading = true;

    public void CompleteInitialLoad() => IsTimelineLoading = false;

    public void ApplyInitialPage(GameTimelinePageView? page)
    {
        items.Clear();

        if (page is null)
        {
            HasOlderMessages = false;
            OldestMessageId = null;
            NewestMessageId = null;
            return;
        }

        var orderedItems = page.Items.OrderBy(entry => entry.Id).ToList();
        items.AddRange(
            orderedItems.Select(entry => new TimelineEntryState { Key = entry.Id, Message = entry })
        );

        HasOlderMessages = page.HasMore;
        OldestMessageId = orderedItems.FirstOrDefault()?.Id;
        NewestMessageId = orderedItems.LastOrDefault()?.Id;
    }

    public void BeginOlderLoad() => IsLoadingOlderMessages = true;

    public void CompleteOlderLoad() => IsLoadingOlderMessages = false;

    public void ApplyOlderPage(GameTimelinePageView? page, TimelineScrollMetrics metrics)
    {
        if (page is null)
        {
            return;
        }

        var existingIds = items
            .Where(entry => entry.Message is not null)
            .Select(entry => entry.Message!.Id)
            .ToHashSet();
        var olderItems = page
            .Items.Where(entry => !existingIds.Contains(entry.Id))
            .OrderBy(entry => entry.Id)
            .Select(entry => new TimelineEntryState { Key = entry.Id, Message = entry })
            .ToList();

        if (olderItems.Count > 0)
        {
            items.InsertRange(0, olderItems);
            OldestMessageId = items.FirstOrDefault(entry => entry.Message is not null)?.Message?.Id;
            RestoreScrollHeight = metrics.ScrollHeight;
            RestoreScrollTop = metrics.ScrollTop;
            ShouldRestoreScrollAfterOlderLoad = true;
        }

        HasOlderMessages = page.HasMore;
    }

    public void UpsertPersistedMessages(IEnumerable<GameTimelineEntryView> messages)
    {
        foreach (var message in messages)
        {
            RemoveMatchingLocalTimelineItems(message);

            var existingItem = items.SingleOrDefault(entry => entry.Message?.Id == message.Id);
            if (existingItem is not null)
            {
                existingItem.Message = message;
                continue;
            }

            items.Add(new TimelineEntryState { Key = message.Id, Message = message });
        }

        SortItems();
        OldestMessageId = items.FirstOrDefault(entry => entry.Message is not null)?.Message?.Id;
        NewestMessageId = items.LastOrDefault(entry => entry.Message is not null)?.Message?.Id;
    }

    public void AddImmediateGameplayEntries(
        IReadOnlyList<string> messageBodies,
        DateTime createdAtUtc
    )
    {
        var addedAny = false;

        foreach (var body in messageBodies)
        {
            var alreadyExists = items.Any(entry =>
                entry.Message is not null
                && entry.Message.Kind == GameMessageKind.GameplayEvent
                && entry.Message.CreatedAtUtc == createdAtUtc
                && string.Equals(entry.Message.Body, body, StringComparison.Ordinal)
            );

            if (alreadyExists)
            {
                continue;
            }

            var alreadyBuffered = items.Any(entry =>
                entry.Local is not null
                && entry.Local.Kind == GameMessageKind.GameplayEvent
                && entry.Local.CreatedAtUtc == createdAtUtc
                && string.Equals(entry.Local.Body, body, StringComparison.Ordinal)
            );

            if (alreadyBuffered)
            {
                continue;
            }

            items.Add(
                new TimelineEntryState
                {
                    Key = Guid.NewGuid(),
                    Local = new LocalTimelineMessageState(
                        "Gameplay",
                        body,
                        createdAtUtc,
                        GameMessageKind.GameplayEvent
                    ),
                }
            );
            addedAny = true;
        }

        if (!addedAny)
        {
            return;
        }

        SortItems();
        RequestScrollToBottom();
    }

    public TimelineEntryState AddPendingMessage(
        string body,
        Guid? recipientPlayerId,
        string recipientDisplayName,
        bool isPrivate
    )
    {
        var entry = new TimelineEntryState
        {
            Key = Guid.NewGuid(),
            Pending = new PendingMessageState(
                body,
                recipientPlayerId,
                recipientDisplayName,
                DateTime.UtcNow,
                isPrivate,
                false
            ),
        };

        items.Add(entry);
        RequestScrollToBottom();
        return entry;
    }

    public TimelineEntryState? FindPendingMessage(Guid itemKey) =>
        items.SingleOrDefault(entry => entry.Key == itemKey && entry.Pending is not null);

    public void SetPendingMessageFailed(Guid itemKey, bool failed)
    {
        var item = FindPendingMessage(itemKey);
        if (item?.Pending is null)
        {
            return;
        }

        item.Pending.Failed = failed;
    }

    public void DismissPendingMessage(Guid itemKey) =>
        items.RemoveAll(entry => entry.Key == itemKey && entry.Pending is not null);

    public void ReplacePendingWithPersistedMessage(Guid itemKey, GameTimelineEntryView message)
    {
        items.RemoveAll(entry => entry.Key == itemKey);
        UpsertPersistedMessages([message]);
    }

    public void BeginEdit(GameTimelineEntryView message)
    {
        EditingMessageId = message.Id;
        EditMessageText = message.Body;
    }

    public void CancelEdit()
    {
        EditingMessageId = null;
        EditMessageText = string.Empty;
        IsSavingEdit = false;
    }

    public void BeginSend() => IsSendingMessage = true;

    public void CompleteSend() => IsSendingMessage = false;

    public void ClearComposer()
    {
        ComposerResetVersion++;
    }

    public void BeginSaveEdit() => IsSavingEdit = true;

    public void CompleteSaveEdit() => IsSavingEdit = false;

    public void RequestScrollToBottom() => ShouldScrollTimelineToBottom = true;

    public void MarkScrollToBottomHandled() => ShouldScrollTimelineToBottom = false;

    public void MarkRestoreScrollHandled() => ShouldRestoreScrollAfterOlderLoad = false;

    private void RemoveMatchingLocalTimelineItems(GameTimelineEntryView message)
    {
        if (message.Kind != GameMessageKind.GameplayEvent)
        {
            return;
        }

        items.RemoveAll(entry =>
            entry.Local is not null
            && entry.Local.Kind == GameMessageKind.GameplayEvent
            && entry.Local.CreatedAtUtc == message.CreatedAtUtc
            && string.Equals(entry.Local.Body, message.Body, StringComparison.Ordinal)
        );
    }

    private void SortItems() =>
        items.Sort(static (left, right) => CompareTimelineItems(left, right));

    private static int CompareTimelineItems(TimelineEntryState left, TimelineEntryState right)
    {
        if (left.Message is null && right.Message is not null)
        {
            return 1;
        }

        if (left.Message is not null && right.Message is null)
        {
            return -1;
        }

        if (left.Message is null && right.Message is null)
        {
            return DateTime.Compare(
                left.Pending?.CreatedAtUtc ?? DateTime.MinValue,
                right.Pending?.CreatedAtUtc ?? DateTime.MinValue
            );
        }

        return left.Message!.Id.CompareTo(right.Message!.Id);
    }
}

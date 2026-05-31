using Spx.Game.Application;
using Spx.Game.Application.Features.DeleteMessage;
using Spx.Game.Application.Features.EditMessage;
using Spx.Game.Application.Features.GetMessages;
using Spx.Game.Application.Features.GetMessageUpdates;
using Spx.Game.Application.Features.SendPrivateMessage;
using Spx.Game.Application.Features.SendPublicMessage;
using Spx.Web.Components.Lobby;

namespace Spx.Web.Components.Pages.Nexus;

internal sealed partial class NexusPageMessageCoordinator(
    IGetMessagesHandler getMessagesHandler,
    IGetMessageUpdatesHandler getMessageUpdatesHandler,
    ISendPublicMessageHandler sendPublicMessageHandler,
    ISendPrivateMessageHandler sendPrivateMessageHandler,
    IEditMessageHandler editMessageHandler,
    IDeleteMessageHandler deleteMessageHandler,
    ILogger<NexusPageMessageCoordinator> logger,
    NexusPageDataState data,
    NexusTimelineState timeline
)
{
    public async Task LoadInitialMessagesAsync(Guid gameId, Guid playerId)
    {
        if (data.Lobby is null)
        {
            return;
        }

        timeline.BeginInitialLoad();

        try
        {
            var page = await getMessagesHandler.HandleAsync(gameId, playerId);
            timeline.ApplyInitialPage(page);
        }
        catch (Exception exception)
        {
            LogLoadMessagesFailed(logger, exception, gameId, playerId);
            timeline.SetTimelineError("We couldn't load messages right now. Please try again.");
        }
        finally
        {
            timeline.CompleteInitialLoad();
        }
    }

    public async Task LoadMessageUpdatesAsync(
        Guid gameId,
        Guid playerId,
        ILobbyMessagesViewport? viewport
    )
    {
        if (data.Lobby is null)
        {
            return;
        }

        if (timeline.NewestMessageId is null)
        {
            await LoadInitialMessagesAsync(gameId, playerId);
            timeline.RequestScrollToBottom();
            return;
        }

        try
        {
            var shouldStickToBottom = viewport is null || await viewport.IsNearBottomAsync();
            var updates = await getMessageUpdatesHandler.HandleAsync(
                gameId,
                playerId,
                timeline.NewestMessageId
            );
            if (updates is null || updates.Count == 0)
            {
                return;
            }

            timeline.UpsertPersistedMessages(updates);
            if (shouldStickToBottom)
            {
                timeline.RequestScrollToBottom();
            }
        }
        catch (Exception exception)
        {
            LogLoadMessageUpdatesFailed(logger, exception, gameId, playerId);
            timeline.SetTimelineError("We couldn't refresh messages right now. Please try again.");
        }
    }

    public async Task LoadOlderMessagesAsync(
        Guid gameId,
        Guid playerId,
        ILobbyMessagesViewport? viewport
    )
    {
        if (
            data.Lobby is null
            || !timeline.HasOlderMessages
            || timeline.IsLoadingOlderMessages
            || timeline.OldestMessageId is null
        )
        {
            return;
        }

        timeline.BeginOlderLoad();

        try
        {
            var metrics = viewport is null
                ? new TimelineScrollMetrics()
                : await viewport.GetScrollMetricsAsync();
            var page = await getMessagesHandler.HandleAsync(
                gameId,
                playerId,
                timeline.OldestMessageId
            );
            timeline.ApplyOlderPage(page, metrics);
        }
        catch (Exception exception)
        {
            LogLoadOlderMessagesFailed(logger, exception, gameId, playerId);
            timeline.SetTimelineError(
                "We couldn't load older messages right now. Please try again."
            );
        }
        finally
        {
            timeline.CompleteOlderLoad();
        }
    }

    public async Task SendMessageAsync(
        Guid gameId,
        Guid playerId,
        LobbyMessageComposerSubmitRequest request
    )
    {
        var lobby = data.Lobby;
        if (lobby is null || !lobby.IsCurrentUserActive || timeline.IsSendingMessage)
        {
            return;
        }

        timeline.ClearComposerError();
        var body = request.Text.Trim();
        if (string.IsNullOrWhiteSpace(body))
        {
            timeline.SetComposerError("Messages cannot be empty.");
            return;
        }

        var recipient = GetSelectedRecipient(lobby, request.SelectedRecipientPlayerIdString);
        var pendingItem = timeline.AddPendingMessage(
            body,
            recipient?.PlayerId,
            recipient?.Name ?? string.Empty,
            recipient is not null
        );
        timeline.ClearComposer();
        timeline.BeginSend();

        try
        {
            GameMessageCommandOutcome result;
            if (recipient is null)
            {
                result = await sendPublicMessageHandler.HandleAsync(
                    gameId,
                    playerId,
                    new SendGameMessageRequest(body)
                );
            }
            else
            {
                result = await sendPrivateMessageHandler.HandleAsync(
                    gameId,
                    playerId,
                    recipient.PlayerId,
                    new SendGameMessageRequest(body)
                );
            }

            if (result is not GameMessageCommandSucceeded succeeded)
            {
                timeline.SetPendingMessageFailed(pendingItem.Key, true);
                timeline.SetComposerError(((GameMessageCommandFailed)result).ErrorMessage);
                return;
            }

            timeline.ReplacePendingWithPersistedMessage(pendingItem.Key, succeeded.Message);
        }
        catch (Exception exception)
        {
            timeline.SetPendingMessageFailed(pendingItem.Key, true);
            LogSendMessageFailed(logger, exception, gameId, playerId);
            timeline.SetComposerError("We couldn't send that message right now. Please try again.");
        }
        finally
        {
            timeline.CompleteSend();
        }
    }

    public async Task RetryPendingMessageAsync(Guid gameId, Guid playerId, Guid itemKey)
    {
        var lobby = data.Lobby;
        var item = timeline.FindPendingMessage(itemKey);
        if (item?.Pending is null || timeline.IsSendingMessage || lobby is null)
        {
            return;
        }

        timeline.SetPendingMessageFailed(itemKey, false);
        timeline.BeginSend();
        timeline.ClearComposerError();

        try
        {
            GameMessageCommandOutcome result;
            if (item.Pending.RecipientPlayerId is null)
            {
                result = await sendPublicMessageHandler.HandleAsync(
                    gameId,
                    playerId,
                    new SendGameMessageRequest(item.Pending.Body)
                );
            }
            else
            {
                result = await sendPrivateMessageHandler.HandleAsync(
                    gameId,
                    playerId,
                    item.Pending.RecipientPlayerId.Value,
                    new SendGameMessageRequest(item.Pending.Body)
                );
            }

            if (result is not GameMessageCommandSucceeded succeeded)
            {
                timeline.SetPendingMessageFailed(itemKey, true);
                timeline.SetComposerError(((GameMessageCommandFailed)result).ErrorMessage);
                return;
            }

            timeline.ReplacePendingWithPersistedMessage(itemKey, succeeded.Message);
        }
        catch (Exception exception)
        {
            timeline.SetPendingMessageFailed(itemKey, true);
            LogRetryMessageFailed(logger, exception, gameId, playerId);
            timeline.SetComposerError(
                "We couldn't resend that message right now. Please try again."
            );
        }
        finally
        {
            timeline.CompleteSend();
        }
    }

    public void DismissPendingMessage(Guid itemKey) => timeline.DismissPendingMessage(itemKey);

    public void BeginEdit(GameTimelineEntryView message) => timeline.BeginEdit(message);

    public void CancelEdit() => timeline.CancelEdit();

    public void SetEditMessageText(string text) => timeline.EditMessageText = text;

    public async Task SaveEditAsync(Guid gameId, Guid playerId)
    {
        var lobby = data.Lobby;
        if (lobby is null || timeline.EditingMessageId is null || timeline.IsSavingEdit)
        {
            return;
        }

        timeline.BeginSaveEdit();
        timeline.ClearTimelineError();

        try
        {
            var result = await editMessageHandler.HandleAsync(
                gameId,
                playerId,
                timeline.EditingMessageId.Value,
                new UpdateGameMessageRequest(timeline.EditMessageText)
            );
            if (result is not GameMessageCommandSucceeded succeeded)
            {
                timeline.SetTimelineError(((GameMessageCommandFailed)result).ErrorMessage);
                return;
            }

            timeline.UpsertPersistedMessages([succeeded.Message]);
            timeline.CancelEdit();
        }
        catch (Exception exception)
        {
            LogSaveEditFailed(logger, exception, gameId, playerId);
            timeline.SetTimelineError("We couldn't save that message right now. Please try again.");
        }
        finally
        {
            timeline.CompleteSaveEdit();
        }
    }

    public async Task DeleteMessageAsync(Guid gameId, Guid playerId, Guid messageId)
    {
        if (data.Lobby is null)
        {
            return;
        }

        timeline.ClearTimelineError();

        try
        {
            var result = await deleteMessageHandler.HandleAsync(gameId, playerId, messageId);
            if (result is not GameMessageCommandSucceeded succeeded)
            {
                timeline.SetTimelineError(((GameMessageCommandFailed)result).ErrorMessage);
                return;
            }

            timeline.UpsertPersistedMessages([succeeded.Message]);
            if (timeline.EditingMessageId == messageId)
            {
                timeline.CancelEdit();
            }
        }
        catch (Exception exception)
        {
            LogDeleteMessageFailed(logger, exception, gameId, playerId);
            timeline.SetTimelineError(
                "We couldn't delete that message right now. Please try again."
            );
        }
    }

    private static PendingRecipient? GetSelectedRecipient(
        GameLobbyView lobby,
        string? selectedRecipientPlayerIdString
    )
    {
        if (!Guid.TryParse(selectedRecipientPlayerIdString, out var recipientPlayerId))
        {
            return null;
        }

        var player = lobby.Players.SingleOrDefault(entry => entry.PlayerId == recipientPlayerId);
        return player is null ? null : new PendingRecipient(player.PlayerId, player.Name);
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to load initial messages for game {GameId} player {PlayerId}."
    )]
    private static partial void LogLoadMessagesFailed(
        ILogger logger,
        Exception exception,
        Guid gameId,
        Guid playerId
    );

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to load message updates for game {GameId} player {PlayerId}."
    )]
    private static partial void LogLoadMessageUpdatesFailed(
        ILogger logger,
        Exception exception,
        Guid gameId,
        Guid playerId
    );

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to load older messages for game {GameId} player {PlayerId}."
    )]
    private static partial void LogLoadOlderMessagesFailed(
        ILogger logger,
        Exception exception,
        Guid gameId,
        Guid playerId
    );

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to send a message for game {GameId} player {PlayerId}."
    )]
    private static partial void LogSendMessageFailed(
        ILogger logger,
        Exception exception,
        Guid gameId,
        Guid playerId
    );

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to retry a message for game {GameId} player {PlayerId}."
    )]
    private static partial void LogRetryMessageFailed(
        ILogger logger,
        Exception exception,
        Guid gameId,
        Guid playerId
    );

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to save a message edit for game {GameId} player {PlayerId}."
    )]
    private static partial void LogSaveEditFailed(
        ILogger logger,
        Exception exception,
        Guid gameId,
        Guid playerId
    );

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to delete a message for game {GameId} player {PlayerId}."
    )]
    private static partial void LogDeleteMessageFailed(
        ILogger logger,
        Exception exception,
        Guid gameId,
        Guid playerId
    );

    private sealed record PendingRecipient(Guid PlayerId, string Name);
}

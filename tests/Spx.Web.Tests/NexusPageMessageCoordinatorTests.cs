using Microsoft.Extensions.Logging.Abstractions;
using Spx.Game.Application;
using Spx.Game.Application.Features.DeleteMessage;
using Spx.Game.Application.Features.EditMessage;
using Spx.Game.Application.Features.GetMessages;
using Spx.Game.Application.Features.GetMessageUpdates;
using Spx.Game.Application.Features.SendPrivateMessage;
using Spx.Game.Application.Features.SendPublicMessage;
using Spx.Web.Components.Lobby;
using Spx.Web.Components.Pages.Nexus;
using Xunit;

namespace Spx.Web.Tests;

public sealed class NexusPageMessageCoordinatorTests
{
    // ── LoadInitialMessagesAsync ──────────────────────────────────────────

    [Fact]
    public async Task LoadInitialMessagesAsync_is_no_op_when_lobby_is_null()
    {
        var getHandler = new StubGetMessagesHandler
        {
            Result = new GameTimelinePageView([MakeMessage()], false),
        };
        var (coordinator, _, timeline) = CreateCoordinator(getMessages: getHandler);

        await coordinator.LoadInitialMessagesAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(0, getHandler.CallCount);
        Assert.Empty(timeline.Items);
    }

    [Fact]
    public async Task LoadInitialMessagesAsync_applies_page_and_completes_load()
    {
        var gameId = Guid.NewGuid();
        var message = MakeMessage();
        var (coordinator, _, timeline) = CreateCoordinator(
            lobby: MakeLobby(gameId),
            getMessages: new StubGetMessagesHandler
            {
                Result = new GameTimelinePageView([message], false),
            }
        );

        await coordinator.LoadInitialMessagesAsync(
            gameId,
            GamePageCoordinatorTestData.CurrentPlayerId
        );

        Assert.Single(timeline.Items);
        Assert.Equal(message.Id, timeline.Items[0].Message!.Id);
        Assert.False(timeline.IsTimelineLoading);
        Assert.Null(timeline.TimelineError);
    }

    [Fact]
    public async Task LoadInitialMessagesAsync_sets_timeline_error_and_completes_load_when_handler_throws()
    {
        var gameId = Guid.NewGuid();
        var (coordinator, _, timeline) = CreateCoordinator(
            lobby: MakeLobby(gameId),
            getMessages: new StubGetMessagesHandler
            {
                Exception = new InvalidOperationException("db down"),
            }
        );

        await coordinator.LoadInitialMessagesAsync(
            gameId,
            GamePageCoordinatorTestData.CurrentPlayerId
        );

        Assert.NotNull(timeline.TimelineError);
        Assert.False(timeline.IsTimelineLoading);
        Assert.Empty(timeline.Items);
    }

    // ── SendMessageAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task SendMessageAsync_is_no_op_when_lobby_is_null()
    {
        var sendHandler = new StubSendPublicMessageHandler
        {
            Result = new GameMessageCommandSucceeded(MakeMessage()),
        };
        var (coordinator, _, timeline) = CreateCoordinator(sendPublic: sendHandler);

        await coordinator.SendMessageAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new LobbyMessageComposerSubmitRequest("hello", null)
        );

        Assert.Equal(0, sendHandler.CallCount);
        Assert.Empty(timeline.Items);
    }

    [Fact]
    public async Task SendMessageAsync_sets_composer_error_when_body_is_whitespace()
    {
        var gameId = Guid.NewGuid();
        var (coordinator, _, timeline) = CreateCoordinator(lobby: MakeLobby(gameId));

        await coordinator.SendMessageAsync(
            gameId,
            GamePageCoordinatorTestData.CurrentPlayerId,
            new LobbyMessageComposerSubmitRequest("   ", null)
        );

        Assert.NotNull(timeline.ComposerError);
        Assert.Empty(timeline.Items);
    }

    [Fact]
    public async Task SendMessageAsync_replaces_pending_with_persisted_on_public_success()
    {
        var gameId = Guid.NewGuid();
        var persisted = MakeMessage();
        var (coordinator, _, timeline) = CreateCoordinator(
            lobby: MakeLobby(gameId),
            sendPublic: new StubSendPublicMessageHandler
            {
                Result = new GameMessageCommandSucceeded(persisted),
            }
        );

        await coordinator.SendMessageAsync(
            gameId,
            GamePageCoordinatorTestData.CurrentPlayerId,
            new LobbyMessageComposerSubmitRequest("hello", null)
        );

        var item = Assert.Single(timeline.Items);
        Assert.Equal(persisted.Id, item.Message!.Id);
        Assert.Null(item.Pending);
        Assert.Null(timeline.ComposerError);
        Assert.False(timeline.IsSendingMessage);
    }

    [Fact]
    public async Task SendMessageAsync_replaces_pending_with_persisted_on_private_success()
    {
        var gameId = Guid.NewGuid();
        var recipientId = GamePageCoordinatorTestData.OpponentPlayerId;
        var persisted = MakeMessage();
        var (coordinator, _, timeline) = CreateCoordinator(
            lobby: MakeLobby(gameId),
            sendPrivate: new StubSendPrivateMessageHandler
            {
                Result = new GameMessageCommandSucceeded(persisted),
            }
        );

        await coordinator.SendMessageAsync(
            gameId,
            GamePageCoordinatorTestData.CurrentPlayerId,
            new LobbyMessageComposerSubmitRequest("hello", recipientId.ToString())
        );

        var item = Assert.Single(timeline.Items);
        Assert.Equal(persisted.Id, item.Message!.Id);
        Assert.Null(timeline.ComposerError);
        Assert.False(timeline.IsSendingMessage);
    }

    [Fact]
    public async Task SendMessageAsync_marks_pending_failed_and_sets_composer_error_on_handler_failure()
    {
        var gameId = Guid.NewGuid();
        var (coordinator, _, timeline) = CreateCoordinator(
            lobby: MakeLobby(gameId),
            sendPublic: new StubSendPublicMessageHandler
            {
                Result = new GameMessageCommandFailed("Not allowed."),
            }
        );

        await coordinator.SendMessageAsync(
            gameId,
            GamePageCoordinatorTestData.CurrentPlayerId,
            new LobbyMessageComposerSubmitRequest("hello", null)
        );

        var item = Assert.Single(timeline.Items);
        Assert.True(item.Pending!.Failed);
        Assert.Equal("Not allowed.", timeline.ComposerError);
        Assert.False(timeline.IsSendingMessage);
    }

    [Fact]
    public async Task SendMessageAsync_marks_pending_failed_and_sets_composer_error_on_exception()
    {
        var gameId = Guid.NewGuid();
        var (coordinator, _, timeline) = CreateCoordinator(
            lobby: MakeLobby(gameId),
            sendPublic: new StubSendPublicMessageHandler
            {
                Exception = new InvalidOperationException("network error"),
            }
        );

        await coordinator.SendMessageAsync(
            gameId,
            GamePageCoordinatorTestData.CurrentPlayerId,
            new LobbyMessageComposerSubmitRequest("hello", null)
        );

        var item = Assert.Single(timeline.Items);
        Assert.True(item.Pending!.Failed);
        Assert.NotNull(timeline.ComposerError);
        Assert.False(timeline.IsSendingMessage);
    }

    // ── RetryPendingMessageAsync ──────────────────────────────────────────

    [Fact]
    public async Task RetryPendingMessageAsync_is_no_op_when_item_not_found()
    {
        var gameId = Guid.NewGuid();
        var sendHandler = new StubSendPublicMessageHandler
        {
            Result = new GameMessageCommandSucceeded(MakeMessage()),
        };
        var (coordinator, _, _) = CreateCoordinator(
            lobby: MakeLobby(gameId),
            sendPublic: sendHandler
        );

        await coordinator.RetryPendingMessageAsync(
            gameId,
            GamePageCoordinatorTestData.CurrentPlayerId,
            Guid.NewGuid()
        );

        Assert.Equal(0, sendHandler.CallCount);
    }

    [Fact]
    public async Task RetryPendingMessageAsync_replaces_pending_on_success()
    {
        var gameId = Guid.NewGuid();
        var persisted = MakeMessage();

        // seed a failed pending item by sending one that fails, then retry
        var (coordinator, _, timeline) = CreateCoordinator(
            lobby: MakeLobby(gameId),
            sendPublic: new StubSendPublicMessageHandler
            {
                Result = new GameMessageCommandFailed("temporary error"),
            }
        );
        await coordinator.SendMessageAsync(
            gameId,
            GamePageCoordinatorTestData.CurrentPlayerId,
            new LobbyMessageComposerSubmitRequest("hello", null)
        );
        var failedItem = Assert.Single(timeline.Items);
        Assert.True(failedItem.Pending!.Failed);

        // now retry with a succeeding handler
        var (coordinator2, _, timeline2) = CreateCoordinator(
            lobby: MakeLobby(gameId),
            sendPublic: new StubSendPublicMessageHandler
            {
                Result = new GameMessageCommandSucceeded(persisted),
            }
        );
        // pre-seed the same pending item into timeline2
        var pending = timeline2.AddPendingMessage("hello", null, string.Empty, false);
        timeline2.SetPendingMessageFailed(pending.Key, true);

        await coordinator2.RetryPendingMessageAsync(
            gameId,
            GamePageCoordinatorTestData.CurrentPlayerId,
            pending.Key
        );

        var item = Assert.Single(timeline2.Items);
        Assert.Equal(persisted.Id, item.Message!.Id);
        Assert.Null(timeline2.ComposerError);
        Assert.False(timeline2.IsSendingMessage);
    }

    [Fact]
    public async Task RetryPendingMessageAsync_sets_composer_error_on_handler_failure()
    {
        var gameId = Guid.NewGuid();
        var (coordinator, _, timeline) = CreateCoordinator(
            lobby: MakeLobby(gameId),
            sendPublic: new StubSendPublicMessageHandler
            {
                Result = new GameMessageCommandFailed("Still not allowed."),
            }
        );
        var pending = timeline.AddPendingMessage("hello", null, string.Empty, false);
        timeline.SetPendingMessageFailed(pending.Key, true);

        await coordinator.RetryPendingMessageAsync(
            gameId,
            GamePageCoordinatorTestData.CurrentPlayerId,
            pending.Key
        );

        Assert.True(pending.Pending!.Failed);
        Assert.Equal("Still not allowed.", timeline.ComposerError);
        Assert.False(timeline.IsSendingMessage);
    }

    // ── SaveEditAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task SaveEditAsync_is_no_op_when_not_editing()
    {
        var gameId = Guid.NewGuid();
        var editHandler = new StubEditMessageHandler
        {
            Result = new GameMessageCommandSucceeded(MakeMessage()),
        };
        var (coordinator, _, _) = CreateCoordinator(lobby: MakeLobby(gameId), edit: editHandler);

        await coordinator.SaveEditAsync(gameId, GamePageCoordinatorTestData.CurrentPlayerId);

        Assert.Equal(0, editHandler.CallCount);
    }

    [Fact]
    public async Task SaveEditAsync_upserts_message_and_cancels_edit_on_success()
    {
        var gameId = Guid.NewGuid();
        var original = MakeMessage();
        var updated = original with { Body = "updated body" };
        var (coordinator, _, timeline) = CreateCoordinator(
            lobby: MakeLobby(gameId),
            edit: new StubEditMessageHandler { Result = new GameMessageCommandSucceeded(updated) }
        );
        coordinator.BeginEdit(original);

        await coordinator.SaveEditAsync(gameId, GamePageCoordinatorTestData.CurrentPlayerId);

        var item = Assert.Single(timeline.Items);
        Assert.Equal("updated body", item.Message!.Body);
        Assert.Null(timeline.EditingMessageId);
        Assert.Null(timeline.TimelineError);
        Assert.False(timeline.IsSavingEdit);
    }

    [Fact]
    public async Task SaveEditAsync_sets_timeline_error_and_keeps_editing_on_handler_failure()
    {
        var gameId = Guid.NewGuid();
        var original = MakeMessage();
        var (coordinator, _, timeline) = CreateCoordinator(
            lobby: MakeLobby(gameId),
            edit: new StubEditMessageHandler
            {
                Result = new GameMessageCommandFailed("Edit rejected."),
            }
        );
        coordinator.BeginEdit(original);

        await coordinator.SaveEditAsync(gameId, GamePageCoordinatorTestData.CurrentPlayerId);

        Assert.Equal("Edit rejected.", timeline.TimelineError);
        Assert.Equal(original.Id, timeline.EditingMessageId);
        Assert.False(timeline.IsSavingEdit);
    }

    [Fact]
    public async Task SaveEditAsync_sets_timeline_error_on_exception()
    {
        var gameId = Guid.NewGuid();
        var original = MakeMessage();
        var (coordinator, _, timeline) = CreateCoordinator(
            lobby: MakeLobby(gameId),
            edit: new StubEditMessageHandler
            {
                Exception = new InvalidOperationException("network error"),
            }
        );
        coordinator.BeginEdit(original);

        await coordinator.SaveEditAsync(gameId, GamePageCoordinatorTestData.CurrentPlayerId);

        Assert.NotNull(timeline.TimelineError);
        Assert.False(timeline.IsSavingEdit);
    }

    // ── DeleteMessageAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteMessageAsync_is_no_op_when_lobby_is_null()
    {
        var deleteHandler = new StubDeleteMessageHandler
        {
            Result = new GameMessageCommandSucceeded(MakeMessage()),
        };
        var (coordinator, _, _) = CreateCoordinator(delete: deleteHandler);

        await coordinator.DeleteMessageAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(0, deleteHandler.CallCount);
    }

    [Fact]
    public async Task DeleteMessageAsync_upserts_result_message_on_success()
    {
        var gameId = Guid.NewGuid();
        var deleted = MakeMessage() with { DeletedAtUtc = DateTime.UtcNow };
        var (coordinator, _, timeline) = CreateCoordinator(
            lobby: MakeLobby(gameId),
            delete: new StubDeleteMessageHandler
            {
                Result = new GameMessageCommandSucceeded(deleted),
            }
        );

        await coordinator.DeleteMessageAsync(
            gameId,
            GamePageCoordinatorTestData.CurrentPlayerId,
            deleted.Id
        );

        var item = Assert.Single(timeline.Items);
        Assert.Equal(deleted.Id, item.Message!.Id);
        Assert.NotNull(item.Message.DeletedAtUtc);
        Assert.Null(timeline.TimelineError);
    }

    [Fact]
    public async Task DeleteMessageAsync_sets_timeline_error_on_handler_failure()
    {
        var gameId = Guid.NewGuid();
        var (coordinator, _, timeline) = CreateCoordinator(
            lobby: MakeLobby(gameId),
            delete: new StubDeleteMessageHandler
            {
                Result = new GameMessageCommandFailed("Cannot delete."),
            }
        );

        await coordinator.DeleteMessageAsync(
            gameId,
            GamePageCoordinatorTestData.CurrentPlayerId,
            Guid.NewGuid()
        );

        Assert.Equal("Cannot delete.", timeline.TimelineError);
    }

    [Fact]
    public async Task DeleteMessageAsync_cancels_edit_when_deleting_the_message_being_edited()
    {
        var gameId = Guid.NewGuid();
        var message = MakeMessage();
        var (coordinator, _, timeline) = CreateCoordinator(
            lobby: MakeLobby(gameId),
            delete: new StubDeleteMessageHandler
            {
                Result = new GameMessageCommandSucceeded(
                    message with
                    {
                        DeletedAtUtc = DateTime.UtcNow,
                    }
                ),
            }
        );
        coordinator.BeginEdit(message);
        Assert.Equal(message.Id, timeline.EditingMessageId);

        await coordinator.DeleteMessageAsync(
            gameId,
            GamePageCoordinatorTestData.CurrentPlayerId,
            message.Id
        );

        Assert.Null(timeline.EditingMessageId);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static (
        NexusPageMessageCoordinator Coordinator,
        NexusPageDataState Data,
        NexusTimelineState Timeline
    ) CreateCoordinator(
        GameLobbyView? lobby = null,
        StubGetMessagesHandler? getMessages = null,
        StubSendPublicMessageHandler? sendPublic = null,
        StubSendPrivateMessageHandler? sendPrivate = null,
        StubEditMessageHandler? edit = null,
        StubDeleteMessageHandler? delete = null
    )
    {
        var data = new NexusPageDataState();
        var timeline = new NexusTimelineState();

        if (lobby is not null)
        {
            data.ApplyPage(new GamePageView(lobby, null, GamePresenceView.Empty));
        }

        var coordinator = new NexusPageMessageCoordinator(
            getMessages ?? new StubGetMessagesHandler(),
            new StubGetMessageUpdatesHandler(),
            sendPublic
                ?? new StubSendPublicMessageHandler
                {
                    Result = new GameMessageCommandSucceeded(MakeMessage()),
                },
            sendPrivate
                ?? new StubSendPrivateMessageHandler
                {
                    Result = new GameMessageCommandSucceeded(MakeMessage()),
                },
            edit
                ?? new StubEditMessageHandler
                {
                    Result = new GameMessageCommandSucceeded(MakeMessage()),
                },
            delete
                ?? new StubDeleteMessageHandler
                {
                    Result = new GameMessageCommandSucceeded(MakeMessage()),
                },
            NullLogger<NexusPageMessageCoordinator>.Instance,
            data,
            timeline
        );

        return (coordinator, data, timeline);
    }

    private static GameLobbyView MakeLobby(Guid gameId) =>
        GamePageCoordinatorTestData.CreateLobby(gameId);

    private static GameTimelineEntryView MakeMessage(Guid? id = null) =>
        new(
            id ?? Guid.NewGuid(),
            GameMessageKind.PlayerPublic,
            GameMessageSenderKind.Player,
            GamePageCoordinatorTestData.CurrentPlayerId,
            "Captain Red",
            null,
            string.Empty,
            "Hello world",
            DateTime.UtcNow,
            null,
            null,
            true,
            false,
            true,
            true
        );

    // ── Stub handlers ──────────────────────────────────────────────────────

    private sealed class StubGetMessagesHandler : IGetMessagesHandler
    {
        public GameTimelinePageView? Result { get; init; }
        public Exception? Exception { get; init; }
        public int CallCount { get; private set; }

        public Task<GameTimelinePageView?> HandleAsync(
            Guid gameId,
            Guid playerId,
            Guid? beforeMessageId = default,
            int take = 0,
            CancellationToken cancellationToken = default
        )
        {
            CallCount++;
            return Exception is null
                ? Task.FromResult(Result)
                : Task.FromException<GameTimelinePageView?>(Exception);
        }
    }

    private sealed class StubGetMessageUpdatesHandler : IGetMessageUpdatesHandler
    {
        public Task<IReadOnlyList<GameTimelineEntryView>?> HandleAsync(
            Guid gameId,
            Guid playerId,
            Guid? afterMessageId,
            int take = 0,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<IReadOnlyList<GameTimelineEntryView>?>(null);
    }

    private sealed class StubSendPublicMessageHandler : ISendPublicMessageHandler
    {
        public GameMessageCommandOutcome Result { get; init; } =
            new GameMessageCommandFailed("No result configured.");

        public Exception? Exception { get; init; }
        public int CallCount { get; private set; }

        public Task<GameMessageCommandOutcome> HandleAsync(
            Guid gameId,
            Guid playerId,
            SendGameMessageRequest request,
            CancellationToken cancellationToken = default
        )
        {
            CallCount++;
            return Exception is null
                ? Task.FromResult(Result)
                : Task.FromException<GameMessageCommandOutcome>(Exception);
        }
    }

    private sealed class StubSendPrivateMessageHandler : ISendPrivateMessageHandler
    {
        public GameMessageCommandOutcome Result { get; init; } =
            new GameMessageCommandFailed("No result configured.");

        public Exception? Exception { get; init; }

        public Task<GameMessageCommandOutcome> HandleAsync(
            Guid gameId,
            Guid playerId,
            Guid recipientPlayerId,
            SendGameMessageRequest request,
            CancellationToken cancellationToken = default
        ) =>
            Exception is null
                ? Task.FromResult(Result)
                : Task.FromException<GameMessageCommandOutcome>(Exception);
    }

    private sealed class StubEditMessageHandler : IEditMessageHandler
    {
        public GameMessageCommandOutcome Result { get; init; } =
            new GameMessageCommandFailed("No result configured.");

        public Exception? Exception { get; init; }
        public int CallCount { get; private set; }

        public Task<GameMessageCommandOutcome> HandleAsync(
            Guid gameId,
            Guid playerId,
            Guid messageId,
            UpdateGameMessageRequest request,
            CancellationToken cancellationToken = default
        )
        {
            CallCount++;
            return Exception is null
                ? Task.FromResult(Result)
                : Task.FromException<GameMessageCommandOutcome>(Exception);
        }
    }

    private sealed class StubDeleteMessageHandler : IDeleteMessageHandler
    {
        public GameMessageCommandOutcome Result { get; init; } =
            new GameMessageCommandFailed("No result configured.");

        public Exception? Exception { get; init; }
        public int CallCount { get; private set; }

        public Task<GameMessageCommandOutcome> HandleAsync(
            Guid gameId,
            Guid playerId,
            Guid messageId,
            CancellationToken cancellationToken = default
        )
        {
            CallCount++;
            return Exception is null
                ? Task.FromResult(Result)
                : Task.FromException<GameMessageCommandOutcome>(Exception);
        }
    }
}

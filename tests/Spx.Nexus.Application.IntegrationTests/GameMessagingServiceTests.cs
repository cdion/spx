using Microsoft.EntityFrameworkCore;
using Spx.Data;
using Spx.Nexus.Application;
using Spx.Nexus.Domain;
using Xunit;

namespace Spx.Nexus.Application.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class GameMessagingServiceTests(PostgresDatabaseFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task SendPublicMessageAsync_CreatesMessageAndReturnsMappedView()
    {
        var database = Database;
        await database.AddUserAsync("user-1", "user1@example.com");
        var game = await database.AddGameAsync(
            "user-1",
            "ABC123",
            "Alpha",
            "user-1",
            "Captain Red"
        );
        var sender = await database.Context.GamePlayers.SingleAsync(entry =>
            entry.GameId == game.Id && entry.UserId == "user-1"
        );
        var publisher = new FakeGameMessagePublisher();
        var features = GameFeatureTestFactory.Create(
            database.ContextFactory,
            messagePublisher: publisher
        );

        var result = await features.SendPublicMessage.HandleAsync(
            game.Id,
            sender.Id,
            new SendGameMessageRequest("Hello crew")
        );

        var succeeded = Assert.IsType<GameMessageCommandSucceeded>(result);
        var message = await database.Context.GameMessages.SingleAsync();
        Assert.Equal(GameMessageKind.PlayerPublic, message.Kind);
        Assert.Equal("Hello crew", message.Body);
        Assert.Null(message.RecipientPlayerId);
        Assert.True(succeeded.Message.IsCurrentUserSender);
        Assert.True(succeeded.Message.CanEdit);
        Assert.True(succeeded.Message.CanDelete);
    }

    [Fact]
    public async Task SendPrivateMessageAsync_OnlySenderAndRecipientCanSeeIt()
    {
        var database = Database;
        await database.AddUserAsync("user-1", "user1@example.com");
        await database.AddUserAsync("user-2", "user2@example.com");
        await database.AddUserAsync("user-3", "user3@example.com");
        var game = await database.AddGameAsync(
            "user-1",
            "ABC123",
            "Alpha",
            "user-1",
            "Captain Red"
        );
        var recipient = await database.AddGamePlayerAsync(game.Id, "user-2", "Captain Blue");
        var thirdPlayer = await database.AddGamePlayerAsync(game.Id, "user-3", "Captain Green");
        var sender = await database.Context.GamePlayers.SingleAsync(entry =>
            entry.GameId == game.Id && entry.UserId == "user-1"
        );
        var publisher = new FakeGameMessagePublisher();
        var features = GameFeatureTestFactory.Create(
            database.ContextFactory,
            messagePublisher: publisher
        );

        var sendResult = await features.SendPrivateMessage.HandleAsync(
            game.Id,
            sender.Id,
            recipient.Id,
            new SendGameMessageRequest("Keep this private.")
        );

        Assert.IsType<GameMessageCommandSucceeded>(sendResult);

        var senderView = await features.GetMessages.HandleAsync(game.Id, sender.Id);
        var recipientView = await features.GetMessages.HandleAsync(game.Id, recipient.Id);
        var otherView = await features.GetMessages.HandleAsync(game.Id, thirdPlayer.Id);

        Assert.Single(senderView!.Items);
        Assert.Single(recipientView!.Items);
        Assert.Empty(otherView!.Items);
        Assert.True(senderView.Items[0].IsPrivate);
        Assert.Equal("Captain Blue", senderView.Items[0].RecipientDisplayName);
    }

    [Fact]
    public async Task SendPrivateMessageAsync_RejectsRecipientWhoIsNoLongerActive()
    {
        var database = Database;
        await database.AddUserAsync("user-1", "user1@example.com");
        await database.AddUserAsync("user-2", "user2@example.com");
        var game = await database.AddGameAsync(
            "user-1",
            "ABC123",
            "Alpha",
            "user-1",
            "Captain Red"
        );
        var recipient = await database.AddGamePlayerAsync(
            game.Id,
            "user-2",
            "Captain Blue",
            leftAtUtc: DateTime.UtcNow.AddMinutes(-1)
        );
        var sender = await database.Context.GamePlayers.SingleAsync(entry =>
            entry.GameId == game.Id && entry.UserId == "user-1"
        );
        var publisher = new FakeGameMessagePublisher();
        var features = GameFeatureTestFactory.Create(
            database.ContextFactory,
            messagePublisher: publisher
        );

        var result = await features.SendPrivateMessage.HandleAsync(
            game.Id,
            sender.Id,
            recipient.Id,
            new SendGameMessageRequest("Still there?")
        );

        var failed = Assert.IsType<GameMessageCommandFailed>(result);
        Assert.Equal("That recipient is not an active player in this game.", failed.ErrorMessage);
        Assert.Empty(await database.Context.GameMessages.ToListAsync());
    }

    [Fact]
    public async Task GetMessagesAsync_FormerPlayerStopsAtVisibilityCutoff()
    {
        var database = Database;
        await database.AddUserAsync("user-1", "user1@example.com");
        await database.AddUserAsync("user-2", "user2@example.com");
        var game = await database.AddGameAsync(
            "user-1",
            "ABC123",
            "Alpha",
            "user-1",
            "Captain Red"
        );
        var formerPlayer = await database.AddGamePlayerAsync(
            game.Id,
            "user-2",
            "Captain Blue",
            leftAtUtc: DateTime.UtcNow.AddMinutes(-1)
        );
        var hostPlayer = await database.Context.GamePlayers.SingleAsync(entry =>
            entry.GameId == game.Id && entry.UserId == "user-1"
        );
        var firstMessage = await database.AddMessageAsync(
            game.Id,
            GameMessageKind.PlayerPublic,
            GameMessageSenderKind.Player,
            hostPlayer.Id,
            null,
            hostPlayer.Name,
            string.Empty,
            "First",
            createdAtUtc: DateTime.UtcNow.AddMinutes(-5),
            id: Guid.Parse("019e0000-0000-7000-8000-000000000001")
        );
        var cutoffMessage = await database.AddMessageAsync(
            game.Id,
            GameMessageKind.PlayerPublic,
            GameMessageSenderKind.Player,
            hostPlayer.Id,
            null,
            hostPlayer.Name,
            string.Empty,
            "Second",
            createdAtUtc: DateTime.UtcNow.AddMinutes(-4),
            id: Guid.Parse("019e0000-0000-7000-8000-000000000002")
        );
        await database.AddMessageAsync(
            game.Id,
            GameMessageKind.PlayerPublic,
            GameMessageSenderKind.Player,
            hostPlayer.Id,
            null,
            hostPlayer.Name,
            string.Empty,
            "Third",
            createdAtUtc: DateTime.UtcNow.AddMinutes(-3),
            id: Guid.Parse("019e0000-0000-7000-8000-000000000003")
        );

        await database.SetVisibleThroughMessageIdAsync(formerPlayer.Id, cutoffMessage.Id);

        var features = GameFeatureTestFactory.Create(database.ContextFactory);

        var page = await features.GetMessages.HandleAsync(game.Id, formerPlayer.Id);

        Assert.Equal(2, page!.Items.Count);
        Assert.Contains(page.Items, entry => entry.Id == firstMessage.Id);
        Assert.Contains(page.Items, entry => entry.Id == cutoffMessage.Id);
    }

    [Fact]
    public async Task GetMessagesAsync_HonorsBeforeCursorAndHasMore()
    {
        var database = Database;
        await database.AddUserAsync("user-1", "user1@example.com");
        var game = await database.AddGameAsync(
            "user-1",
            "ABC123",
            "Alpha",
            "user-1",
            "Captain Red"
        );
        var sender = await database.Context.GamePlayers.SingleAsync(entry =>
            entry.GameId == game.Id && entry.UserId == "user-1"
        );
        await database.AddMessageAsync(
            game.Id,
            GameMessageKind.PlayerPublic,
            GameMessageSenderKind.Player,
            sender.Id,
            null,
            sender.Name,
            string.Empty,
            "First",
            id: Guid.Parse("019e0000-0000-7000-8000-000000000001")
        );
        var secondMessage = await database.AddMessageAsync(
            game.Id,
            GameMessageKind.PlayerPublic,
            GameMessageSenderKind.Player,
            sender.Id,
            null,
            sender.Name,
            string.Empty,
            "Second",
            id: Guid.Parse("019e0000-0000-7000-8000-000000000002")
        );
        var thirdMessage = await database.AddMessageAsync(
            game.Id,
            GameMessageKind.PlayerPublic,
            GameMessageSenderKind.Player,
            sender.Id,
            null,
            sender.Name,
            string.Empty,
            "Third",
            id: Guid.Parse("019e0000-0000-7000-8000-000000000003")
        );
        var features = GameFeatureTestFactory.Create(database.ContextFactory);

        var page = await features.GetMessages.HandleAsync(
            game.Id,
            sender.Id,
            beforeMessageId: thirdMessage.Id,
            take: 1
        );

        Assert.NotNull(page);
        Assert.True(page!.HasMore);
        Assert.Single(page.Items);
        Assert.Equal(secondMessage.Id, page.Items[0].Id);
    }

    [Fact]
    public async Task GetMessageUpdatesAsync_ReturnsMessagesAfterCursorInAscendingOrder()
    {
        var database = Database;
        await database.AddUserAsync("user-1", "user1@example.com");
        var game = await database.AddGameAsync(
            "user-1",
            "ABC123",
            "Alpha",
            "user-1",
            "Captain Red"
        );
        var sender = await database.Context.GamePlayers.SingleAsync(entry =>
            entry.GameId == game.Id && entry.UserId == "user-1"
        );
        var firstMessage = await database.AddMessageAsync(
            game.Id,
            GameMessageKind.PlayerPublic,
            GameMessageSenderKind.Player,
            sender.Id,
            null,
            sender.Name,
            string.Empty,
            "First",
            id: Guid.Parse("019e0000-0000-7000-8000-000000000011")
        );
        var secondMessage = await database.AddMessageAsync(
            game.Id,
            GameMessageKind.PlayerPublic,
            GameMessageSenderKind.Player,
            sender.Id,
            null,
            sender.Name,
            string.Empty,
            "Second",
            id: Guid.Parse("019e0000-0000-7000-8000-000000000012")
        );
        var thirdMessage = await database.AddMessageAsync(
            game.Id,
            GameMessageKind.PlayerPublic,
            GameMessageSenderKind.Player,
            sender.Id,
            null,
            sender.Name,
            string.Empty,
            "Third",
            id: Guid.Parse("019e0000-0000-7000-8000-000000000013")
        );
        var features = GameFeatureTestFactory.Create(database.ContextFactory);

        var updates = await features.GetMessageUpdates.HandleAsync(
            game.Id,
            sender.Id,
            firstMessage.Id,
            take: 10
        );

        Assert.NotNull(updates);
        Assert.Equal([secondMessage.Id, thirdMessage.Id], updates!.Select(entry => entry.Id));
    }

    [Fact]
    public async Task EditMessageAsync_RejectsExpiredMessage()
    {
        var database = Database;
        await database.AddUserAsync("user-1", "user1@example.com");
        var game = await database.AddGameAsync(
            "user-1",
            "ABC123",
            "Alpha",
            "user-1",
            "Captain Red"
        );
        var sender = await database.Context.GamePlayers.SingleAsync(entry =>
            entry.GameId == game.Id && entry.UserId == "user-1"
        );
        var message = await database.AddMessageAsync(
            game.Id,
            GameMessageKind.PlayerPublic,
            GameMessageSenderKind.Player,
            sender.Id,
            null,
            sender.Name,
            string.Empty,
            "Original",
            createdAtUtc: DateTime.UtcNow.AddMinutes(-3)
        );
        var features = GameFeatureTestFactory.Create(database.ContextFactory);

        var result = await features.EditMessage.HandleAsync(
            game.Id,
            sender.Id,
            message.Id,
            new UpdateGameMessageRequest("Updated")
        );

        var failed = Assert.IsType<GameMessageCommandFailed>(result);
        Assert.Equal("That message can no longer be edited.", failed.ErrorMessage);
    }

    [Fact]
    public async Task EditMessageAsync_RejectsDeletedMessage()
    {
        var database = Database;
        await database.AddUserAsync("user-1", "user1@example.com");
        var game = await database.AddGameAsync(
            "user-1",
            "ABC123",
            "Alpha",
            "user-1",
            "Captain Red"
        );
        var sender = await database.Context.GamePlayers.SingleAsync(entry =>
            entry.GameId == game.Id && entry.UserId == "user-1"
        );
        var message = await database.AddMessageAsync(
            game.Id,
            GameMessageKind.PlayerPublic,
            GameMessageSenderKind.Player,
            sender.Id,
            null,
            sender.Name,
            string.Empty,
            "Original",
            deletedAtUtc: DateTime.UtcNow.AddSeconds(-5)
        );
        var features = GameFeatureTestFactory.Create(database.ContextFactory);

        var result = await features.EditMessage.HandleAsync(
            game.Id,
            sender.Id,
            message.Id,
            new UpdateGameMessageRequest("Updated")
        );

        var failed = Assert.IsType<GameMessageCommandFailed>(result);
        Assert.Equal("Deleted messages cannot be edited.", failed.ErrorMessage);
    }

    [Fact]
    public async Task DeleteMessageAsync_SoftDeletesOwnedMessageWithinWindow()
    {
        var database = Database;
        await database.AddUserAsync("user-1", "user1@example.com");
        var game = await database.AddGameAsync(
            "user-1",
            "ABC123",
            "Alpha",
            "user-1",
            "Captain Red"
        );
        var sender = await database.Context.GamePlayers.SingleAsync(entry =>
            entry.GameId == game.Id && entry.UserId == "user-1"
        );
        var message = await database.AddMessageAsync(
            game.Id,
            GameMessageKind.PlayerPublic,
            GameMessageSenderKind.Player,
            sender.Id,
            null,
            sender.Name,
            string.Empty,
            "Original",
            createdAtUtc: DateTime.UtcNow.AddMinutes(-1)
        );
        var publisher = new FakeGameMessagePublisher();
        var features = GameFeatureTestFactory.Create(
            database.ContextFactory,
            messagePublisher: publisher
        );

        var result = await features.DeleteMessage.HandleAsync(game.Id, sender.Id, message.Id);

        var succeeded = Assert.IsType<GameMessageCommandSucceeded>(result);

        var updatedMessage = await database.Context.GameMessages.SingleAsync(entry =>
            entry.Id == message.Id
        );
        Assert.Equal(string.Empty, updatedMessage.Body);
        Assert.NotNull(updatedMessage.DeletedAtUtc);
        Assert.NotNull(succeeded.Message.DeletedAtUtc);
    }

    [Fact]
    public async Task DeleteMessageAsync_RejectsAlreadyDeletedMessage()
    {
        var database = Database;
        await database.AddUserAsync("user-1", "user1@example.com");
        var game = await database.AddGameAsync(
            "user-1",
            "ABC123",
            "Alpha",
            "user-1",
            "Captain Red"
        );
        var sender = await database.Context.GamePlayers.SingleAsync(entry =>
            entry.GameId == game.Id && entry.UserId == "user-1"
        );
        var message = await database.AddMessageAsync(
            game.Id,
            GameMessageKind.PlayerPublic,
            GameMessageSenderKind.Player,
            sender.Id,
            null,
            sender.Name,
            string.Empty,
            "Original",
            deletedAtUtc: DateTime.UtcNow.AddSeconds(-5)
        );
        var publisher = new FakeGameMessagePublisher();
        var features = GameFeatureTestFactory.Create(
            database.ContextFactory,
            messagePublisher: publisher
        );

        var result = await features.DeleteMessage.HandleAsync(game.Id, sender.Id, message.Id);

        var failed = Assert.IsType<GameMessageCommandFailed>(result);
        Assert.Equal("That message has already been deleted.", failed.ErrorMessage);
    }
}

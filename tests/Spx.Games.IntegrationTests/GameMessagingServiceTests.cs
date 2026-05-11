using Microsoft.EntityFrameworkCore;
using Spx.Data;
using Spx.Games;
using Xunit;

namespace Spx.Games.IntegrationTests;

public sealed class GameMessagingServiceTests
{
    [Fact]
    public async Task SendPublicMessageAsync_CreatesMessageAndPublishesEvent()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddUserAsync("user-1", "user1@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", "user-1", "Captain Red");
        var publisher = new FakeGameMessagePublisher();
        var service = new GameMessagingService(database.Context, publisher);

        var result = await service.SendPublicMessageAsync(game.Id, "user-1", new SendGameMessageRequest("Hello crew"));

        Assert.True(result.Succeeded);
        Assert.Equal([game.Id], publisher.PublishedGameIds);

        var message = await database.Context.GameMessages.SingleAsync();
        Assert.Equal(GameMessageKind.PlayerPublic, message.Kind);
        Assert.Equal("Hello crew", message.Body);
        Assert.Null(message.RecipientPlayerId);
        Assert.NotNull(result.Message);
        Assert.True(result.Message!.IsCurrentUserSender);
        Assert.True(result.Message.CanEdit);
        Assert.True(result.Message.CanDelete);
    }

    [Fact]
    public async Task SendPublicMessageAsync_RejectsWhitespaceOnlyMessage()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddUserAsync("user-1", "user1@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", "user-1", "Captain Red");
        var publisher = new FakeGameMessagePublisher();
        var service = new GameMessagingService(database.Context, publisher);

        var result = await service.SendPublicMessageAsync(game.Id, "user-1", new SendGameMessageRequest("  \r\n  "));

        Assert.False(result.Succeeded);
        Assert.Equal("Messages cannot be empty.", result.ErrorMessage);
        Assert.Empty(publisher.PublishedGameIds);
        Assert.Empty(await database.Context.GameMessages.ToListAsync());
    }

    [Fact]
    public async Task SendPrivateMessageAsync_OnlySenderAndRecipientCanSeeIt()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddUserAsync("user-1", "user1@example.com");
        await database.AddUserAsync("user-2", "user2@example.com");
        await database.AddUserAsync("user-3", "user3@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", "user-1", "Captain Red");
        var recipient = await database.AddGamePlayerAsync(game.Id, "user-2", "Captain Blue");
        await database.AddGamePlayerAsync(game.Id, "user-3", "Captain Green");
        var publisher = new FakeGameMessagePublisher();
        var service = new GameMessagingService(database.Context, publisher);

        var sendResult = await service.SendPrivateMessageAsync(game.Id, "user-1", recipient.Id, new SendGameMessageRequest("Keep this private."));

        Assert.True(sendResult.Succeeded);
        Assert.Equal([game.Id], publisher.PublishedGameIds);

        var senderView = await service.GetMessagesAsync(game.Id, "user-1");
        var recipientView = await service.GetMessagesAsync(game.Id, "user-2");
        var otherView = await service.GetMessagesAsync(game.Id, "user-3");

        Assert.Single(senderView!.Items);
        Assert.Single(recipientView!.Items);
        Assert.Empty(otherView!.Items);
        Assert.True(senderView.Items[0].IsPrivate);
        Assert.Equal("Captain Blue", senderView.Items[0].RecipientDisplayName);
    }

    [Fact]
    public async Task SendPrivateMessageAsync_RejectsRecipientWhoIsNoLongerActive()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddUserAsync("user-1", "user1@example.com");
        await database.AddUserAsync("user-2", "user2@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", "user-1", "Captain Red");
        var recipient = await database.AddGamePlayerAsync(game.Id, "user-2", "Captain Blue", leftAtUtc: DateTime.UtcNow.AddMinutes(-1));
        var publisher = new FakeGameMessagePublisher();
        var service = new GameMessagingService(database.Context, publisher);

        var result = await service.SendPrivateMessageAsync(game.Id, "user-1", recipient.Id, new SendGameMessageRequest("Still there?"));

        Assert.False(result.Succeeded);
        Assert.Equal("That recipient is not an active player in this game.", result.ErrorMessage);
        Assert.Empty(publisher.PublishedGameIds);
        Assert.Empty(await database.Context.GameMessages.ToListAsync());
    }

    [Fact]
    public async Task GetMessagesAsync_FormerPlayerStopsAtVisibilityCutoff()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddUserAsync("user-1", "user1@example.com");
        await database.AddUserAsync("user-2", "user2@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", "user-1", "Captain Red");
        var formerPlayer = await database.AddGamePlayerAsync(game.Id, "user-2", "Captain Blue", leftAtUtc: DateTime.UtcNow.AddMinutes(-1));
        var hostPlayer = await database.Context.GamePlayers.SingleAsync(entry => entry.GameId == game.Id && entry.UserId == "user-1");
        var firstMessage = await database.AddMessageAsync(game.Id, GameMessageKind.PlayerPublic, GameMessageSenderKind.Player, hostPlayer.Id, null, hostPlayer.Name, string.Empty, "First", createdAtUtc: DateTime.UtcNow.AddMinutes(-5), id: Guid.Parse("019e0000-0000-7000-8000-000000000001"));
        var cutoffMessage = await database.AddMessageAsync(game.Id, GameMessageKind.PlayerPublic, GameMessageSenderKind.Player, hostPlayer.Id, null, hostPlayer.Name, string.Empty, "Second", createdAtUtc: DateTime.UtcNow.AddMinutes(-4), id: Guid.Parse("019e0000-0000-7000-8000-000000000002"));
        await database.AddMessageAsync(game.Id, GameMessageKind.PlayerPublic, GameMessageSenderKind.Player, hostPlayer.Id, null, hostPlayer.Name, string.Empty, "Third", createdAtUtc: DateTime.UtcNow.AddMinutes(-3), id: Guid.Parse("019e0000-0000-7000-8000-000000000003"));

        formerPlayer.VisibleThroughMessageId = cutoffMessage.Id;
        await database.Context.SaveChangesAsync();

        var service = new GameMessagingService(database.Context, new FakeGameMessagePublisher());

        var page = await service.GetMessagesAsync(game.Id, "user-2");

        Assert.Equal(2, page!.Items.Count);
        Assert.Contains(page.Items, entry => entry.Id == firstMessage.Id);
        Assert.Contains(page.Items, entry => entry.Id == cutoffMessage.Id);
    }

    [Fact]
    public async Task GetMessagesAsync_HonorsBeforeCursorAndHasMore()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddUserAsync("user-1", "user1@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", "user-1", "Captain Red");
        var sender = await database.Context.GamePlayers.SingleAsync(entry => entry.GameId == game.Id && entry.UserId == "user-1");
        await database.AddMessageAsync(game.Id, GameMessageKind.PlayerPublic, GameMessageSenderKind.Player, sender.Id, null, sender.Name, string.Empty, "First", id: Guid.Parse("019e0000-0000-7000-8000-000000000001"));
        var secondMessage = await database.AddMessageAsync(game.Id, GameMessageKind.PlayerPublic, GameMessageSenderKind.Player, sender.Id, null, sender.Name, string.Empty, "Second", id: Guid.Parse("019e0000-0000-7000-8000-000000000002"));
        var thirdMessage = await database.AddMessageAsync(game.Id, GameMessageKind.PlayerPublic, GameMessageSenderKind.Player, sender.Id, null, sender.Name, string.Empty, "Third", id: Guid.Parse("019e0000-0000-7000-8000-000000000003"));
        var service = new GameMessagingService(database.Context, new FakeGameMessagePublisher());

        var page = await service.GetMessagesAsync(game.Id, "user-1", beforeMessageId: thirdMessage.Id, take: 1);

        Assert.NotNull(page);
        Assert.True(page!.HasMore);
        Assert.Single(page.Items);
        Assert.Equal(secondMessage.Id, page.Items[0].Id);
    }

    [Fact]
    public async Task GetMessageUpdatesAsync_ReturnsMessagesAfterCursorInAscendingOrder()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddUserAsync("user-1", "user1@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", "user-1", "Captain Red");
        var sender = await database.Context.GamePlayers.SingleAsync(entry => entry.GameId == game.Id && entry.UserId == "user-1");
        var firstMessage = await database.AddMessageAsync(game.Id, GameMessageKind.PlayerPublic, GameMessageSenderKind.Player, sender.Id, null, sender.Name, string.Empty, "First", id: Guid.Parse("019e0000-0000-7000-8000-000000000011"));
        var secondMessage = await database.AddMessageAsync(game.Id, GameMessageKind.PlayerPublic, GameMessageSenderKind.Player, sender.Id, null, sender.Name, string.Empty, "Second", id: Guid.Parse("019e0000-0000-7000-8000-000000000012"));
        var thirdMessage = await database.AddMessageAsync(game.Id, GameMessageKind.PlayerPublic, GameMessageSenderKind.Player, sender.Id, null, sender.Name, string.Empty, "Third", id: Guid.Parse("019e0000-0000-7000-8000-000000000013"));
        var service = new GameMessagingService(database.Context, new FakeGameMessagePublisher());

        var updates = await service.GetMessageUpdatesAsync(game.Id, "user-1", firstMessage.Id, take: 10);

        Assert.NotNull(updates);
        Assert.Equal([secondMessage.Id, thirdMessage.Id], updates!.Select(entry => entry.Id));
    }

    [Fact]
    public async Task EditMessageAsync_RejectsExpiredMessage()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddUserAsync("user-1", "user1@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", "user-1", "Captain Red");
        var sender = await database.Context.GamePlayers.SingleAsync(entry => entry.GameId == game.Id && entry.UserId == "user-1");
        var message = await database.AddMessageAsync(game.Id, GameMessageKind.PlayerPublic, GameMessageSenderKind.Player, sender.Id, null, sender.Name, string.Empty, "Original", createdAtUtc: DateTime.UtcNow.AddMinutes(-3));
        var service = new GameMessagingService(database.Context, new FakeGameMessagePublisher());

        var result = await service.EditMessageAsync(game.Id, "user-1", message.Id, new UpdateGameMessageRequest("Updated"));

        Assert.False(result.Succeeded);
        Assert.Equal("That message can no longer be edited.", result.ErrorMessage);
    }

    [Fact]
    public async Task EditMessageAsync_RejectsDeletedMessage()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddUserAsync("user-1", "user1@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", "user-1", "Captain Red");
        var sender = await database.Context.GamePlayers.SingleAsync(entry => entry.GameId == game.Id && entry.UserId == "user-1");
        var message = await database.AddMessageAsync(game.Id, GameMessageKind.PlayerPublic, GameMessageSenderKind.Player, sender.Id, null, sender.Name, string.Empty, "Original", deletedAtUtc: DateTime.UtcNow.AddSeconds(-5));
        var publisher = new FakeGameMessagePublisher();
        var service = new GameMessagingService(database.Context, publisher);

        var result = await service.EditMessageAsync(game.Id, "user-1", message.Id, new UpdateGameMessageRequest("Updated"));

        Assert.False(result.Succeeded);
        Assert.Equal("Deleted messages cannot be edited.", result.ErrorMessage);
        Assert.Empty(publisher.PublishedGameIds);
    }

    [Fact]
    public async Task DeleteMessageAsync_SoftDeletesOwnedMessageWithinWindow()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddUserAsync("user-1", "user1@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", "user-1", "Captain Red");
        var sender = await database.Context.GamePlayers.SingleAsync(entry => entry.GameId == game.Id && entry.UserId == "user-1");
        var message = await database.AddMessageAsync(game.Id, GameMessageKind.PlayerPublic, GameMessageSenderKind.Player, sender.Id, null, sender.Name, string.Empty, "Original", createdAtUtc: DateTime.UtcNow.AddMinutes(-1));
        var publisher = new FakeGameMessagePublisher();
        var service = new GameMessagingService(database.Context, publisher);

        var result = await service.DeleteMessageAsync(game.Id, "user-1", message.Id);

        Assert.True(result.Succeeded);
        Assert.Equal([game.Id], publisher.PublishedGameIds);

        var updatedMessage = await database.Context.GameMessages.SingleAsync(entry => entry.Id == message.Id);
        Assert.Equal(string.Empty, updatedMessage.Body);
        Assert.NotNull(updatedMessage.DeletedAtUtc);
        Assert.NotNull(result.Message!.DeletedAtUtc);
    }

    [Fact]
    public async Task DeleteMessageAsync_RejectsAlreadyDeletedMessage()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.AddUserAsync("user-1", "user1@example.com");
        var game = await database.AddGameAsync("user-1", "ABC123", "Alpha", "user-1", "Captain Red");
        var sender = await database.Context.GamePlayers.SingleAsync(entry => entry.GameId == game.Id && entry.UserId == "user-1");
        var message = await database.AddMessageAsync(game.Id, GameMessageKind.PlayerPublic, GameMessageSenderKind.Player, sender.Id, null, sender.Name, string.Empty, "Original", deletedAtUtc: DateTime.UtcNow.AddSeconds(-5));
        var publisher = new FakeGameMessagePublisher();
        var service = new GameMessagingService(database.Context, publisher);

        var result = await service.DeleteMessageAsync(game.Id, "user-1", message.Id);

        Assert.False(result.Succeeded);
        Assert.Equal("That message has already been deleted.", result.ErrorMessage);
        Assert.Empty(publisher.PublishedGameIds);
    }
}
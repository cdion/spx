using Microsoft.Extensions.DependencyInjection;
using Spx.Games;
using Spx.Games.Features.EditMessage;
using Xunit;

namespace Spx.Games.Tests;

public sealed class EditMessageHandlerTests
{
    [Fact]
    public async Task HandleAsync_rejects_whitespace_only_messages_without_hitting_persistence()
    {
        var persistence = new FakeGameMessagePersistence();
        var publisher = new FakeGameMessageEventsPublisher();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<IEditMessageHandler>();
        var result = await handler.HandleAsync(Guid.NewGuid(), "user-1", Guid.NewGuid(), new UpdateGameMessageRequest("  \r\n  "));

        Assert.False(result.Succeeded);
        Assert.Equal("Messages cannot be empty.", result.ErrorMessage);
        Assert.Equal(0, persistence.EditMessageCallCount);
        Assert.Empty(publisher.PublishedGameIds);
    }

    [Fact]
    public async Task HandleAsync_publishes_messages_changed_when_persistence_succeeds()
    {
        var gameId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var persistedMessage = new GameMessageView(
            messageId,
            GameMessageKind.PlayerPublic,
            GameMessageSenderKind.Player,
            Guid.NewGuid(),
            "Captain Red",
            null,
            string.Empty,
            "Updated",
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow,
            null,
            true,
            false,
            true,
            true);
        var persistence = new FakeGameMessagePersistence
        {
            EditMessageResult = GameMessageCommandResult.Success(persistedMessage)
        };
        var publisher = new FakeGameMessageEventsPublisher();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<IEditMessageHandler>();
        var result = await handler.HandleAsync(gameId, "user-1", messageId, new UpdateGameMessageRequest(" Updated "));

        Assert.True(result.Succeeded);
        Assert.Equal(messageId, persistence.LastEditedMessageId);
        Assert.Equal("Updated", persistence.LastEditBody);
        Assert.Equal([gameId], publisher.PublishedGameIds);
    }

    [Fact]
    public async Task HandleAsync_does_not_publish_when_persistence_fails()
    {
        var persistence = new FakeGameMessagePersistence
        {
            EditMessageResult = GameMessageCommandResult.Failure("Deleted messages cannot be edited.")
        };
        var publisher = new FakeGameMessageEventsPublisher();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<IEditMessageHandler>();
        var result = await handler.HandleAsync(Guid.NewGuid(), "user-1", Guid.NewGuid(), new UpdateGameMessageRequest("Updated"));

        Assert.False(result.Succeeded);
        Assert.Equal("Deleted messages cannot be edited.", result.ErrorMessage);
        Assert.Empty(publisher.PublishedGameIds);
    }
}
using Microsoft.Extensions.DependencyInjection;
using Spx.Games;
using Spx.Games.Features.DeleteMessage;
using Xunit;

namespace Spx.Games.Tests;

public sealed class DeleteMessageHandlerTests
{
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
            string.Empty,
            DateTime.UtcNow.AddMinutes(-1),
            null,
            DateTime.UtcNow,
            true,
            false,
            false,
            false);
        var persistence = new FakeGameMessagePersistence
        {
            DeleteMessageResult = GameMessageCommandResult.Success(persistedMessage)
        };
        var publisher = new FakeGameMessageEventsPublisher();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<IDeleteMessageHandler>();
        var result = await handler.HandleAsync(gameId, "user-1", messageId);

        Assert.True(result.Succeeded);
        Assert.Equal(messageId, persistence.LastDeletedMessageId);
        Assert.Equal([gameId], publisher.PublishedGameIds);
    }

    [Fact]
    public async Task HandleAsync_does_not_publish_when_persistence_fails()
    {
        var persistence = new FakeGameMessagePersistence
        {
            DeleteMessageResult = GameMessageCommandResult.Failure("That message has already been deleted.")
        };
        var publisher = new FakeGameMessageEventsPublisher();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<IDeleteMessageHandler>();
        var result = await handler.HandleAsync(Guid.NewGuid(), "user-1", Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal("That message has already been deleted.", result.ErrorMessage);
        Assert.Empty(publisher.PublishedGameIds);
    }
}
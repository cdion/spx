using Microsoft.Extensions.DependencyInjection;
using Spx.Games;
using Spx.Games.Features.SendPrivateMessage;
using Xunit;

namespace Spx.Games.Tests;

public sealed class SendPrivateMessageHandlerTests
{
    [Fact]
    public async Task HandleAsync_rejects_whitespace_only_messages_without_hitting_persistence()
    {
        var persistence = new FakeGameMessagePersistence();
        var publisher = new FakeGameMessageEventsPublisher();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<ISendPrivateMessageHandler>();
        var result = await handler.HandleAsync(Guid.NewGuid(), "user-1", Guid.NewGuid(), new SendGameMessageRequest("  \r\n  "));

        Assert.False(result.Succeeded);
        Assert.Equal("Messages cannot be empty.", result.ErrorMessage);
        Assert.Equal(0, persistence.SendPrivateMessageCallCount);
        Assert.Empty(publisher.PublishedGameIds);
    }

    [Fact]
    public async Task HandleAsync_publishes_messages_changed_when_persistence_succeeds()
    {
        var gameId = Guid.NewGuid();
        var recipientPlayerId = Guid.NewGuid();
        var persistedMessage = new GameTimelineEntryView(
            Guid.NewGuid(),
            GameMessageKind.PlayerPrivate,
            GameMessageSenderKind.Player,
            Guid.NewGuid(),
            "Captain Red",
            recipientPlayerId,
            "Captain Blue",
            "Keep this private.",
            DateTime.UtcNow,
            null,
            null,
            true,
            true,
            true,
            true);
        var persistence = new FakeGameMessagePersistence
        {
            SendPrivateMessageResult = GameMessageCommandResult.Success(persistedMessage)
        };
        var publisher = new FakeGameMessageEventsPublisher();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<ISendPrivateMessageHandler>();
        var result = await handler.HandleAsync(gameId, "user-1", recipientPlayerId, new SendGameMessageRequest(" Keep this private. "));

        Assert.True(result.Succeeded);
        Assert.Equal(recipientPlayerId, persistence.LastRecipientPlayerId);
        Assert.Equal("Keep this private.", persistence.LastPrivateBody);
        Assert.Equal([gameId], publisher.PublishedGameIds);
    }

    [Fact]
    public async Task HandleAsync_does_not_publish_when_persistence_fails()
    {
        var persistence = new FakeGameMessagePersistence
        {
            SendPrivateMessageResult = GameMessageCommandResult.Failure("That recipient is not an active player in this game.")
        };
        var publisher = new FakeGameMessageEventsPublisher();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<ISendPrivateMessageHandler>();
        var result = await handler.HandleAsync(Guid.NewGuid(), "user-1", Guid.NewGuid(), new SendGameMessageRequest("Still there?"));

        Assert.False(result.Succeeded);
        Assert.Equal("That recipient is not an active player in this game.", result.ErrorMessage);
        Assert.Empty(publisher.PublishedGameIds);
    }
}
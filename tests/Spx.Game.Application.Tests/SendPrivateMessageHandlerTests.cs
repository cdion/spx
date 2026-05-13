using Microsoft.Extensions.DependencyInjection;
using Spx.Game.Application;
using Spx.Game.Application.Features.SendPrivateMessage;
using Xunit;

namespace Spx.Game.Application.Tests;

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

        var failed = Assert.IsType<GameMessageCommandFailed>(result);
        Assert.Equal("Messages cannot be empty.", failed.ErrorMessage);
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
            SendPrivateMessageResult = new GameMessageCommandSucceeded(persistedMessage)
        };
        var publisher = new FakeGameMessageEventsPublisher();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<ISendPrivateMessageHandler>();
        var result = await handler.HandleAsync(gameId, "user-1", recipientPlayerId, new SendGameMessageRequest(" Keep this private. "));

        Assert.IsType<GameMessageCommandSucceeded>(result);
        Assert.Equal(recipientPlayerId, persistence.LastRecipientPlayerId);
        Assert.Equal("Keep this private.", persistence.LastPrivateBody);
        Assert.Equal([gameId], publisher.PublishedGameIds);
    }

    [Fact]
    public async Task HandleAsync_does_not_publish_when_persistence_fails()
    {
        var persistence = new FakeGameMessagePersistence
        {
            SendPrivateMessageResult = new GameMessageCommandFailed("That recipient is not an active player in this game.")
        };
        var publisher = new FakeGameMessageEventsPublisher();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<ISendPrivateMessageHandler>();
        var result = await handler.HandleAsync(Guid.NewGuid(), "user-1", Guid.NewGuid(), new SendGameMessageRequest("Still there?"));

        var failed = Assert.IsType<GameMessageCommandFailed>(result);
        Assert.Equal("That recipient is not an active player in this game.", failed.ErrorMessage);
        Assert.Empty(publisher.PublishedGameIds);
    }
}
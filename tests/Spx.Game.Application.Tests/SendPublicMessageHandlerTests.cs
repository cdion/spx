using Microsoft.Extensions.DependencyInjection;
using Spx.Game.Application;
using Spx.Game.Application.Features.SendPublicMessage;
using Xunit;

namespace Spx.Game.Application.Tests;

public sealed class SendPublicMessageHandlerTests
{
    [Fact]
    public async Task HandleAsync_rejects_whitespace_only_messages_without_hitting_persistence()
    {
        var persistence = new FakeGameMessagePersistence();
        var publisher = new FakeGameMessageEventsPublisher();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<ISendPublicMessageHandler>();
        var result = await handler.HandleAsync(Guid.NewGuid(), "user-1", new SendGameMessageRequest("  \r\n  "));

        var failed = Assert.IsType<GameMessageCommandFailed>(result);
        Assert.Equal("Messages cannot be empty.", failed.ErrorMessage);
        Assert.Equal(0, persistence.SendPublicMessageCallCount);
        Assert.Empty(publisher.PublishedGameIds);
    }

    [Fact]
    public async Task HandleAsync_publishes_messages_changed_when_persistence_succeeds()
    {
        var gameId = Guid.NewGuid();
        var persistedMessage = new GameTimelineEntryView(
            Guid.NewGuid(),
            GameMessageKind.PlayerPublic,
            GameMessageSenderKind.Player,
            Guid.NewGuid(),
            "Captain Red",
            null,
            string.Empty,
            "Hello crew",
            DateTime.UtcNow,
            null,
            null,
            true,
            false,
            true,
            true);
        var persistence = new FakeGameMessagePersistence
        {
            SendPublicMessageResult = new GameMessageCommandSucceeded(persistedMessage)
        };
        var publisher = new FakeGameMessageEventsPublisher();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<ISendPublicMessageHandler>();
        var result = await handler.HandleAsync(gameId, "user-1", new SendGameMessageRequest(" Hello crew "));

        Assert.IsType<GameMessageCommandSucceeded>(result);
        Assert.Equal("Hello crew", persistence.LastPublicBody);
        Assert.Equal([gameId], publisher.PublishedGameIds);
    }

    [Fact]
    public async Task HandleAsync_does_not_publish_when_persistence_fails()
    {
        var persistence = new FakeGameMessagePersistence
        {
            SendPublicMessageResult = new GameMessageCommandFailed("That player is not active in this game.")
        };
        var publisher = new FakeGameMessageEventsPublisher();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<ISendPublicMessageHandler>();
        var result = await handler.HandleAsync(Guid.NewGuid(), "user-1", new SendGameMessageRequest("Hello crew"));

        var failed = Assert.IsType<GameMessageCommandFailed>(result);
        Assert.Equal("That player is not active in this game.", failed.ErrorMessage);
        Assert.Empty(publisher.PublishedGameIds);
    }
}
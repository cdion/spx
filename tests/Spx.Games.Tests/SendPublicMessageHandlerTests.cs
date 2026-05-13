using Microsoft.Extensions.DependencyInjection;
using Spx.Games;
using Spx.Games.Features.SendPublicMessage;
using Xunit;

namespace Spx.Games.Tests;

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

        Assert.False(result.Succeeded);
        Assert.Equal("Messages cannot be empty.", result.ErrorMessage);
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
            SendPublicMessageResult = GameMessageCommandResult.Success(persistedMessage)
        };
        var publisher = new FakeGameMessageEventsPublisher();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<ISendPublicMessageHandler>();
        var result = await handler.HandleAsync(gameId, "user-1", new SendGameMessageRequest(" Hello crew "));

        Assert.True(result.Succeeded);
        Assert.Equal("Hello crew", persistence.LastPublicBody);
        Assert.Equal([gameId], publisher.PublishedGameIds);
    }

    [Fact]
    public async Task HandleAsync_does_not_publish_when_persistence_fails()
    {
        var persistence = new FakeGameMessagePersistence
        {
            SendPublicMessageResult = GameMessageCommandResult.Failure("That player is not active in this game.")
        };
        var publisher = new FakeGameMessageEventsPublisher();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<ISendPublicMessageHandler>();
        var result = await handler.HandleAsync(Guid.NewGuid(), "user-1", new SendGameMessageRequest("Hello crew"));

        Assert.False(result.Succeeded);
        Assert.Equal("That player is not active in this game.", result.ErrorMessage);
        Assert.Empty(publisher.PublishedGameIds);
    }
}
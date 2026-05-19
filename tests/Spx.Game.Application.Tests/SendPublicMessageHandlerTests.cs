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
        var persistence = Substitute.For<IGameMessagePersistence>();
        var publisher = Substitute.For<IGameMessageInvalidationPublisher>();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<ISendPublicMessageHandler>();
        var result = await handler.HandleAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new SendGameMessageRequest("  \r\n  ")
        );

        var failed = Assert.IsType<GameMessageCommandFailed>(result);
        Assert.Equal("Messages cannot be empty.", failed.ErrorMessage);
        await persistence
            .DidNotReceive()
            .SendPublicMessageAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );
        await publisher
            .DidNotReceive()
            .PublishMessagesInvalidatedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
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
            true
        );
        var persistence = Substitute.For<IGameMessagePersistence>();
        persistence
            .SendPublicMessageAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new GameMessageCommandSucceeded(persistedMessage));
        var publisher = Substitute.For<IGameMessageInvalidationPublisher>();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<ISendPublicMessageHandler>();
        var result = await handler.HandleAsync(
            gameId,
            Guid.NewGuid(),
            new SendGameMessageRequest(" Hello crew ")
        );

        Assert.IsType<GameMessageCommandSucceeded>(result);
        await persistence
            .Received(1)
            .SendPublicMessageAsync(
                gameId,
                Arg.Any<Guid>(),
                "Hello crew",
                Arg.Any<CancellationToken>()
            );
        await publisher
            .Received(1)
            .PublishMessagesInvalidatedAsync(gameId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_does_not_publish_when_persistence_fails()
    {
        var persistence = Substitute.For<IGameMessagePersistence>();
        persistence
            .SendPublicMessageAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new GameMessageCommandFailed("That player is not active in this game."));
        var publisher = Substitute.For<IGameMessageInvalidationPublisher>();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<ISendPublicMessageHandler>();
        var result = await handler.HandleAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new SendGameMessageRequest("Hello crew")
        );

        var failed = Assert.IsType<GameMessageCommandFailed>(result);
        Assert.Equal("That player is not active in this game.", failed.ErrorMessage);
        await publisher
            .DidNotReceive()
            .PublishMessagesInvalidatedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}

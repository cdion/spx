using Microsoft.Extensions.DependencyInjection;
using Spx.Game.Application;
using Spx.Game.Application.Features.EditMessage;
using Xunit;

namespace Spx.Game.Application.Tests;

public sealed class EditMessageHandlerTests
{
    [Fact]
    public async Task HandleAsync_rejects_whitespace_only_messages_without_hitting_persistence()
    {
        var persistence = Substitute.For<IGameMessagePersistence>();
        var publisher = Substitute.For<IGameMessageInvalidationPublisher>();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<IEditMessageHandler>();
        var result = await handler.HandleAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new UpdateGameMessageRequest("  \r\n  ")
        );

        var failed = Assert.IsType<GameMessageCommandFailed>(result);
        Assert.Equal("Messages cannot be empty.", failed.ErrorMessage);
        await persistence
            .DidNotReceive()
            .EditMessageAsync(
                Arg.Any<Guid>(),
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
        var messageId = Guid.NewGuid();
        var persistedMessage = new GameTimelineEntryView(
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
            true
        );
        var persistence = Substitute.For<IGameMessagePersistence>();
        persistence
            .EditMessageAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new GameMessageCommandSucceeded(persistedMessage));
        var publisher = Substitute.For<IGameMessageInvalidationPublisher>();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<IEditMessageHandler>();
        var result = await handler.HandleAsync(
            gameId,
            Guid.NewGuid(),
            messageId,
            new UpdateGameMessageRequest(" Updated ")
        );

        Assert.IsType<GameMessageCommandSucceeded>(result);
        await persistence
            .Received(1)
            .EditMessageAsync(
                gameId,
                Arg.Any<Guid>(),
                messageId,
                "Updated",
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
            .EditMessageAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new GameMessageCommandFailed("Deleted messages cannot be edited."));
        var publisher = Substitute.For<IGameMessageInvalidationPublisher>();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<IEditMessageHandler>();
        var result = await handler.HandleAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new UpdateGameMessageRequest("Updated")
        );

        var failed = Assert.IsType<GameMessageCommandFailed>(result);
        Assert.Equal("Deleted messages cannot be edited.", failed.ErrorMessage);
        await publisher
            .DidNotReceive()
            .PublishMessagesInvalidatedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}

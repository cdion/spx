using Microsoft.Extensions.DependencyInjection;
using Spx.Nexus.Application;
using Spx.Nexus.Application.Features.DeleteMessage;
using Xunit;

namespace Spx.Nexus.Application.Tests;

public sealed class DeleteMessageHandlerTests
{
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
            string.Empty,
            DateTime.UtcNow.AddMinutes(-1),
            null,
            DateTime.UtcNow,
            true,
            false,
            false,
            false
        );
        var persistence = Substitute.For<IGameMessagePersistence>();
        persistence
            .DeleteMessageAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new GameMessageCommandSucceeded(persistedMessage));
        var publisher = Substitute.For<IGameMessageInvalidationPublisher>();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<IDeleteMessageHandler>();
        var result = await handler.HandleAsync(gameId, Guid.NewGuid(), messageId);

        Assert.IsType<GameMessageCommandSucceeded>(result);
        await persistence
            .Received(1)
            .DeleteMessageAsync(gameId, Arg.Any<Guid>(), messageId, Arg.Any<CancellationToken>());
        await publisher
            .Received(1)
            .PublishMessagesInvalidatedAsync(gameId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_does_not_publish_when_persistence_fails()
    {
        var persistence = Substitute.For<IGameMessagePersistence>();
        persistence
            .DeleteMessageAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new GameMessageCommandFailed("That message has already been deleted."));
        var publisher = Substitute.For<IGameMessageInvalidationPublisher>();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<IDeleteMessageHandler>();
        var result = await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var failed = Assert.IsType<GameMessageCommandFailed>(result);
        Assert.Equal("That message has already been deleted.", failed.ErrorMessage);
        await publisher
            .DidNotReceive()
            .PublishMessagesInvalidatedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}

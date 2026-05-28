using Microsoft.Extensions.DependencyInjection;
using Spx.Nexus.Application;
using Spx.Nexus.Application.Features.SendPrivateMessage;
using Xunit;

namespace Spx.Nexus.Application.Tests;

public sealed class SendPrivateMessageHandlerTests
{
    [Fact]
    public async Task HandleAsync_rejects_whitespace_only_messages_without_hitting_persistence()
    {
        var persistence = Substitute.For<IGameMessagePersistence>();
        var publisher = Substitute.For<IGameMessageInvalidationPublisher>();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<ISendPrivateMessageHandler>();
        var result = await handler.HandleAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new SendGameMessageRequest("  \r\n  ")
        );

        var failed = Assert.IsType<GameMessageCommandFailed>(result);
        Assert.Equal("Messages cannot be empty.", failed.ErrorMessage);
        await persistence
            .DidNotReceive()
            .SendPrivateMessageAsync(
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
            true
        );
        var persistence = Substitute.For<IGameMessagePersistence>();
        persistence
            .SendPrivateMessageAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new GameMessageCommandSucceeded(persistedMessage));
        var publisher = Substitute.For<IGameMessageInvalidationPublisher>();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<ISendPrivateMessageHandler>();
        var result = await handler.HandleAsync(
            gameId,
            Guid.NewGuid(),
            recipientPlayerId,
            new SendGameMessageRequest(" Keep this private. ")
        );

        Assert.IsType<GameMessageCommandSucceeded>(result);
        await persistence
            .Received(1)
            .SendPrivateMessageAsync(
                gameId,
                Arg.Any<Guid>(),
                recipientPlayerId,
                "Keep this private.",
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
            .SendPrivateMessageAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new GameMessageCommandFailed("That recipient is not an active player in this game.")
            );
        var publisher = Substitute.For<IGameMessageInvalidationPublisher>();
        using var services = GameMessageHandlerTestServices.Create(persistence, publisher);

        var handler = services.GetRequiredService<ISendPrivateMessageHandler>();
        var result = await handler.HandleAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new SendGameMessageRequest("Still there?")
        );

        var failed = Assert.IsType<GameMessageCommandFailed>(result);
        Assert.Equal("That recipient is not an active player in this game.", failed.ErrorMessage);
        await publisher
            .DidNotReceive()
            .PublishMessagesInvalidatedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}

using System.Collections.Immutable;
using Spx.Game.Application;
using Spx.Game.Application.Features.SubmitOrders;
using Xunit;

namespace Spx.Game.Application.Tests;

public sealed class SubmitOrdersHandlerTests
{
    private static readonly Guid GameId = Guid.NewGuid();
    private static readonly Guid RedPlayerId = Guid.NewGuid();
    private static readonly Guid BluePlayerId = Guid.NewGuid();

    [Fact]
    public async Task HandleAsync_returns_failed_outcome_when_session_service_rejects()
    {
        var sessionService = Substitute.For<IGameSessionService>();
        sessionService
            .SubmitOrdersAsync(
                Arg.Any<Guid>(),
                Arg.Any<NexusTurnOrdersCommand>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new GameSessionCommandFailed("rejected"));

        var persistence = Substitute.For<IGamePersistence>();
        var messagesPersistence = Substitute.For<IGameMessagePersistence>();
        var sessionInvalidationPublisher = Substitute.For<IGameSessionInvalidationPublisher>();
        var messageInvalidationPublisher = Substitute.For<IGameMessageInvalidationPublisher>();

        var handler = new SubmitOrdersHandler(
            sessionService,
            sessionInvalidationPublisher,
            messageInvalidationPublisher,
            persistence,
            messagesPersistence
        );
        var command = new NexusTurnOrdersCommand(RedPlayerId, 1, [], [], false);
        var result = await handler.HandleAsync(GameId, command);

        Assert.IsType<GameSessionCommandFailed>(result);
        await messagesPersistence
            .DidNotReceive()
            .WriteGameplayEventsAsync(
                Arg.Any<Guid>(),
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleAsync_does_not_write_events_when_resolve_events_empty()
    {
        var session = CreateSession(resolveEvents: []);

        var sessionService = Substitute.For<IGameSessionService>();
        sessionService
            .SubmitOrdersAsync(
                Arg.Any<Guid>(),
                Arg.Any<NexusTurnOrdersCommand>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new GameSessionCommandSucceeded(session));

        var persistence = Substitute.For<IGamePersistence>();
        var messagesPersistence = Substitute.For<IGameMessagePersistence>();
        var sessionInvalidationPublisher = Substitute.For<IGameSessionInvalidationPublisher>();
        var messageInvalidationPublisher = Substitute.For<IGameMessageInvalidationPublisher>();

        var handler = new SubmitOrdersHandler(
            sessionService,
            sessionInvalidationPublisher,
            messageInvalidationPublisher,
            persistence,
            messagesPersistence
        );
        var command = new NexusTurnOrdersCommand(RedPlayerId, 1, [], [], false);
        var result = await handler.HandleAsync(GameId, command);

        Assert.IsType<GameSessionCommandSucceeded>(result);
        await messagesPersistence
            .DidNotReceive()
            .WriteGameplayEventsAsync(
                Arg.Any<Guid>(),
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleAsync_writes_formatted_events_when_round_resolves()
    {
        var resolveEvents = new NexusResolveEvent[]
        {
            new NexusIncomeEvent(RedPlayerId, 1, []),
            new NexusIncomeEvent(BluePlayerId, 1, []),
        };

        var session = CreateSession(resolveEvents: resolveEvents);

        var sessionService = Substitute.For<IGameSessionService>();
        sessionService
            .SubmitOrdersAsync(
                Arg.Any<Guid>(),
                Arg.Any<NexusTurnOrdersCommand>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new GameSessionCommandSucceeded(session));

        var persistence = Substitute.For<IGamePersistence>();
        persistence
            .GetActivePlayersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<GamePlayerView>>([
                new GamePlayerView(RedPlayerId, "Alice", DateTime.UtcNow),
                new GamePlayerView(BluePlayerId, "Bob", DateTime.UtcNow),
            ]);

        var messagesPersistence = Substitute.For<IGameMessagePersistence>();
        var sessionInvalidationPublisher = Substitute.For<IGameSessionInvalidationPublisher>();
        var messageInvalidationPublisher = Substitute.For<IGameMessageInvalidationPublisher>();

        var handler = new SubmitOrdersHandler(
            sessionService,
            sessionInvalidationPublisher,
            messageInvalidationPublisher,
            persistence,
            messagesPersistence
        );
        var command = new NexusTurnOrdersCommand(RedPlayerId, 1, [], [], false);
        var result = await handler.HandleAsync(GameId, command);

        Assert.IsType<GameSessionCommandSucceeded>(result);
        await messagesPersistence
            .Received(1)
            .WriteGameplayEventsAsync(
                GameId,
                Arg.Is<IReadOnlyList<string>>(bodies =>
                    bodies.Count == 2 && bodies[0].Contains("Alice") && bodies[1].Contains("Bob")
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleAsync_falls_back_to_faction_name_when_player_not_found()
    {
        var resolveEvents = new NexusResolveEvent[] { new NexusIncomeEvent(RedPlayerId, 1, []) };

        var session = CreateSession(resolveEvents: resolveEvents);

        var sessionService = Substitute.For<IGameSessionService>();
        sessionService
            .SubmitOrdersAsync(
                Arg.Any<Guid>(),
                Arg.Any<NexusTurnOrdersCommand>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new GameSessionCommandSucceeded(session));

        var persistence = Substitute.For<IGamePersistence>();
        persistence
            .GetActivePlayersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<GamePlayerView>>([]);

        var messagesPersistence = Substitute.For<IGameMessagePersistence>();
        var sessionInvalidationPublisher = Substitute.For<IGameSessionInvalidationPublisher>();
        var messageInvalidationPublisher = Substitute.For<IGameMessageInvalidationPublisher>();

        var handler = new SubmitOrdersHandler(
            sessionService,
            sessionInvalidationPublisher,
            messageInvalidationPublisher,
            persistence,
            messagesPersistence
        );
        var command = new NexusTurnOrdersCommand(RedPlayerId, 1, [], [], false);
        await handler.HandleAsync(GameId, command);

        await messagesPersistence
            .Received(1)
            .WriteGameplayEventsAsync(
                GameId,
                Arg.Is<IReadOnlyList<string>>(bodies =>
                    bodies.Count == 1 && bodies[0].Contains("Red")
                ),
                Arg.Any<CancellationToken>()
            );
    }

    private static NexusGameView CreateSession(IReadOnlyList<NexusResolveEvent> resolveEvents)
    {
        var currentPlayer = new NexusPlayerView(
            RedPlayerId,
            NexusFactionColor.Red,
            0,
            NexusGateProgress.None,
            false,
            true,
            [],
            null,
            false,
            0,
            0
        );
        var opponentPlayer = new NexusPlayerView(
            BluePlayerId,
            NexusFactionColor.Blue,
            0,
            NexusGateProgress.None,
            false,
            true,
            null,
            null,
            false,
            0,
            0
        );
        return new NexusGameView(
            GameId,
            2,
            [],
            currentPlayer,
            opponentPlayer,
            resolveEvents.ToImmutableArray(),
            null
        );
    }
}

using System.Collections.Immutable;
using Spx.Game.Application;
using Spx.Game.Application.Nexus;
using Spx.Game.Application.Nexus.Features.SubmitOrders;
using Spx.Nexus.Domain;
using Xunit;

namespace Spx.Game.Application.Tests;

public sealed class SubmitOrdersHandlerTests
{
    private static readonly Guid GameId = Guid.NewGuid();
    private static readonly Guid RedPlayerId = Guid.NewGuid();
    private static readonly Guid BluePlayerId = Guid.NewGuid();

    [Fact]
    public void Format_WhenViewingPlayerMatchesHomeOwner_UsesYourHomeSystem()
    {
        var playerNames = new Dictionary<Guid, string>
        {
            [RedPlayerId] = "Alice",
            [BluePlayerId] = "Bob",
        };
        var evt = new NexusUnitsMovedEvent(
            RedPlayerId,
            NexusMapTopology.Player1HomeCoord,
            new HexCoord(1, -2),
            ImmutableArray.Create(new NexusUnitStackGroup(NexusUnitType.Fighter, 1, 1)),
            IsRetreat: false
        );

        var message = NexusSessionEventFormatter.Format(evt, playerNames, RedPlayerId);

        Assert.Contains("Your Home System", message);
        Assert.DoesNotContain("home system", message);
    }

    [Fact]
    public void Format_WhenCombatPhaseResolves_IncludesPerRollUnitHitsAndLossSummary()
    {
        var playerNames = new Dictionary<Guid, string>
        {
            [RedPlayerId] = "Alice",
            [BluePlayerId] = "Bob",
        };
        var evt = new NexusCombatResultEvent(
            new HexCoord(0, 0),
            [
                new NexusCombatLoss(BluePlayerId, NexusUnitType.Destroyer, 1),
                new NexusCombatLoss(RedPlayerId, NexusUnitType.Fighter, 2),
            ],
            [
                new NexusCombatAttackRoll(
                    RedPlayerId,
                    NexusUnitType.Cruiser,
                    NexusUnitType.Destroyer,
                    5,
                    3,
                    true,
                    2,
                    BluePlayerId,
                    1
                ),
                new NexusCombatAttackRoll(
                    RedPlayerId,
                    NexusUnitType.Cruiser,
                    NexusUnitType.Destroyer,
                    2,
                    3,
                    false,
                    2,
                    BluePlayerId,
                    1
                ),
                new NexusCombatAttackRoll(
                    BluePlayerId,
                    NexusUnitType.Destroyer,
                    NexusUnitType.Fighter,
                    5,
                    5,
                    true,
                    1,
                    RedPlayerId,
                    1
                ),
            ]
        );

        var message = NexusSessionEventFormatter.Format(evt, playerNames, RedPlayerId);

        Assert.Contains("Normal at Nexus", message);
        Assert.Contains(
            "Alice Cruiser (2/2 hits) -> Bob Destroyer (1/2 hits): rolled 5 vs 3 hit",
            message
        );
        Assert.Contains(
            "Alice Cruiser (2/2 hits) -> Bob Destroyer (1/2 hits): rolled 2 vs 3 miss",
            message
        );
        Assert.Contains(
            "Bob Destroyer (1/2 hits) -> Alice Fighter (1/1 hits): rolled 5 vs 5 hit",
            message
        );
        Assert.Contains("Losses: Alice loses 2× Fighter; Bob loses 1× Destroyer", message);
    }

    [Fact]
    public void Format_WhenSystemIsContested_DoesNotAppendUnitsOnBothSides()
    {
        var playerNames = new Dictionary<Guid, string>
        {
            [RedPlayerId] = "Alice",
            [BluePlayerId] = "Bob",
        };
        var evt = new NexusSystemContestedEvent(new HexCoord(-1, 1));

        var message = NexusSessionEventFormatter.Format(evt, playerNames, RedPlayerId);

        Assert.Equal("Delta is contested", message);
    }

    [Fact]
    public async Task HandleAsync_returns_failed_outcome_when_session_service_rejects()
    {
        var sessionService = Substitute.For<INexusSessionService>();
        sessionService
            .SubmitOrdersAsync(
                Arg.Any<Guid>(),
                Arg.Any<NexusTurnOrdersCommand>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new GameSessionCommandFailed("rejected"));

        var persistence = Substitute.For<IGamePersistence>();
        var messagesPersistence = Substitute.For<IGameMessagePersistence>();
        var sessionInvalidationPublisher = Substitute.For<INexusSessionInvalidationPublisher>();
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

        var sessionService = Substitute.For<INexusSessionService>();
        sessionService
            .SubmitOrdersAsync(
                Arg.Any<Guid>(),
                Arg.Any<NexusTurnOrdersCommand>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new GameSessionCommandSucceeded(session));

        var persistence = Substitute.For<IGamePersistence>();
        var messagesPersistence = Substitute.For<IGameMessagePersistence>();
        var sessionInvalidationPublisher = Substitute.For<INexusSessionInvalidationPublisher>();
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
            new NexusIncomeEvent(RedPlayerId, 0, []),
            new NexusIncomeEvent(BluePlayerId, 0, []),
        };

        var session = CreateSession(resolveEvents: resolveEvents);

        var sessionService = Substitute.For<INexusSessionService>();
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
        var sessionInvalidationPublisher = Substitute.For<INexusSessionInvalidationPublisher>();
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
        var resolveEvents = new NexusResolveEvent[] { new NexusIncomeEvent(RedPlayerId, 0, []) };

        var session = CreateSession(resolveEvents: resolveEvents);

        var sessionService = Substitute.For<INexusSessionService>();
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
        var sessionInvalidationPublisher = Substitute.For<INexusSessionInvalidationPublisher>();
        var messageInvalidationPublisher = Substitute.For<IGameMessageInvalidationPublisher>();

        var handler = new SubmitOrdersHandler(
            sessionService,
            sessionInvalidationPublisher,
            messageInvalidationPublisher,
            persistence,
            messagesPersistence
        );
        var command = new NexusTurnOrdersCommand(RedPlayerId, 1, [], [], false);
        var expectedFallbackName = RedPlayerId.ToString("N").Substring(0, 8);
        await handler.HandleAsync(GameId, command);

        await messagesPersistence
            .Received(1)
            .WriteGameplayEventsAsync(
                GameId,
                Arg.Is<IReadOnlyList<string>>(bodies =>
                    bodies.Count == 1 && bodies[0].Contains(expectedFallbackName)
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

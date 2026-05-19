using Microsoft.Extensions.DependencyInjection;
using Spx.Game.Application;
using Spx.Game.Application.Features.SubmitAcquireCard;
using Spx.Game.Application.Features.SubmitPlayBatch;
using Spx.Game.Domain;
using Xunit;

namespace Spx.Game.Application.Tests;

public sealed class GameSessionCommandHandlerTests
{
    [Fact]
    public async Task SubmitAcquire_returns_failure_when_session_service_rejects_choice()
    {
        var sessionService = Substitute.For<IGameSessionService>();
        sessionService
            .SubmitAcquireAsync(
                Arg.Any<Guid>(),
                Arg.Any<SubmitAcquireRequest>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new GameSessionCommandFailed("The selected market card is no longer available.")
            );
        using var services = CreateServices(sessionService);

        var handler = services.GetRequiredService<ISubmitAcquireCardHandler>();
        var result = await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), 2, Guid.NewGuid());

        var failed = Assert.IsType<GameSessionCommandFailed>(result);
        Assert.Equal("The selected market card is no longer available.", failed.ErrorMessage);
    }

    [Fact]
    public async Task SubmitAcquire_returns_session_and_publishes_invalidation_on_success()
    {
        var session = CreateSession();
        var sessionService = Substitute.For<IGameSessionService>();
        sessionService
            .SubmitAcquireAsync(
                Arg.Any<Guid>(),
                Arg.Any<SubmitAcquireRequest>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new GameSessionCommandSucceeded(session));
        var invalidationPublisher = Substitute.For<IGameSessionInvalidationPublisher>();
        using var services = CreateServices(sessionService, invalidationPublisher);

        var handler = services.GetRequiredService<ISubmitAcquireCardHandler>();
        var result = await handler.HandleAsync(
            session.GameId,
            Guid.NewGuid(),
            session.RoundNumber,
            Guid.NewGuid()
        );

        var succeeded = Assert.IsType<GameSessionCommandSucceeded>(result);
        Assert.Equal(session, succeeded.Session);
        await invalidationPublisher
            .Received(1)
            .PublishSessionInvalidatedAsync(session.GameId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitPlayBatch_returns_session_and_publishes_invalidation_on_success()
    {
        var pendingGameplayEventBatchId = Guid.NewGuid();
        var session = CreateSession(
            phase: GamePhase.Play,
            canLockBatch: true,
            lastResolvedBatch: new GameResolvedBatchView(
                1,
                [
                    new GameResolvedPlayerBatchView(
                        new GameSessionParticipant(Guid.NewGuid()),
                        [],
                        false
                    ),
                    new GameResolvedPlayerBatchView(
                        new GameSessionParticipant(Guid.NewGuid()),
                        [],
                        false
                    ),
                ],
                DateTime.UtcNow
            )
        );
        var firstPlayerId = Guid.NewGuid();
        var secondPlayerId = Guid.NewGuid();
        var gameplayEvents = new[]
        {
            new GameplayEvent(
                GameplayEventKind.CreatedCard,
                firstPlayerId,
                GameCardDefinition.Extract,
                null,
                null,
                GameCardDefinition.Red
            ),
            new GameplayEvent(
                GameplayEventKind.Fizzled,
                secondPlayerId,
                GameCardDefinition.Sabotage,
                null,
                null,
                null
            ),
        };
        var sessionService = Substitute.For<IGameSessionService>();
        sessionService
            .SubmitPlayBatchAsync(
                Arg.Any<Guid>(),
                Arg.Any<SubmitPlayBatchRequest>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new GameSessionCommandSucceeded(
                    session,
                    gameplayEvents,
                    pendingGameplayEventBatchId
                )
            );
        Guid? acknowledgedBatchId = null;
        sessionService
            .When(s =>
                s.AcknowledgeGameplayEventBatchAsync(
                    Arg.Any<Guid>(),
                    Arg.Any<Guid>(),
                    Arg.Any<CancellationToken>()
                )
            )
            .Do(call => acknowledgedBatchId = call.ArgAt<Guid>(1));
        var invalidationPublisher = Substitute.For<IGameSessionInvalidationPublisher>();
        var messagePublisher = Substitute.For<IGameMessageInvalidationPublisher>();
        var gameplayEventWriter = Substitute.For<IGameplayEventMessageWriter>();
        gameplayEventWriter
            .PersistResolvedBatchAsync(
                Arg.Any<Guid>(),
                Arg.Any<GameResolvedBatchView?>(),
                Arg.Any<GameCompletionView?>(),
                Arg.Any<IReadOnlyList<GameplayEvent>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(1);
        using var services = CreateServices(
            sessionService,
            invalidationPublisher,
            messagePublisher,
            gameplayEventWriter
        );

        var handler = services.GetRequiredService<ISubmitPlayBatchHandler>();
        var result = await handler.HandleAsync(
            session.GameId,
            Guid.NewGuid(),
            session.RoundNumber,
            [
                new GameBatchCardSelection(
                    Guid.NewGuid(),
                    GameResourceColor.Red,
                    null,
                    null,
                    null,
                    []
                ),
            ]
        );

        var succeeded = Assert.IsType<GameSessionCommandSucceeded>(result);
        Assert.Equal(session, succeeded.Session);
        await invalidationPublisher
            .Received(1)
            .PublishSessionInvalidatedAsync(session.GameId, Arg.Any<CancellationToken>());
        await messagePublisher
            .Received(1)
            .PublishMessagesInvalidatedAsync(session.GameId, Arg.Any<CancellationToken>());
        await gameplayEventWriter
            .Received(1)
            .PersistResolvedBatchAsync(
                session.GameId,
                session.LastResolvedBatch,
                Arg.Any<GameCompletionView?>(),
                gameplayEvents,
                Arg.Any<CancellationToken>()
            );
        Assert.Equal(pendingGameplayEventBatchId, acknowledgedBatchId);
    }

    private static ServiceProvider CreateServices(
        IGameSessionService sessionService,
        IGameSessionInvalidationPublisher? invalidationPublisher = null,
        IGameMessageInvalidationPublisher? messagePublisher = null,
        IGameplayEventMessageWriter? gameplayEventWriter = null
    )
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGameApplication();
        services.AddSingleton(sessionService);
        services.AddSingleton(gameplayEventWriter ?? Substitute.For<IGameplayEventMessageWriter>());
        services.AddSingleton(Substitute.For<IGamePersistence>());
        services.AddSingleton(Substitute.For<IGameLobbyInvalidationPublisher>());
        services.AddSingleton(
            invalidationPublisher ?? Substitute.For<IGameSessionInvalidationPublisher>()
        );
        services.AddSingleton(
            messagePublisher ?? Substitute.For<IGameMessageInvalidationPublisher>()
        );
        services.AddSingleton(Substitute.For<IGameMessagePersistence>());
        return services.BuildServiceProvider();
    }

    private static GameSessionView CreateSession(
        GamePhase phase = GamePhase.Acquire,
        bool canLockBatch = false,
        GameResolvedBatchView? lastResolvedBatch = null
    )
    {
        var currentPlayer = new GameSessionParticipant(Guid.NewGuid());
        var opponentPlayer = new GameSessionParticipant(Guid.NewGuid());

        return new GameSessionView(
            Guid.NewGuid(),
            1,
            phase,
            new GamePlayerStateView(
                currentPlayer,
                [],
                false,
                0,
                0,
                false,
                phase == GamePhase.Acquire,
                []
            ),
            new GamePlayerStateView(
                opponentPlayer,
                [],
                false,
                0,
                0,
                false,
                phase != GamePhase.Acquire,
                []
            ),
            [],
            0,
            false,
            phase == GamePhase.Acquire,
            canLockBatch,
            GameCardCatalog.MaxBatchSize,
            lastResolvedBatch,
            null
        );
    }
}

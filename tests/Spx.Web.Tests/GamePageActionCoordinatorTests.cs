using Microsoft.Extensions.Logging.Abstractions;
using Spx.Contracts;
using Spx.Game.Application;
using Spx.Game.Application.Features.LeaveGame;
using Spx.Game.Application.Features.SubmitAcquireCard;
using Spx.Game.Application.Features.SubmitPlayBatch;
using Spx.Web.Components.Pages;
using Xunit;

namespace Spx.Web.Tests;

public sealed class GamePageActionCoordinatorTests
{
    [Fact]
    public async Task LeaveGameAsync_returns_true_when_leave_succeeds()
    {
        var coordinator = CreateCoordinator(out var data, out var actions, out _, leaveHandler: new StubLeaveGameHandler { Result = new GameCommandSucceeded(Guid.NewGuid()) });

        var result = await coordinator.LeaveGameAsync(Guid.NewGuid(), "user-1");

        Assert.True(result);
        Assert.False(actions.IsLeaving);
        Assert.Null(data.ErrorMessage);
    }

    [Fact]
    public async Task LeaveGameAsync_sets_error_when_leave_fails()
    {
        var coordinator = CreateCoordinator(out var data, out var actions, out _, leaveHandler: new StubLeaveGameHandler { Result = new GameCommandFailed("Cannot leave.") });

        var result = await coordinator.LeaveGameAsync(Guid.NewGuid(), "user-1");

        Assert.False(result);
        Assert.Equal("Cannot leave.", data.ErrorMessage);
        Assert.False(actions.IsLeaving);
    }

    [Fact]
    public async Task AcquireCardAsync_updates_session_when_command_succeeds()
    {
        var gameId = Guid.NewGuid();
        var initialSession = GamePageCoordinatorTestData.CreateSession(gameId, roundNumber: 2);
        var updatedSession = GamePageCoordinatorTestData.CreateSession(gameId, roundNumber: 3);
        var coordinator = CreateCoordinator(
            out var data,
            out var actions,
            out _,
            lobby: GamePageCoordinatorTestData.CreateLobby(gameId),
            session: initialSession,
            acquireHandler: new StubSubmitAcquireCardHandler { Result = new GameSessionCommandSucceeded(updatedSession) });

        await coordinator.AcquireCardAsync(gameId, GamePageCoordinatorTestData.CurrentPlayerId, Guid.NewGuid());

        Assert.Equal(updatedSession, data.Session);
        Assert.False(actions.IsSubmittingGameplayAction);
        Assert.Null(data.GameplayError);
    }

    [Fact]
    public async Task AcquireCardAsync_skips_handler_when_current_user_is_inactive()
    {
        var gameId = Guid.NewGuid();
        var acquireHandler = new StubSubmitAcquireCardHandler { Result = new GameSessionCommandSucceeded(GamePageCoordinatorTestData.CreateSession(gameId, roundNumber: 3)) };
        var coordinator = CreateCoordinator(
            out var data,
            out _,
            out _,
            lobby: GamePageCoordinatorTestData.CreateLobby(gameId, isCurrentUserActive: false),
            session: GamePageCoordinatorTestData.CreateSession(gameId, roundNumber: 2),
            acquireHandler: acquireHandler);

        await coordinator.AcquireCardAsync(gameId, GamePageCoordinatorTestData.CurrentPlayerId, Guid.NewGuid());

        Assert.Equal(0, acquireHandler.CallCount);
        Assert.Equal(2, data.Session!.RoundNumber);
    }

    [Fact]
    public async Task LockBatchAsync_sets_gameplay_error_when_command_fails()
    {
        var gameId = Guid.NewGuid();
        var coordinator = CreateCoordinator(
            out var data,
            out var actions,
            out _,
            lobby: GamePageCoordinatorTestData.CreateLobby(gameId),
            session: GamePageCoordinatorTestData.CreateSession(gameId, roundNumber: 2),
            playBatchHandler: new StubSubmitPlayBatchHandler { Result = new GameSessionCommandFailed("Batch rejected.") });

        await coordinator.LockBatchAsync(gameId, GamePageCoordinatorTestData.CurrentPlayerId, GamePageCoordinatorTestData.CreateBatchSelection());

        Assert.Equal("Batch rejected.", data.GameplayError);
        Assert.False(actions.IsSubmittingGameplayAction);
    }

    [Fact]
    public async Task LockBatchAsync_updates_session_and_adds_immediate_gameplay_entries()
    {
        var gameId = Guid.NewGuid();
        var resolvedAtUtc = new DateTime(2026, 5, 17, 12, 0, 0, DateTimeKind.Utc);
        var updatedSession = GamePageCoordinatorTestData.CreateSession(gameId, roundNumber: 3, lastResolvedBatch: GamePageCoordinatorTestData.CreateResolvedBatch(3, resolvedAtUtc));
        var formatter = new StubGameplayEventMessageFormatter { Result = ["Captain Red resolved Extract."] };
        var coordinator = CreateCoordinator(
            out var data,
            out var actions,
            out var timeline,
            lobby: GamePageCoordinatorTestData.CreateLobby(gameId),
            session: GamePageCoordinatorTestData.CreateSession(gameId, roundNumber: 2),
            playBatchHandler: new StubSubmitPlayBatchHandler { Result = new GameSessionCommandSucceeded(updatedSession, [GamePageCoordinatorTestData.CreateGameplayEvent()]) },
            formatter: formatter);

        await coordinator.LockBatchAsync(gameId, GamePageCoordinatorTestData.CurrentPlayerId, GamePageCoordinatorTestData.CreateBatchSelection());

        Assert.Equal(updatedSession, data.Session);
        Assert.False(actions.IsSubmittingGameplayAction);
        Assert.True(timeline.ShouldScrollTimelineToBottom);
        var localEntry = Assert.Single(timeline.Items);
        Assert.Equal("Captain Red resolved Extract.", localEntry.Local?.Body);
        Assert.Equal(GameMessageKind.GameplayEvent, localEntry.Local?.Kind);
        Assert.Equal(resolvedAtUtc, localEntry.Local?.CreatedAtUtc);
        Assert.Equal(1, formatter.CallCount);
    }

    private static GamePageActionCoordinator CreateCoordinator(
        out GamePageDataState data,
        out GamePageActionState actions,
        out GameTimelineState timeline,
        GameLobbyView? lobby = null,
        GameSessionView? session = null,
        StubLeaveGameHandler? leaveHandler = null,
        StubSubmitAcquireCardHandler? acquireHandler = null,
        StubSubmitPlayBatchHandler? playBatchHandler = null,
        StubGameplayEventMessageFormatter? formatter = null)
    {
        data = new GamePageDataState();
        actions = new GamePageActionState();
        timeline = new GameTimelineState();

        if (lobby is not null || session is not null)
        {
            data.ApplyPage(new GamePageView(
                lobby ?? GamePageCoordinatorTestData.CreateLobby(Guid.NewGuid()),
                session,
                GamePresenceView.Empty));
        }

        return new GamePageActionCoordinator(
            leaveHandler ?? new StubLeaveGameHandler { Result = new GameCommandSucceeded(Guid.NewGuid()) },
            acquireHandler ?? new StubSubmitAcquireCardHandler { Result = new GameSessionCommandSucceeded(session ?? GamePageCoordinatorTestData.CreateSession(Guid.NewGuid())) },
            playBatchHandler ?? new StubSubmitPlayBatchHandler { Result = new GameSessionCommandSucceeded(session ?? GamePageCoordinatorTestData.CreateSession(Guid.NewGuid())) },
            formatter ?? new StubGameplayEventMessageFormatter(),
            NullLogger<GamePageActionCoordinator>.Instance,
            data,
            actions,
            timeline);
    }

    private sealed class StubLeaveGameHandler : ILeaveGameHandler
    {
        public GameCommandOutcome Result { get; init; } = new GameCommandSucceeded(Guid.NewGuid());

        public Exception? Exception { get; init; }

        public int CallCount { get; private set; }

        public Task<GameCommandOutcome> HandleAsync(Guid gameId, string userId, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Exception is null ? Task.FromResult(Result) : Task.FromException<GameCommandOutcome>(Exception);
        }
    }

    private sealed class StubSubmitAcquireCardHandler : ISubmitAcquireCardHandler
    {
        public GameSessionCommandOutcome Result { get; init; } = new GameSessionCommandFailed("No result configured.");

        public Exception? Exception { get; init; }

        public int CallCount { get; private set; }

        public Task<GameSessionCommandOutcome> HandleAsync(Guid gameId, Guid playerId, int expectedRoundNumber, Guid marketCardInstanceId, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Exception is null ? Task.FromResult(Result) : Task.FromException<GameSessionCommandOutcome>(Exception);
        }
    }

    private sealed class StubSubmitPlayBatchHandler : ISubmitPlayBatchHandler
    {
        public GameSessionCommandOutcome Result { get; init; } = new GameSessionCommandFailed("No result configured.");

        public Exception? Exception { get; init; }

        public int CallCount { get; private set; }

        public Task<GameSessionCommandOutcome> HandleAsync(Guid gameId, Guid playerId, int expectedRoundNumber, IReadOnlyList<GameBatchCardSelection> cards, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Exception is null ? Task.FromResult(Result) : Task.FromException<GameSessionCommandOutcome>(Exception);
        }
    }

    private sealed class StubGameplayEventMessageFormatter : IGameplayEventMessageFormatter
    {
        public IReadOnlyList<string> Result { get; init; } = [];

        public int CallCount { get; private set; }

        public IReadOnlyList<string> CreateMessageBodies(
            GameResolvedBatchView? lastResolvedBatch,
            GameCompletionView? completion,
            IReadOnlyList<GameplayEvent> gameplayEvents,
            IReadOnlyDictionary<Guid, string> playerNames)
        {
            CallCount++;
            return Result;
        }
    }
}
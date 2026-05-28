using Microsoft.Extensions.Logging.Abstractions;
using Spx.Game.Application;
using Spx.Game.Application.Features.LeaveGame;
using Spx.Game.Application.Nexus.Features.SubmitOrders;
using Spx.Web.Components.Pages.Nexus;
using Xunit;

namespace Spx.Web.Tests;

public sealed class GamePageActionCoordinatorTests
{
    [Fact]
    public async Task LeaveGameAsync_returns_true_when_leave_succeeds()
    {
        var coordinator = CreateCoordinator(
            out var data,
            out var actions,
            leaveHandler: new StubLeaveGameHandler
            {
                Result = new GameCommandSucceeded(Guid.NewGuid()),
            }
        );

        var result = await coordinator.LeaveGameAsync(Guid.NewGuid(), "user-1");

        Assert.True(result);
        Assert.False(actions.IsLeaving);
        Assert.Null(data.ErrorMessage);
    }

    [Fact]
    public async Task LeaveGameAsync_sets_error_when_leave_fails()
    {
        var coordinator = CreateCoordinator(
            out var data,
            out var actions,
            leaveHandler: new StubLeaveGameHandler
            {
                Result = new GameCommandFailed("Cannot leave."),
            }
        );

        var result = await coordinator.LeaveGameAsync(Guid.NewGuid(), "user-1");

        Assert.False(result);
        Assert.Equal("Cannot leave.", data.ErrorMessage);
        Assert.False(actions.IsLeaving);
    }

    [Fact]
    public async Task SubmitOrdersAsync_updates_session_when_command_succeeds()
    {
        var gameId = Guid.NewGuid();
        var initialSession = GamePageCoordinatorTestData.CreateSession(gameId, roundNumber: 2);
        var updatedSession = GamePageCoordinatorTestData.CreateSession(gameId, roundNumber: 3);
        var coordinator = CreateCoordinator(
            out var data,
            out var actions,
            lobby: GamePageCoordinatorTestData.CreateLobby(gameId),
            session: initialSession,
            submitOrdersHandler: new StubSubmitOrdersHandler
            {
                Result = new GameSessionCommandSucceeded(updatedSession),
            }
        );

        var command = new NexusSubmitTurnCommand(
            GamePageCoordinatorTestData.CurrentPlayerId,
            2,
            [],
            [],
            false
        );
        await coordinator.SubmitOrdersAsync(gameId, command);

        Assert.Equal(updatedSession, data.Session);
        Assert.False(actions.IsSubmittingGameplayAction);
        Assert.Null(data.GameplayError);
    }

    [Fact]
    public async Task SubmitOrdersAsync_sets_gameplay_error_when_command_fails()
    {
        var gameId = Guid.NewGuid();
        var coordinator = CreateCoordinator(
            out var data,
            out var actions,
            lobby: GamePageCoordinatorTestData.CreateLobby(gameId),
            session: GamePageCoordinatorTestData.CreateSession(gameId, roundNumber: 2),
            submitOrdersHandler: new StubSubmitOrdersHandler
            {
                Result = new GameSessionCommandFailed("Orders rejected."),
            }
        );

        var command = new NexusSubmitTurnCommand(
            GamePageCoordinatorTestData.CurrentPlayerId,
            2,
            [],
            [],
            false
        );
        await coordinator.SubmitOrdersAsync(gameId, command);

        Assert.Equal("Orders rejected.", data.GameplayError);
        Assert.False(actions.IsSubmittingGameplayAction);
    }

    [Fact]
    public async Task SubmitOrdersAsync_skips_handler_when_current_user_is_inactive()
    {
        var gameId = Guid.NewGuid();
        var submitHandler = new StubSubmitOrdersHandler
        {
            Result = new GameSessionCommandSucceeded(
                GamePageCoordinatorTestData.CreateSession(gameId, roundNumber: 3)
            ),
        };
        var coordinator = CreateCoordinator(
            out var data,
            out _,
            lobby: GamePageCoordinatorTestData.CreateLobby(gameId, isCurrentUserActive: false),
            session: GamePageCoordinatorTestData.CreateSession(gameId, roundNumber: 2),
            submitOrdersHandler: submitHandler
        );

        var command = new NexusSubmitTurnCommand(
            GamePageCoordinatorTestData.CurrentPlayerId,
            2,
            [],
            [],
            false
        );
        await coordinator.SubmitOrdersAsync(gameId, command);

        Assert.Equal(0, submitHandler.CallCount);
        Assert.Equal(2, data.Session!.RoundNumber);
    }

    private static NexusPageActionCoordinator CreateCoordinator(
        out NexusPageDataState data,
        out NexusPageActionState actions,
        GameLobbyView? lobby = null,
        NexusSessionView? session = null,
        StubLeaveGameHandler? leaveHandler = null,
        StubSubmitOrdersHandler? submitOrdersHandler = null
    )
    {
        data = new NexusPageDataState();
        actions = new NexusPageActionState();

        if (lobby is not null || session is not null)
        {
            data.ApplyPage(
                new GamePageView(
                    lobby ?? GamePageCoordinatorTestData.CreateLobby(Guid.NewGuid()),
                    session,
                    GamePresenceView.Empty
                )
            );
        }

        return new NexusPageActionCoordinator(
            leaveHandler
                ?? new StubLeaveGameHandler { Result = new GameCommandSucceeded(Guid.NewGuid()) },
            submitOrdersHandler
                ?? new StubSubmitOrdersHandler
                {
                    Result = new GameSessionCommandSucceeded(
                        session ?? GamePageCoordinatorTestData.CreateSession(Guid.NewGuid())
                    ),
                },
            NullLogger<NexusPageActionCoordinator>.Instance,
            data,
            actions
        );
    }

    private sealed class StubLeaveGameHandler : ILeaveGameHandler
    {
        public GameCommandOutcome Result { get; init; } = new GameCommandSucceeded(Guid.NewGuid());

        public Exception? Exception { get; init; }

        public int CallCount { get; private set; }

        public Task<GameCommandOutcome> HandleAsync(
            Guid gameId,
            string userId,
            CancellationToken cancellationToken = default
        )
        {
            CallCount++;
            return Exception is null
                ? Task.FromResult(Result)
                : Task.FromException<GameCommandOutcome>(Exception);
        }
    }

    private sealed class StubSubmitOrdersHandler : ISubmitOrdersHandler
    {
        public GameSessionCommandOutcome Result { get; init; } =
            new GameSessionCommandFailed("No result configured.");

        public Exception? Exception { get; init; }

        public int CallCount { get; private set; }

        public Task<GameSessionCommandOutcome> HandleAsync(
            Guid gameId,
            NexusSubmitTurnCommand command,
            CancellationToken cancellationToken = default
        )
        {
            CallCount++;
            return Exception is null
                ? Task.FromResult(Result)
                : Task.FromException<GameSessionCommandOutcome>(Exception);
        }
    }
}

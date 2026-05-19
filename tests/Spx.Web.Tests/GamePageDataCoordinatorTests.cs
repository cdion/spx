using Microsoft.Extensions.Logging.Abstractions;
using Spx.Game.Application;
using Spx.Game.Application.Features.GetGamePage;
using Spx.Game.Application.Features.GetGamePresence;
using Spx.Game.Application.Features.GetGameSession;
using Spx.Web.Components.Pages;
using Xunit;

namespace Spx.Web.Tests;

public sealed class GamePageDataCoordinatorTests
{
    [Fact]
    public async Task LoadPageAsync_applies_loaded_page_and_clears_errors()
    {
        var gameId = Guid.NewGuid();
        var expectedPage = GamePageCoordinatorTestData.CreatePage(
            gameId,
            new GamePresenceView([GamePageCoordinatorTestData.CurrentPlayerId])
        );
        var state = new GamePageDataState();
        state.SetErrorMessage("old error");
        state.SetGameplayError("old gameplay error");

        var coordinator = new GamePageDataCoordinator(
            new StubGetGamePageHandler { Result = expectedPage },
            new StubGetGameSessionHandler(),
            new StubGetGamePresenceHandler(),
            NullLogger<GamePageDataCoordinator>.Instance,
            state
        );

        await coordinator.LoadPageAsync(gameId, "user-1");

        Assert.Equal(expectedPage.Lobby, state.Lobby);
        Assert.Equal(expectedPage.Session, state.Session);
        Assert.Equal(expectedPage.Presence, state.Presence);
        Assert.Null(state.ErrorMessage);
        Assert.Null(state.GameplayError);
        Assert.False(state.IsLoading);
    }

    [Fact]
    public async Task LoadPageAsync_sets_error_and_clears_page_state_when_handler_throws()
    {
        var gameId = Guid.NewGuid();
        var state = new GamePageDataState();
        state.ApplyPage(
            GamePageCoordinatorTestData.CreatePage(
                gameId,
                new GamePresenceView([GamePageCoordinatorTestData.CurrentPlayerId])
            )
        );

        var coordinator = new GamePageDataCoordinator(
            new StubGetGamePageHandler { Exception = new InvalidOperationException("boom") },
            new StubGetGameSessionHandler(),
            new StubGetGamePresenceHandler(),
            NullLogger<GamePageDataCoordinator>.Instance,
            state
        );

        await coordinator.LoadPageAsync(gameId, "user-1");

        Assert.Null(state.Lobby);
        Assert.Null(state.Session);
        Assert.Equal(GamePresenceView.Empty, state.Presence);
        Assert.Equal(
            "We couldn't load this game right now. Refresh and try again.",
            state.ErrorMessage
        );
        Assert.False(state.IsLoading);
    }

    [Fact]
    public async Task ReloadPresenceAsync_updates_presence()
    {
        var gameId = Guid.NewGuid();
        var expectedPresence = new GamePresenceView([
            GamePageCoordinatorTestData.CurrentPlayerId,
            GamePageCoordinatorTestData.OpponentPlayerId,
        ]);
        var state = new GamePageDataState();

        var coordinator = new GamePageDataCoordinator(
            new StubGetGamePageHandler(),
            new StubGetGameSessionHandler(),
            new StubGetGamePresenceHandler { Result = expectedPresence },
            NullLogger<GamePageDataCoordinator>.Instance,
            state
        );

        await coordinator.ReloadPresenceAsync(gameId);

        Assert.Equal(expectedPresence, state.Presence);
    }

    [Fact]
    public async Task ReloadSessionAsync_updates_session()
    {
        var gameId = Guid.NewGuid();
        var expectedSession = GamePageCoordinatorTestData.CreateSession(gameId, roundNumber: 4);
        var state = new GamePageDataState();

        var coordinator = new GamePageDataCoordinator(
            new StubGetGamePageHandler(),
            new StubGetGameSessionHandler { Result = expectedSession },
            new StubGetGamePresenceHandler(),
            NullLogger<GamePageDataCoordinator>.Instance,
            state
        );

        await coordinator.ReloadSessionAsync(gameId, GamePageCoordinatorTestData.CurrentPlayerId);

        Assert.Equal(expectedSession, state.Session);
    }

    [Fact]
    public async Task ReloadSessionAsync_sets_gameplay_error_when_refresh_fails()
    {
        var gameId = Guid.NewGuid();
        var state = new GamePageDataState();

        var coordinator = new GamePageDataCoordinator(
            new StubGetGamePageHandler(),
            new StubGetGameSessionHandler { Exception = new InvalidOperationException("boom") },
            new StubGetGamePresenceHandler(),
            NullLogger<GamePageDataCoordinator>.Instance,
            state
        );

        await coordinator.ReloadSessionAsync(gameId, GamePageCoordinatorTestData.CurrentPlayerId);

        Assert.Equal(
            "We couldn't refresh the game state right now. Please try again.",
            state.GameplayError
        );
    }

    private sealed class StubGetGamePageHandler : IGetGamePageHandler
    {
        public GamePageView? Result { get; init; }

        public Exception? Exception { get; init; }

        public Task<GamePageView?> HandleAsync(
            Guid gameId,
            string userId,
            CancellationToken cancellationToken = default
        ) =>
            Exception is null
                ? Task.FromResult(Result)
                : Task.FromException<GamePageView?>(Exception);
    }

    private sealed class StubGetGameSessionHandler : IGetGameSessionHandler
    {
        public GameSessionView? Result { get; init; }

        public Exception? Exception { get; init; }

        public Task<GameSessionView?> HandleAsync(
            Guid gameId,
            Guid playerId,
            CancellationToken cancellationToken = default
        ) =>
            Exception is null
                ? Task.FromResult(Result)
                : Task.FromException<GameSessionView?>(Exception);
    }

    private sealed class StubGetGamePresenceHandler : IGetGamePresenceHandler
    {
        public GamePresenceView Result { get; init; } = GamePresenceView.Empty;

        public Exception? Exception { get; init; }

        public Task<GamePresenceView> HandleAsync(
            Guid gameId,
            CancellationToken cancellationToken = default
        ) =>
            Exception is null
                ? Task.FromResult(Result)
                : Task.FromException<GamePresenceView>(Exception);
    }
}

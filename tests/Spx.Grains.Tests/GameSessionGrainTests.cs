using Spx.Contracts;
using Spx.Game.Domain;
using Spx.Grains;
using Xunit;

namespace Spx.Grains.Tests;

public sealed class GameSessionGrainTests
{
    private static readonly Guid GameId = Guid.Parse("6FD75A29-6B90-43AA-B97A-80A0C5210D73");
    private static readonly GameSessionPlayer FirstPlayer = new(Guid.Parse("0C8999C0-D4D2-46B5-B287-5D211CC99A40"), "user-1", "Red Captain");
    private static readonly GameSessionPlayer SecondPlayer = new(Guid.Parse("92C6775C-95F1-4C3B-9025-8E37D126CD4B"), "user-2", "Blue Captain");
    private static readonly IGameRoundResolver RoundResolver = new GameRoundResolver();

    [Fact]
    public void Initialize_allows_same_roster_twice()
    {
        var state = new GameSessionGrainState();
        var command = new InitializeGameSessionCommand(FirstPlayer, SecondPlayer);

        GameSessionEngine.Initialize(state, command);
        GameSessionEngine.Initialize(state, command);

        Assert.Equal(1, state.RoundNumber);
        Assert.Equal(FirstPlayer, state.FirstPlayer);
        Assert.Equal(SecondPlayer, state.SecondPlayer);
    }

    [Fact]
    public void Initialize_rejects_conflicting_roster()
    {
        var state = new GameSessionGrainState();
        GameSessionEngine.Initialize(state, new InitializeGameSessionCommand(FirstPlayer, SecondPlayer));

        var replacementPlayer = new GameSessionPlayer(Guid.NewGuid(), "user-3", "Green Captain");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            GameSessionEngine.Initialize(state, new InitializeGameSessionCommand(FirstPlayer, replacementPlayer)));

        Assert.Equal("The game session was already initialized with a different roster.", exception.Message);
    }

    [Fact]
    public void SubmitMove_keeps_first_submission_hidden_until_opponent_submits()
    {
        var state = CreateInitializedState();

        var view = GameSessionEngine.SubmitMove(
            state,
            GameId,
            new SubmitGameMoveCommand(FirstPlayer.UserId, ExpectedRoundNumber: 1, GameMove.Redite),
            RoundResolver,
            DateTime.UtcNow);

        Assert.Equal(1, view.RoundNumber);
        Assert.True(view.HasSubmittedMove);
        Assert.True(view.WaitingForOpponent);
        Assert.Null(view.LastResolvedRound);
        Assert.Equal(GameMove.Redite, state.FirstPlayerMove);
        Assert.Null(state.LastResolvedRound);
    }

    [Fact]
    public void SubmitMove_resolves_round_when_second_player_submits()
    {
        var state = CreateInitializedState();
        var resolvedAtUtc = new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);

        GameSessionEngine.SubmitMove(
            state,
            GameId,
            new SubmitGameMoveCommand(FirstPlayer.UserId, ExpectedRoundNumber: 1, GameMove.Redite),
            RoundResolver,
            resolvedAtUtc);

        var secondPlayerView = GameSessionEngine.SubmitMove(
            state,
            GameId,
            new SubmitGameMoveCommand(SecondPlayer.UserId, ExpectedRoundNumber: 1, GameMove.Bluon),
            RoundResolver,
            resolvedAtUtc);

        var firstPlayerView = GameSessionEngine.GetPlayerView(
            state,
            GameId,
            new GetGameSessionPlayerViewQuery(FirstPlayer.UserId),
            RoundResolver);

        Assert.NotNull(firstPlayerView);
        Assert.Equal(2, secondPlayerView.RoundNumber);
        Assert.False(secondPlayerView.HasSubmittedMove);
        Assert.False(secondPlayerView.WaitingForOpponent);
        Assert.NotNull(secondPlayerView.LastResolvedRound);
        Assert.Equal(GameRoundOutcome.CurrentPlayerWins, secondPlayerView.LastResolvedRound!.Result.Outcome);
        Assert.Equal(GameRoundOutcome.OpponentWins, firstPlayerView!.LastResolvedRound!.Result.Outcome);
        Assert.Null(state.FirstPlayerMove);
        Assert.Null(state.SecondPlayerMove);
        Assert.Equal(2, state.RoundNumber);
    }

    [Fact]
    public void SubmitMove_replaces_existing_move_before_round_resolves()
    {
        var state = CreateInitializedState();

        GameSessionEngine.SubmitMove(
            state,
            GameId,
            new SubmitGameMoveCommand(FirstPlayer.UserId, ExpectedRoundNumber: 1, GameMove.Greenium),
            RoundResolver,
            DateTime.UtcNow);

        GameSessionEngine.SubmitMove(
            state,
            GameId,
            new SubmitGameMoveCommand(FirstPlayer.UserId, ExpectedRoundNumber: 1, GameMove.Redite),
            RoundResolver,
            DateTime.UtcNow);

        var firstPlayerView = GameSessionEngine.SubmitMove(
            state,
            GameId,
            new SubmitGameMoveCommand(SecondPlayer.UserId, ExpectedRoundNumber: 1, GameMove.Bluon),
            RoundResolver,
            DateTime.UtcNow);

        var updatedFirstPlayerView = GameSessionEngine.GetPlayerView(
            state,
            GameId,
            new GetGameSessionPlayerViewQuery(FirstPlayer.UserId),
            RoundResolver);

        Assert.NotNull(updatedFirstPlayerView);
        Assert.Equal(GameMove.Redite, updatedFirstPlayerView!.LastResolvedRound!.Result.CurrentPlayerMove);
        Assert.Equal(GameRoundOutcome.OpponentWins, updatedFirstPlayerView.LastResolvedRound.Result.Outcome);
        Assert.Equal(GameRoundOutcome.CurrentPlayerWins, firstPlayerView.LastResolvedRound!.Result.Outcome);
    }

    private static GameSessionGrainState CreateInitializedState()
    {
        var state = new GameSessionGrainState();
        GameSessionEngine.Initialize(state, new InitializeGameSessionCommand(FirstPlayer, SecondPlayer));
        return state;
    }
}
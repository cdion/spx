using Spx.Contracts;
using Spx.Game.Domain;
using Xunit;

namespace Spx.Game.Application.Tests;

public sealed class GameRoundResolverTests
{
    private readonly GameRoundResolver resolver = new();

    [Theory]
    [InlineData(GameMove.Redite, GameMove.Redite)]
    [InlineData(GameMove.Greenium, GameMove.Greenium)]
    [InlineData(GameMove.Bluon, GameMove.Bluon)]
    public void Resolve_returns_draw_for_matching_moves(GameMove firstPlayerMove, GameMove secondPlayerMove)
    {
        var result = resolver.Resolve(firstPlayerMove, secondPlayerMove);

        Assert.Equal(GameRoundOutcome.Draw, result.Outcome);
    }

    [Theory]
    [InlineData(GameMove.Redite, GameMove.Greenium)]
    [InlineData(GameMove.Greenium, GameMove.Bluon)]
    [InlineData(GameMove.Bluon, GameMove.Redite)]
    public void Resolve_returns_first_player_win_for_winning_cycle(GameMove firstPlayerMove, GameMove secondPlayerMove)
    {
        var result = resolver.Resolve(firstPlayerMove, secondPlayerMove);

        Assert.Equal(GameRoundOutcome.CurrentPlayerWins, result.Outcome);
    }

    [Theory]
    [InlineData(GameMove.Greenium, GameMove.Redite)]
    [InlineData(GameMove.Bluon, GameMove.Greenium)]
    [InlineData(GameMove.Redite, GameMove.Bluon)]
    public void Resolve_returns_second_player_win_for_losing_cycle(GameMove firstPlayerMove, GameMove secondPlayerMove)
    {
        var result = resolver.Resolve(firstPlayerMove, secondPlayerMove);

        Assert.Equal(GameRoundOutcome.OpponentWins, result.Outcome);
    }
}
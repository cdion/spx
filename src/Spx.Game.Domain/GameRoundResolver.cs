using Spx.Contracts;

namespace Spx.Game.Domain;

public interface IGameRoundResolver
{
    GameRoundResult Resolve(GameMove firstPlayerMove, GameMove secondPlayerMove);
}

public sealed class GameRoundResolver : IGameRoundResolver
{
    public GameRoundResult Resolve(GameMove firstPlayerMove, GameMove secondPlayerMove)
        => new(firstPlayerMove, secondPlayerMove, ResolveOutcome(firstPlayerMove, secondPlayerMove));

    private static GameRoundOutcome ResolveOutcome(GameMove firstPlayerMove, GameMove secondPlayerMove)
    {
        if (firstPlayerMove == secondPlayerMove)
        {
            return GameRoundOutcome.Draw;
        }

        return (firstPlayerMove, secondPlayerMove) switch
        {
            (GameMove.Redite, GameMove.Greenium) => GameRoundOutcome.CurrentPlayerWins,
            (GameMove.Greenium, GameMove.Bluon) => GameRoundOutcome.CurrentPlayerWins,
            (GameMove.Bluon, GameMove.Redite) => GameRoundOutcome.CurrentPlayerWins,
            _ => GameRoundOutcome.OpponentWins
        };
    }
}
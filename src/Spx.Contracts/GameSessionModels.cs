using Orleans;

namespace Spx.Contracts;

public enum GameMove
{
    Redite = 0,
    Greenium = 1,
    Bluon = 2
}

public enum GameRoundOutcome
{
    Draw = 0,
    CurrentPlayerWins = 1,
    OpponentWins = 2
}

[GenerateSerializer]
public sealed record GameRoundResult(
    [property: Id(0)] GameMove CurrentPlayerMove,
    [property: Id(1)] GameMove OpponentMove,
    [property: Id(2)] GameRoundOutcome Outcome);

[GenerateSerializer]
public sealed record GameSessionParticipantView(
    [property: Id(0)] Guid PlayerId,
    [property: Id(1)] string UserId);

[GenerateSerializer]
public sealed record InitializeGameSessionCommand(
    [property: Id(0)] GameSessionParticipantView FirstPlayer,
    [property: Id(1)] GameSessionParticipantView SecondPlayer);

[GenerateSerializer]
public sealed record SubmitGameMoveCommand(
    [property: Id(0)] string UserId,
    [property: Id(1)] int ExpectedRoundNumber,
    [property: Id(2)] GameMove Move);

[GenerateSerializer]
public sealed record GetGameSessionViewQuery(
    [property: Id(0)] string UserId);

[GenerateSerializer]
public sealed record AbandonGameSessionCommand(
    [property: Id(0)] string UserId);

[GenerateSerializer]
public sealed record GameSessionRoundResult(
    [property: Id(0)] int RoundNumber,
    [property: Id(1)] GameRoundResult Result,
    [property: Id(2)] DateTime ResolvedAtUtc);

[GenerateSerializer]
public sealed record GameSessionView(
    [property: Id(0)] Guid GameId,
    [property: Id(1)] int RoundNumber,
    [property: Id(2)] GameSessionParticipantView CurrentPlayer,
    [property: Id(3)] GameSessionParticipantView OpponentPlayer,
    [property: Id(4)] bool HasSubmittedMove,
    [property: Id(5)] bool WaitingForOpponent,
    [property: Id(6)] GameSessionRoundResult? LastResolvedRound);
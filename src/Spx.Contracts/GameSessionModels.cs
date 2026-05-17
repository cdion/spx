using Orleans;

namespace Spx.Contracts;

public enum GamePhase
{
    Acquire = 0,
    Play = 1,
    Resolve = 2,
    Completed = 3
}

public enum GameCardDefinition
{
    Extract = 0,
    Refine = 1,
    Produce = 2,
    Red = 3,
    Yellow = 4,
    Blue = 5,
    Purple = 6,
    Green = 7,
    Orange = 8,
    Sabotage = 9,
    Replicate = 10,
    Catalyst = 11,
    Corrupt = 12,
    Reclaim = 13,
    Scout = 14,
    Victory = 15
}

public enum GameCardCategory
{
    Action = 0,
    Resource = 1,
    Effect = 2,
    Victory = 3
}

public enum GameResourceColor
{
    Red = 0,
    Yellow = 1,
    Blue = 2,
    Purple = 3,
    Green = 4,
    Orange = 5
}

public enum GameCompletionReason
{
    Victory = 0,
    Draw = 1,
    Abandoned = 2
}

public enum GameplayEventKind
{
    Fizzled = 0,
    DiscardedCard = 1,
    CreatedCard = 2,
    ConvertedCard = 3,
    ScheduledReturnToHand = 4,
    ReturnedToHand = 5,
    Resolved = 6
}

[GenerateSerializer]
public sealed record GameSessionParticipantGrainView(
    [property: Id(0)] Guid PlayerId,
    [property: Id(1)] string UserId);

[GenerateSerializer]
public sealed record InitializeGameSessionGrainCommand(
    [property: Id(0)] GameSessionParticipantGrainView FirstPlayer,
    [property: Id(1)] GameSessionParticipantGrainView SecondPlayer);

[GenerateSerializer]
public sealed record SubmitAcquireGrainCommand(
    [property: Id(0)] string UserId,
    [property: Id(1)] int ExpectedRoundNumber,
    [property: Id(2)] Guid MarketCardInstanceId);

[GenerateSerializer]
public sealed record GameCardReferenceGrainCommand(
    [property: Id(0)] Guid? CardInstanceId,
    [property: Id(1)] Guid? ProducedByCardInstanceId,
    [property: Id(2)] GameCardDefinition? ProducedCardDefinition);

[GenerateSerializer]
public sealed record GameBatchCardGrainCommand(
    [property: Id(0)] Guid CardInstanceId,
    [property: Id(1)] GameResourceColor? ChosenResourceColor,
    [property: Id(2)] GameCardDefinition? CraftedCardDefinition,
    [property: Id(3)] GameResourceColor? TargetResourceColor,
    [property: Id(4)] Guid? TargetCardInstanceId,
    [property: Id(5)] IReadOnlyList<GameCardReferenceGrainCommand> ConsumedCards);

[GenerateSerializer]
public sealed record SubmitPlayBatchGrainCommand(
    [property: Id(0)] string UserId,
    [property: Id(1)] int ExpectedRoundNumber,
    [property: Id(2)] IReadOnlyList<GameBatchCardGrainCommand> Cards);

[GenerateSerializer]
public sealed record GetGameSessionGrainQuery(
    [property: Id(0)] string UserId);

[GenerateSerializer]
public sealed record AbandonGameSessionGrainCommand(
    [property: Id(0)] string UserId);

[GenerateSerializer]
public sealed record GameCardInstanceGrainView(
    [property: Id(0)] Guid CardInstanceId,
    [property: Id(1)] GameCardDefinition Definition,
    [property: Id(2)] string DisplayName,
    [property: Id(3)] GameCardCategory Category,
    [property: Id(4)] GameResourceColor? ResourceColor);

[GenerateSerializer]
public sealed record GameCardReferenceGrainView(
    [property: Id(0)] Guid? CardInstanceId,
    [property: Id(1)] Guid? ProducedByCardInstanceId,
    [property: Id(2)] GameCardDefinition? ProducedCardDefinition);

[GenerateSerializer]
public sealed record GameBatchCardGrainView(
    [property: Id(0)] GameCardInstanceGrainView Card,
    [property: Id(1)] GameResourceColor? ChosenResourceColor,
    [property: Id(2)] GameCardDefinition? CraftedCardDefinition,
    [property: Id(3)] GameResourceColor? TargetResourceColor,
    [property: Id(4)] Guid? TargetCardInstanceId,
    [property: Id(5)] IReadOnlyList<GameCardReferenceGrainView> ConsumedCards);

[GenerateSerializer]
public sealed record GamePlayerStateGrainView(
    [property: Id(0)] GameSessionParticipantGrainView Participant,
    [property: Id(1)] IReadOnlyList<GameCardInstanceGrainView> Hand,
    [property: Id(2)] bool HasLockedBatch,
    [property: Id(3)] int LockedBatchCount,
    [property: Id(4)] int InitiativeScore,
    [property: Id(5)] bool HasScoutOverride,
    [property: Id(6)] bool PicksFirstInAcquirePhase,
    [property: Id(7)] IReadOnlyList<GameBatchCardGrainView> VisibleLockedCards);

[GenerateSerializer]
public sealed record GameResolvedPlayerBatchGrainView(
    [property: Id(0)] GameSessionParticipantGrainView Participant,
    [property: Id(1)] IReadOnlyList<GameBatchCardGrainView> PlayedCards,
    [property: Id(2)] bool ProducedVictory);

[GenerateSerializer]
public sealed record GameResolvedBatchGrainView(
    [property: Id(0)] int RoundNumber,
    [property: Id(1)] IReadOnlyList<GameResolvedPlayerBatchGrainView> Players,
    [property: Id(3)] DateTime ResolvedAtUtc);

[GenerateSerializer]
public sealed record GameCompletionGrainView(
    [property: Id(0)] GameCompletionReason Reason,
    [property: Id(1)] GameSessionParticipantGrainView? Winner,
    [property: Id(2)] DateTime CompletedAtUtc);

[GenerateSerializer]
public sealed record GameplayEvent(
    [property: Id(0)] GameplayEventKind Kind,
    [property: Id(1)] string ActorUserId,
    [property: Id(2)] GameCardDefinition SourceCardDefinition,
    [property: Id(3)] string? TargetUserId,
    [property: Id(4)] GameCardDefinition? TargetCardDefinition,
    [property: Id(5)] GameCardDefinition? ProducedCardDefinition);

[GenerateSerializer]
public sealed record GameSessionGrainView(
    [property: Id(0)] Guid GameId,
    [property: Id(1)] int RoundNumber,
    [property: Id(2)] GamePhase Phase,
    [property: Id(3)] GamePlayerStateGrainView CurrentPlayer,
    [property: Id(4)] GamePlayerStateGrainView OpponentPlayer,
    [property: Id(5)] IReadOnlyList<GameCardInstanceGrainView> VisibleMarketCards,
    [property: Id(6)] int MarketDeckCount,
    [property: Id(7)] bool WaitingForOpponent,
    [property: Id(8)] bool CanAcquireCard,
    [property: Id(9)] bool CanLockBatch,
    [property: Id(10)] int MaxBatchSize,
    [property: Id(11)] GameResolvedBatchGrainView? LastResolvedBatch,
    [property: Id(12)] GameCompletionGrainView? Completion);

[GenerateSerializer]
public sealed record PendingGameplayEventBatchGrainView(
    [property: Id(0)] Guid BatchId,
    [property: Id(1)] GameSessionGrainView Session,
    [property: Id(2)] IReadOnlyList<GameplayEvent> GameplayEvents);

[GenerateSerializer]
public sealed record AcknowledgeGameplayEventBatchesGrainCommand(
    [property: Id(0)] IReadOnlyList<Guid> BatchIds);

[GenerateSerializer]
public abstract record GameSessionGrainCommandResult;

[GenerateSerializer]
public sealed record GameSessionGrainCommandSucceededResult(
    [property: Id(0)] GameSessionGrainView Session,
    [property: Id(1)] IReadOnlyList<GameplayEvent> GameplayEvents,
    [property: Id(2)] Guid? PendingGameplayEventBatchId = null) : GameSessionGrainCommandResult
{
    public GameSessionGrainCommandSucceededResult(GameSessionGrainView Session) : this(Session, [])
    {
    }
}

[GenerateSerializer]
public sealed record GameSessionGrainCommandRejectedResult(
    [property: Id(0)] string ErrorMessage) : GameSessionGrainCommandResult;
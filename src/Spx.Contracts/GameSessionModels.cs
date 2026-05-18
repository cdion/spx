using Orleans;
using Spx.Game.Domain;

namespace Spx.Contracts;

[GenerateSerializer]
public sealed record GameSessionParticipantGrainView(
    [property: Id(0)] Guid PlayerId);

[GenerateSerializer]
public sealed record InitializeGameSessionGrainCommand(
    [property: Id(0)] GameSessionParticipantGrainView FirstPlayer,
    [property: Id(1)] GameSessionParticipantGrainView SecondPlayer);

[GenerateSerializer]
public sealed record SubmitAcquireGrainCommand(
    [property: Id(0)] Guid PlayerId,
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
    [property: Id(0)] Guid PlayerId,
    [property: Id(1)] int ExpectedRoundNumber,
    [property: Id(2)] IReadOnlyList<GameBatchCardGrainCommand> Cards);

[GenerateSerializer]
public sealed record GetGameSessionGrainQuery(
    [property: Id(0)] Guid PlayerId);

[GenerateSerializer]
public sealed record AbandonGameSessionGrainCommand(
    [property: Id(0)] Guid PlayerId);

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
    [property: Id(1)] Guid GameId,
    [property: Id(2)] GameResolvedBatchGrainView? LastResolvedBatch,
    [property: Id(3)] GameCompletionGrainView? Completion,
    [property: Id(4)] IReadOnlyList<GameplayEvent> GameplayEvents);

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
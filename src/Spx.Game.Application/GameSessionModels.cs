using Spx.Contracts;

namespace Spx.Game.Application;

public sealed record GameSessionParticipant(
    Guid PlayerId,
    string UserId);

public sealed record SubmitAcquireRequest(
    string UserId,
    int ExpectedRoundNumber,
    Guid MarketCardInstanceId);

public sealed record GameCardReferenceSelection(
    Guid? CardInstanceId,
    Guid? ProducedByCardInstanceId,
    GameCardDefinition? ProducedCardDefinition);

public sealed record GameBatchCardSelection(
    Guid CardInstanceId,
    GameResourceColor? ChosenResourceColor,
    GameCardDefinition? CraftedCardDefinition,
    GameResourceColor? TargetResourceColor,
    Guid? TargetCardInstanceId,
    IReadOnlyList<GameCardReferenceSelection> ConsumedCards);

public sealed record SubmitPlayBatchRequest(
    string UserId,
    int ExpectedRoundNumber,
    IReadOnlyList<GameBatchCardSelection> Cards);

public sealed record GameCardSnapshot(
    Guid CardInstanceId,
    GameCardDefinition Definition,
    string DisplayName,
    GameCardCategory Category,
    GameResourceColor? ResourceColor);

public sealed record GameCardReferenceSnapshot(
    Guid? CardInstanceId,
    Guid? ProducedByCardInstanceId,
    GameCardDefinition? ProducedCardDefinition);

public sealed record GameBatchCardSnapshot(
    GameCardSnapshot Card,
    GameResourceColor? ChosenResourceColor,
    GameCardDefinition? CraftedCardDefinition,
    GameResourceColor? TargetResourceColor,
    Guid? TargetCardInstanceId,
    IReadOnlyList<GameCardReferenceSnapshot> ConsumedCards);

public sealed record GamePlayerSnapshot(
    GameSessionParticipant Participant,
    IReadOnlyList<GameCardSnapshot> Hand,
    bool HasLockedBatch,
    int LockedBatchCount,
    int InitiativeScore,
    bool HasScoutOverride,
    bool PicksFirstInAcquirePhase,
    IReadOnlyList<GameBatchCardSnapshot> VisibleLockedCards);

public sealed record GameResolvedPlayerBatchSnapshot(
    GameSessionParticipant Participant,
    IReadOnlyList<GameBatchCardSnapshot> PlayedCards,
    bool ProducedVictory);

public sealed record GameResolvedBatchSnapshot(
    int RoundNumber,
    IReadOnlyList<GameResolvedPlayerBatchSnapshot> Players,
    DateTime ResolvedAtUtc);

public sealed record GameCompletionSnapshot(
    GameCompletionReason Reason,
    GameSessionParticipant? Winner,
    DateTime CompletedAtUtc);

public sealed record GameSessionSnapshot(
    Guid GameId,
    int RoundNumber,
    GamePhase Phase,
    GamePlayerSnapshot CurrentPlayer,
    GamePlayerSnapshot OpponentPlayer,
    IReadOnlyList<GameCardSnapshot> VisibleMarketCards,
    int MarketDeckCount,
    bool WaitingForOpponent,
    bool CanAcquireCard,
    bool CanLockBatch,
    int MaxBatchSize,
    GameResolvedBatchSnapshot? LastResolvedBatch,
    GameCompletionSnapshot? Completion);
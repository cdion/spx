namespace Spx.Game.Domain;

public sealed record GameSessionParticipant(
    Guid PlayerId);

public sealed record InitializeGameSessionCommand(
    GameSessionParticipant FirstPlayer,
    GameSessionParticipant SecondPlayer);

public sealed record SubmitAcquireCommand(
    Guid PlayerId,
    int ExpectedRoundNumber,
    Guid MarketCardInstanceId);

public sealed record GameCardReferenceCommand(
    Guid? CardInstanceId,
    Guid? ProducedByCardInstanceId,
    GameCardDefinition? ProducedCardDefinition);

public sealed record GameBatchCardCommand(
    Guid CardInstanceId,
    GameResourceColor? ChosenResourceColor,
    GameCardDefinition? CraftedCardDefinition,
    GameResourceColor? TargetResourceColor,
    Guid? TargetCardInstanceId,
    IReadOnlyList<GameCardReferenceCommand> ConsumedCards);

public sealed record SubmitPlayBatchCommand(
    Guid PlayerId,
    int ExpectedRoundNumber,
    IReadOnlyList<GameBatchCardCommand> Cards);

public sealed record GetGameSessionQuery(Guid PlayerId);

public sealed record AbandonGameSessionCommand(Guid PlayerId);

public sealed record GameCardView(
    Guid CardInstanceId,
    GameCardDefinition Definition,
    string DisplayName,
    GameCardCategory Category,
    GameResourceColor? ResourceColor);

public sealed record GameCardReferenceView(
    Guid? CardInstanceId,
    Guid? ProducedByCardInstanceId,
    GameCardDefinition? ProducedCardDefinition);

public sealed record GameBatchCardView(
    GameCardView Card,
    GameResourceColor? ChosenResourceColor,
    GameCardDefinition? CraftedCardDefinition,
    GameResourceColor? TargetResourceColor,
    Guid? TargetCardInstanceId,
    IReadOnlyList<GameCardReferenceView> ConsumedCards);

public sealed record GamePlayerStateView(
    GameSessionParticipant Participant,
    IReadOnlyList<GameCardView> Hand,
    bool HasLockedBatch,
    int LockedBatchCount,
    int InitiativeScore,
    bool HasScoutOverride,
    bool PicksFirstInAcquirePhase,
    IReadOnlyList<GameBatchCardView> VisibleLockedCards);

public sealed record GameResolvedPlayerBatchView(
    GameSessionParticipant Participant,
    IReadOnlyList<GameBatchCardView> PlayedCards,
    bool ProducedVictory);

public sealed record GameResolvedBatchView(
    int RoundNumber,
    IReadOnlyList<GameResolvedPlayerBatchView> Players,
    DateTime ResolvedAtUtc);

public sealed record GameCompletionView(
    GameCompletionReason Reason,
    GameSessionParticipant? Winner,
    DateTime CompletedAtUtc);

public sealed record GameSessionView(
    Guid GameId,
    int RoundNumber,
    GamePhase Phase,
    GamePlayerStateView CurrentPlayer,
    GamePlayerStateView OpponentPlayer,
    IReadOnlyList<GameCardView> VisibleMarketCards,
    int MarketDeckCount,
    bool WaitingForOpponent,
    bool CanAcquireCard,
    bool CanLockBatch,
    int MaxBatchSize,
    GameResolvedBatchView? LastResolvedBatch,
    GameCompletionView? Completion);

public abstract record GameSessionCommandResult;

public sealed record GameSessionCommandSucceededResult(
    GameSessionView Session,
    IReadOnlyList<GameplayEvent> GameplayEvents) : GameSessionCommandResult
{
    public GameSessionCommandSucceededResult(GameSessionView Session) : this(Session, [])
    {
    }
}

public sealed record GameSessionCommandRejectedResult(string ErrorMessage) : GameSessionCommandResult;
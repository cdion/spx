using Orleans;
using Spx.Contracts;

namespace Spx.Grains;

[GenerateSerializer]
public sealed class GameSessionGrainState
{
    [Id(0)] public GameSessionParticipantGrainView? FirstPlayer { get; set; }

    [Id(1)] public GameSessionParticipantGrainView? SecondPlayer { get; set; }

    [Id(2)] public bool FirstPlayerActive { get; set; }

    [Id(3)] public bool SecondPlayerActive { get; set; }

    [Id(4)] public int RoundNumber { get; set; } = 1;

    [Id(5)] public GamePhase Phase { get; set; } = GamePhase.Acquire;

    [Id(6)] public List<GameSessionGrainCardState> MarketDeck { get; set; } = [];

    [Id(7)] public List<GameSessionGrainCardState> VisibleMarketCards { get; set; } = [];

    [Id(8)] public List<GameSessionGrainCardState> FirstPlayerHand { get; set; } = [];

    [Id(9)] public List<GameSessionGrainCardState> SecondPlayerHand { get; set; } = [];

    [Id(10)] public GameSessionPendingBatchGrainState? FirstPlayerPendingBatch { get; set; }

    [Id(11)] public GameSessionPendingBatchGrainState? SecondPlayerPendingBatch { get; set; }

    [Id(12)] public GameSessionResolvedBatchGrainState? LastResolvedBatch { get; set; }

    [Id(13)] public bool FirstPlayerScoutOverride { get; set; }

    [Id(14)] public bool SecondPlayerScoutOverride { get; set; }

    [Id(15)] public Guid? CurrentAcquireFirstPlayerId { get; set; }

    [Id(16)] public Guid? CurrentAcquireSecondPlayerId { get; set; }

    [Id(19)] public Guid? PreviousAcquireSecondPlayerId { get; set; }

    [Id(20)] public Guid? InitialTieBreakerFirstPlayerId { get; set; }

    [Id(21)] public GameSessionCompletionGrainState? Completion { get; set; }

    [Id(22)] public int ConsecutiveStalemateRounds { get; set; }

    [Id(23)] public bool RoundHadHandChange { get; set; }

    [Id(24)] public int AcquirePicksCompletedInPhase { get; set; }

    [Id(25)] public List<PendingGameplayEventBatchGrainState> PendingGameplayEventBatches { get; set; } = [];
}

[GenerateSerializer]
public sealed class GameSessionGrainCardState
{
    [Id(0)] public Guid CardInstanceId { get; set; }

    [Id(1)] public GameCardDefinition Definition { get; set; }
}

[GenerateSerializer]
public sealed class GameSessionCardReferenceGrainState
{
    [Id(0)] public Guid? CardInstanceId { get; set; }

    [Id(1)] public Guid? ProducedByCardInstanceId { get; set; }

    [Id(2)] public GameCardDefinition? ProducedCardDefinition { get; set; }
}

[GenerateSerializer]
public sealed class GameSessionPendingBatchGrainState
{
    [Id(0)] public Guid PlayerId { get; set; }

    [Id(1)] public List<GameSessionPendingBatchCardGrainState> Cards { get; set; } = [];
}

[GenerateSerializer]
public sealed class GameSessionPendingBatchCardGrainState
{
    [Id(0)] public GameSessionGrainCardState Card { get; set; } = new();

    [Id(1)] public GameResourceColor? ChosenResourceColor { get; set; }

    [Id(2)] public GameCardDefinition? CraftedCardDefinition { get; set; }

    [Id(3)] public GameResourceColor? TargetResourceColor { get; set; }

    [Id(4)] public Guid? TargetCardInstanceId { get; set; }

    [Id(5)] public List<GameSessionCardReferenceGrainState> ConsumedCards { get; set; } = [];

    [Id(6)] public bool ReturnToHand { get; set; }
}

[GenerateSerializer]
public sealed class GameSessionResolvedBatchGrainState
{
    [Id(0)] public int RoundNumber { get; set; }

    [Id(1)] public List<GameSessionResolvedPlayerBatchGrainState> Players { get; set; } = [];

    [Id(2)] public DateTime ResolvedAtUtc { get; set; }
}

[GenerateSerializer]
public sealed class GameSessionResolvedPlayerBatchGrainState
{
    [Id(0)] public Guid PlayerId { get; set; }

    [Id(1)] public List<GameSessionPendingBatchCardGrainState> Cards { get; set; } = [];

    [Id(2)] public bool ProducedVictory { get; set; }
}

[GenerateSerializer]
public sealed class GameSessionCompletionGrainState
{
    [Id(0)] public GameCompletionReason Reason { get; set; }

    [Id(1)] public Guid? WinnerPlayerId { get; set; }

    [Id(2)] public DateTime CompletedAtUtc { get; set; }
}

[GenerateSerializer]
public sealed class PendingGameplayEventBatchGrainState
{
    [Id(0)] public Guid BatchId { get; set; }

    [Id(1)] public Guid GameId { get; set; }

    [Id(2)] public GameResolvedBatchGrainView? LastResolvedBatch { get; set; }

    [Id(3)] public GameCompletionGrainView? Completion { get; set; }

    [Id(4)] public List<GameplayEvent> GameplayEvents { get; set; } = [];
}
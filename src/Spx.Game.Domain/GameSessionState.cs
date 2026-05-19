namespace Spx.Game.Domain;

public sealed class GameSessionState
{
    public GameSessionParticipant? FirstPlayer { get; set; }

    public GameSessionParticipant? SecondPlayer { get; set; }

    public bool FirstPlayerActive { get; set; }

    public bool SecondPlayerActive { get; set; }

    public int RoundNumber { get; set; } = 1;

    public GamePhase Phase { get; set; } = GamePhase.Acquire;

    public List<GameCardState> MarketDeck { get; set; } = [];

    public List<GameCardState> VisibleMarketCards { get; set; } = [];

    public List<GameCardState> FirstPlayerHand { get; set; } = [];

    public List<GameCardState> SecondPlayerHand { get; set; } = [];

    public PendingGameBatchState? FirstPlayerPendingBatch { get; set; }

    public PendingGameBatchState? SecondPlayerPendingBatch { get; set; }

    public ResolvedGameBatchState? LastResolvedBatch { get; set; }

    public bool FirstPlayerScoutOverride { get; set; }

    public bool SecondPlayerScoutOverride { get; set; }

    public Guid? CurrentAcquireFirstPlayerId { get; set; }

    public Guid? CurrentAcquireSecondPlayerId { get; set; }

    public Guid? PreviousAcquireSecondPlayerId { get; set; }

    public Guid? InitialTieBreakerFirstPlayerId { get; set; }

    public GameCompletionState? Completion { get; set; }

    public int ConsecutiveStalemateRounds { get; set; }

    public bool RoundHadHandChange { get; set; }

    public int AcquirePicksCompletedInPhase { get; set; }
}

public sealed class GameCardState
{
    public Guid CardInstanceId { get; set; }

    public GameCardDefinition Definition { get; set; }
}

public sealed class GameCardReferenceState
{
    public Guid? CardInstanceId { get; set; }

    public Guid? ProducedByCardInstanceId { get; set; }

    public GameCardDefinition? ProducedCardDefinition { get; set; }
}

public sealed class PendingGameBatchState
{
    public Guid PlayerId { get; set; }

    public List<PendingGameBatchCardState> Cards { get; set; } = [];
}

public sealed class PendingGameBatchCardState
{
    public GameCardState Card { get; set; } = new();

    public GameResourceColor? ChosenResourceColor { get; set; }

    public GameCardDefinition? CraftedCardDefinition { get; set; }

    public GameResourceColor? TargetResourceColor { get; set; }

    public Guid? TargetCardInstanceId { get; set; }

    public List<GameCardReferenceState> ConsumedCards { get; set; } = [];

    public bool ReturnToHand { get; set; }
}

public sealed class ResolvedGameBatchState
{
    public int RoundNumber { get; set; }

    public List<ResolvedGamePlayerBatchState> Players { get; set; } = [];

    public DateTime ResolvedAtUtc { get; set; }
}

public sealed class ResolvedGamePlayerBatchState
{
    public Guid PlayerId { get; set; }

    public List<PendingGameBatchCardState> Cards { get; set; } = [];

    public bool ProducedVictory { get; set; }
}

public sealed class GameCompletionState
{
    public GameCompletionReason Reason { get; set; }

    public Guid? WinnerPlayerId { get; set; }

    public DateTime CompletedAtUtc { get; set; }
}

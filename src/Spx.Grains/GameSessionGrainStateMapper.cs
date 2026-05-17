using Spx.Game.Domain;

namespace Spx.Grains;

internal static class GameSessionGrainStateMapper
{
    public static GameSessionState ToDomainState(GameSessionGrainState state)
        => new()
        {
            FirstPlayer = state.FirstPlayer is null ? null : new(state.FirstPlayer.PlayerId, state.FirstPlayer.UserId),
            SecondPlayer = state.SecondPlayer is null ? null : new(state.SecondPlayer.PlayerId, state.SecondPlayer.UserId),
            FirstPlayerActive = state.FirstPlayerActive,
            SecondPlayerActive = state.SecondPlayerActive,
            RoundNumber = state.RoundNumber,
            Phase = state.Phase,
            MarketDeck = state.MarketDeck.Select(ToDomainCard).ToList(),
            VisibleMarketCards = state.VisibleMarketCards.Select(ToDomainCard).ToList(),
            FirstPlayerHand = state.FirstPlayerHand.Select(ToDomainCard).ToList(),
            SecondPlayerHand = state.SecondPlayerHand.Select(ToDomainCard).ToList(),
            FirstPlayerPendingBatch = state.FirstPlayerPendingBatch is null ? null : ToDomainPendingBatch(state.FirstPlayerPendingBatch),
            SecondPlayerPendingBatch = state.SecondPlayerPendingBatch is null ? null : ToDomainPendingBatch(state.SecondPlayerPendingBatch),
            LastResolvedBatch = state.LastResolvedBatch is null ? null : ToDomainResolvedBatch(state.LastResolvedBatch),
            FirstPlayerScoutOverride = state.FirstPlayerScoutOverride,
            SecondPlayerScoutOverride = state.SecondPlayerScoutOverride,
            CurrentAcquireFirstUserId = state.CurrentAcquireFirstUserId,
            CurrentAcquireSecondUserId = state.CurrentAcquireSecondUserId,
            AcquireFirstCompleted = state.AcquireFirstCompleted,
            AcquireSecondCompleted = state.AcquireSecondCompleted,
            PreviousAcquireSecondUserId = state.PreviousAcquireSecondUserId,
            InitialTieBreakerFirstUserId = state.InitialTieBreakerFirstUserId,
            Completion = state.Completion is null ? null : ToDomainCompletion(state.Completion),
            ConsecutiveStalemateRounds = state.ConsecutiveStalemateRounds,
            RoundHadHandChange = state.RoundHadHandChange,
            AcquirePicksCompletedInPhase = state.AcquirePicksCompletedInPhase
        };

    public static GameSessionGrainState FromDomainState(
        GameSessionState state,
        IEnumerable<PendingGameplayEventBatchGrainState>? pendingGameplayEventBatches = null)
        => new()
        {
            FirstPlayer = state.FirstPlayer is null ? null : new(state.FirstPlayer.PlayerId, state.FirstPlayer.UserId),
            SecondPlayer = state.SecondPlayer is null ? null : new(state.SecondPlayer.PlayerId, state.SecondPlayer.UserId),
            FirstPlayerActive = state.FirstPlayerActive,
            SecondPlayerActive = state.SecondPlayerActive,
            RoundNumber = state.RoundNumber,
            Phase = state.Phase,
            MarketDeck = state.MarketDeck.Select(FromDomainCard).ToList(),
            VisibleMarketCards = state.VisibleMarketCards.Select(FromDomainCard).ToList(),
            FirstPlayerHand = state.FirstPlayerHand.Select(FromDomainCard).ToList(),
            SecondPlayerHand = state.SecondPlayerHand.Select(FromDomainCard).ToList(),
            FirstPlayerPendingBatch = state.FirstPlayerPendingBatch is null ? null : FromDomainPendingBatch(state.FirstPlayerPendingBatch),
            SecondPlayerPendingBatch = state.SecondPlayerPendingBatch is null ? null : FromDomainPendingBatch(state.SecondPlayerPendingBatch),
            LastResolvedBatch = state.LastResolvedBatch is null ? null : FromDomainResolvedBatch(state.LastResolvedBatch),
            FirstPlayerScoutOverride = state.FirstPlayerScoutOverride,
            SecondPlayerScoutOverride = state.SecondPlayerScoutOverride,
            CurrentAcquireFirstUserId = state.CurrentAcquireFirstUserId,
            CurrentAcquireSecondUserId = state.CurrentAcquireSecondUserId,
            AcquireFirstCompleted = state.AcquireFirstCompleted,
            AcquireSecondCompleted = state.AcquireSecondCompleted,
            PreviousAcquireSecondUserId = state.PreviousAcquireSecondUserId,
            InitialTieBreakerFirstUserId = state.InitialTieBreakerFirstUserId,
            Completion = state.Completion is null ? null : FromDomainCompletion(state.Completion),
            ConsecutiveStalemateRounds = state.ConsecutiveStalemateRounds,
            RoundHadHandChange = state.RoundHadHandChange,
            AcquirePicksCompletedInPhase = state.AcquirePicksCompletedInPhase,
            PendingGameplayEventBatches = pendingGameplayEventBatches?.Select(ClonePendingGameplayEventBatch).ToList() ?? []
        };

    private static GameCardState ToDomainCard(GameSessionGrainCardState card)
        => new() { CardInstanceId = card.CardInstanceId, Definition = card.Definition };

    private static GameSessionGrainCardState FromDomainCard(GameCardState card)
        => new() { CardInstanceId = card.CardInstanceId, Definition = card.Definition };

    private static GameCardReferenceState ToDomainReference(GameSessionCardReferenceGrainState reference)
        => new()
        {
            CardInstanceId = reference.CardInstanceId,
            ProducedByCardInstanceId = reference.ProducedByCardInstanceId,
            ProducedCardDefinition = reference.ProducedCardDefinition
        };

    private static GameSessionCardReferenceGrainState FromDomainReference(GameCardReferenceState reference)
        => new()
        {
            CardInstanceId = reference.CardInstanceId,
            ProducedByCardInstanceId = reference.ProducedByCardInstanceId,
            ProducedCardDefinition = reference.ProducedCardDefinition
        };

    private static PendingGameBatchState ToDomainPendingBatch(GameSessionPendingBatchGrainState batch)
        => new()
        {
            UserId = batch.UserId,
            Cards = batch.Cards.Select(ToDomainPendingBatchCard).ToList()
        };

    private static GameSessionPendingBatchGrainState FromDomainPendingBatch(PendingGameBatchState batch)
        => new()
        {
            UserId = batch.UserId,
            Cards = batch.Cards.Select(FromDomainPendingBatchCard).ToList()
        };

    private static PendingGameBatchCardState ToDomainPendingBatchCard(GameSessionPendingBatchCardGrainState card)
        => new()
        {
            Card = ToDomainCard(card.Card),
            ChosenResourceColor = card.ChosenResourceColor,
            CraftedCardDefinition = card.CraftedCardDefinition,
            TargetResourceColor = card.TargetResourceColor,
            TargetCardInstanceId = card.TargetCardInstanceId,
            ConsumedCards = card.ConsumedCards.Select(ToDomainReference).ToList(),
            ReturnToHand = card.ReturnToHand
        };

    private static GameSessionPendingBatchCardGrainState FromDomainPendingBatchCard(PendingGameBatchCardState card)
        => new()
        {
            Card = FromDomainCard(card.Card),
            ChosenResourceColor = card.ChosenResourceColor,
            CraftedCardDefinition = card.CraftedCardDefinition,
            TargetResourceColor = card.TargetResourceColor,
            TargetCardInstanceId = card.TargetCardInstanceId,
            ConsumedCards = card.ConsumedCards.Select(FromDomainReference).ToList(),
            ReturnToHand = card.ReturnToHand
        };

    private static ResolvedGameBatchState ToDomainResolvedBatch(GameSessionResolvedBatchGrainState batch)
        => new()
        {
            RoundNumber = batch.RoundNumber,
            Players = batch.Players.Select(ToDomainResolvedPlayerBatch).ToList(),
            ResolvedAtUtc = batch.ResolvedAtUtc
        };

    private static GameSessionResolvedBatchGrainState FromDomainResolvedBatch(ResolvedGameBatchState batch)
        => new()
        {
            RoundNumber = batch.RoundNumber,
            Players = batch.Players.Select(FromDomainResolvedPlayerBatch).ToList(),
            ResolvedAtUtc = batch.ResolvedAtUtc
        };

    private static ResolvedGamePlayerBatchState ToDomainResolvedPlayerBatch(GameSessionResolvedPlayerBatchGrainState batch)
        => new()
        {
            UserId = batch.UserId,
            Cards = batch.Cards.Select(ToDomainPendingBatchCard).ToList(),
            ProducedVictory = batch.ProducedVictory
        };

    private static GameSessionResolvedPlayerBatchGrainState FromDomainResolvedPlayerBatch(ResolvedGamePlayerBatchState batch)
        => new()
        {
            UserId = batch.UserId,
            Cards = batch.Cards.Select(FromDomainPendingBatchCard).ToList(),
            ProducedVictory = batch.ProducedVictory
        };

    private static GameCompletionState ToDomainCompletion(GameSessionCompletionGrainState completion)
        => new()
        {
            Reason = completion.Reason,
            WinnerUserId = completion.WinnerUserId,
            CompletedAtUtc = completion.CompletedAtUtc
        };

    private static GameSessionCompletionGrainState FromDomainCompletion(GameCompletionState completion)
        => new()
        {
            Reason = completion.Reason,
            WinnerUserId = completion.WinnerUserId,
            CompletedAtUtc = completion.CompletedAtUtc
        };

    private static PendingGameplayEventBatchGrainState ClonePendingGameplayEventBatch(PendingGameplayEventBatchGrainState batch)
        => new()
        {
            BatchId = batch.BatchId,
            Session = batch.Session,
            GameplayEvents = [.. batch.GameplayEvents]
        };
}
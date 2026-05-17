using Spx.Contracts;

namespace Spx.Game.Domain;

public static class GameSessionEngine
{
    private const int AcquirePicksPerPhase = 4;

    private static readonly GameCardDefinition[] InitialMarketDeck =
    [
        GameCardDefinition.Extract,
        GameCardDefinition.Extract,
        GameCardDefinition.Extract,
        GameCardDefinition.Extract,
        GameCardDefinition.Extract,
        GameCardDefinition.Extract,
        GameCardDefinition.Extract,
        GameCardDefinition.Extract,
        GameCardDefinition.Extract,
        GameCardDefinition.Extract,
        GameCardDefinition.Extract,
        GameCardDefinition.Extract,
        GameCardDefinition.Refine,
        GameCardDefinition.Refine,
        GameCardDefinition.Refine,
        GameCardDefinition.Refine,
        GameCardDefinition.Refine,
        GameCardDefinition.Refine,
        GameCardDefinition.Produce,
        GameCardDefinition.Produce,
        GameCardDefinition.Produce,
        GameCardDefinition.Produce,
        GameCardDefinition.Produce,
        GameCardDefinition.Produce
    ];

    public static GameSessionState CloneState(GameSessionState state)
        => new()
        {
            FirstPlayer = state.FirstPlayer is null ? null : new GameSessionParticipantGrainView(state.FirstPlayer.PlayerId, state.FirstPlayer.UserId),
            SecondPlayer = state.SecondPlayer is null ? null : new GameSessionParticipantGrainView(state.SecondPlayer.PlayerId, state.SecondPlayer.UserId),
            FirstPlayerActive = state.FirstPlayerActive,
            SecondPlayerActive = state.SecondPlayerActive,
            RoundNumber = state.RoundNumber,
            Phase = state.Phase,
            MarketDeck = state.MarketDeck.Select(CloneCard).ToList(),
            VisibleMarketCards = state.VisibleMarketCards.Select(CloneCard).ToList(),
            FirstPlayerHand = state.FirstPlayerHand.Select(CloneCard).ToList(),
            SecondPlayerHand = state.SecondPlayerHand.Select(CloneCard).ToList(),
            FirstPlayerPendingBatch = state.FirstPlayerPendingBatch is null ? null : ClonePendingBatch(state.FirstPlayerPendingBatch),
            SecondPlayerPendingBatch = state.SecondPlayerPendingBatch is null ? null : ClonePendingBatch(state.SecondPlayerPendingBatch),
            LastResolvedBatch = state.LastResolvedBatch is null ? null : CloneResolvedBatch(state.LastResolvedBatch),
            FirstPlayerScoutOverride = state.FirstPlayerScoutOverride,
            SecondPlayerScoutOverride = state.SecondPlayerScoutOverride,
            CurrentAcquireFirstUserId = state.CurrentAcquireFirstUserId,
            CurrentAcquireSecondUserId = state.CurrentAcquireSecondUserId,
            AcquireFirstCompleted = state.AcquireFirstCompleted,
            AcquireSecondCompleted = state.AcquireSecondCompleted,
            PreviousAcquireSecondUserId = state.PreviousAcquireSecondUserId,
            InitialTieBreakerFirstUserId = state.InitialTieBreakerFirstUserId,
            Completion = state.Completion is null
                ? null
                : new GameCompletionState
                {
                    Reason = state.Completion.Reason,
                    WinnerUserId = state.Completion.WinnerUserId,
                    CompletedAtUtc = state.Completion.CompletedAtUtc
                },
            ConsecutiveStalemateRounds = state.ConsecutiveStalemateRounds,
            RoundHadHandChange = state.RoundHadHandChange,
            AcquirePicksCompletedInPhase = state.AcquirePicksCompletedInPhase
        };

    public static void Initialize(GameSessionState state, InitializeGameSessionGrainCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        if (command.FirstPlayer.PlayerId == command.SecondPlayer.PlayerId)
        {
            throw new InvalidOperationException("A game session requires two distinct players.");
        }

        if (state.FirstPlayer is not null
            && state.SecondPlayer is not null
            && HasSameRoster(state.FirstPlayer, state.SecondPlayer, command.FirstPlayer, command.SecondPlayer))
        {
            state.FirstPlayer = command.FirstPlayer;
            state.SecondPlayer = command.SecondPlayer;
            state.FirstPlayerActive = true;
            state.SecondPlayerActive = true;
            return;
        }

        state.FirstPlayer = command.FirstPlayer;
        state.SecondPlayer = command.SecondPlayer;
        state.FirstPlayerActive = true;
        state.SecondPlayerActive = true;
        state.RoundNumber = 1;
        state.Phase = GamePhase.Acquire;
        state.MarketDeck = CreateInitialMarketDeck();
        state.VisibleMarketCards = [];
        state.FirstPlayerHand = [];
        state.SecondPlayerHand = [];
        state.FirstPlayerPendingBatch = null;
        state.SecondPlayerPendingBatch = null;
        state.LastResolvedBatch = null;
        state.FirstPlayerScoutOverride = false;
        state.SecondPlayerScoutOverride = false;
        state.PreviousAcquireSecondUserId = null;
        state.InitialTieBreakerFirstUserId = Random.Shared.Next(2) == 0 ? command.FirstPlayer.UserId : command.SecondPlayer.UserId;
        state.Completion = null;
        state.ConsecutiveStalemateRounds = 0;
        state.AcquirePicksCompletedInPhase = 0;
        StartAcquirePhase(state);
    }

    public static GameSessionGrainCommandResult SubmitAcquire(
        GameSessionState state,
        Guid gameId,
        SubmitAcquireGrainCommand command)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(command);

            EnsureInitialized(state);
            EnsureNotCompleted(state);

            var participant = GetParticipant(state, command.UserId);
            if (!participant.IsActive)
            {
                throw new InvalidOperationException("Inactive players cannot acquire cards.");
            }

            if (command.ExpectedRoundNumber != state.RoundNumber)
            {
                throw new InvalidOperationException("The submitted acquire pick does not match the current round.");
            }

            if (state.Phase != GamePhase.Acquire)
            {
                throw new InvalidOperationException("The game is not in the acquire phase.");
            }

            if (state.VisibleMarketCards.Count == 0)
            {
                state.AcquirePicksCompletedInPhase = AcquirePicksPerPhase;
                state.Phase = GamePhase.Play;
                return new GameSessionGrainCommandSucceededResult(CreatePlayerView(state, gameId, participant.Player.UserId));
            }

            var currentAcquireUserId = GetCurrentAcquireUserId(state);
            if (!string.Equals(currentAcquireUserId, command.UserId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("It is not this player's turn to acquire a card.");
            }

            var marketCard = state.VisibleMarketCards.FirstOrDefault(card => card.CardInstanceId == command.MarketCardInstanceId)
                ?? throw new InvalidOperationException("The selected market card is no longer available.");

            state.VisibleMarketCards.Remove(marketCard);
            AddCardToHand(state, participant.IsFirstPlayer, marketCard);

            state.AcquirePicksCompletedInPhase++;

            if (state.VisibleMarketCards.Count == 0 || state.AcquirePicksCompletedInPhase >= AcquirePicksPerPhase)
            {
                state.PreviousAcquireSecondUserId = state.CurrentAcquireSecondUserId;
                state.Phase = GamePhase.Play;
            }

            return new GameSessionGrainCommandSucceededResult(CreatePlayerView(state, gameId, participant.Player.UserId));
        }
        catch (InvalidOperationException exception)
        {
            return new GameSessionGrainCommandRejectedResult(exception.Message);
        }
    }

    public static GameSessionGrainCommandResult SubmitPlayBatch(
        GameSessionState state,
        Guid gameId,
        SubmitPlayBatchGrainCommand command,
        DateTime nowUtc)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(command);

            EnsureInitialized(state);
            EnsureNotCompleted(state);

            var participant = GetParticipant(state, command.UserId);
            if (!participant.IsActive)
            {
                throw new InvalidOperationException("Inactive players cannot submit play batches.");
            }

            if (command.ExpectedRoundNumber != state.RoundNumber)
            {
                throw new InvalidOperationException("The submitted play batch does not match the current round.");
            }

            if (state.Phase != GamePhase.Play)
            {
                throw new InvalidOperationException("The game is not in the play phase.");
            }

            if (GetPendingBatch(state, participant.IsFirstPlayer) is not null)
            {
                throw new InvalidOperationException("This player's batch is already locked for the current round.");
            }

            var ownHand = GetHand(state, participant.IsFirstPlayer);
            var opponentHand = GetHand(state, !participant.IsFirstPlayer);
            var pendingBatch = BuildPendingBatch(command, ownHand, opponentHand);

            foreach (var playedCard in pendingBatch.Cards)
            {
                if (!TryRemoveCardFromHand(ownHand, playedCard.Card.CardInstanceId, out _))
                {
                    throw new InvalidOperationException("A selected card is no longer in this player's hand.");
                }

                state.RoundHadHandChange = true;
            }

            SetPendingBatch(state, participant.IsFirstPlayer, pendingBatch);

            IReadOnlyList<GameplayEvent> gameplayEvents = [];

            if (state.FirstPlayerPendingBatch is not null && state.SecondPlayerPendingBatch is not null)
            {
                state.Phase = GamePhase.Resolve;
                gameplayEvents = ResolveRound(state, nowUtc);

                if (state.Completion is null)
                {
                    state.RoundNumber++;
                    StartAcquirePhase(state);
                }
                else
                {
                    state.Phase = GamePhase.Completed;
                }
            }

            return new GameSessionGrainCommandSucceededResult(CreatePlayerView(state, gameId, participant.Player.UserId), gameplayEvents);
        }
        catch (InvalidOperationException exception)
        {
            return new GameSessionGrainCommandRejectedResult(exception.Message);
        }
    }

    public static GameSessionGrainView? GetSessionView(
        GameSessionState state,
        Guid gameId,
        GetGameSessionGrainQuery query)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(query);

        if (state.FirstPlayer is null || state.SecondPlayer is null)
        {
            return null;
        }

        return TryGetParticipant(state, query.UserId) is { } participant
            ? CreatePlayerView(state, gameId, participant.Player.UserId)
            : null;
    }

    public static GameSessionGrainView AbandonPlayer(
        GameSessionState state,
        Guid gameId,
        AbandonGameSessionGrainCommand command,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        EnsureInitialized(state);

        var participant = GetParticipant(state, command.UserId);
        if (participant.IsFirstPlayer)
        {
            state.FirstPlayerActive = false;
        }
        else
        {
            state.SecondPlayerActive = false;
        }

        state.FirstPlayerPendingBatch = null;
        state.SecondPlayerPendingBatch = null;
        state.Phase = GamePhase.Completed;
        state.Completion = new GameCompletionState
        {
            Reason = GameCompletionReason.Abandoned,
            WinnerUserId = participant.Opponent.UserId,
            CompletedAtUtc = nowUtc
        };

        return CreatePlayerView(state, gameId, participant.Player.UserId);
    }

    private static bool HasSameRoster(
        GameSessionParticipantGrainView existingFirstPlayer,
        GameSessionParticipantGrainView existingSecondPlayer,
        GameSessionParticipantGrainView incomingFirstPlayer,
        GameSessionParticipantGrainView incomingSecondPlayer)
        => (existingFirstPlayer.PlayerId == incomingFirstPlayer.PlayerId && existingSecondPlayer.PlayerId == incomingSecondPlayer.PlayerId)
            || (existingFirstPlayer.PlayerId == incomingSecondPlayer.PlayerId && existingSecondPlayer.PlayerId == incomingFirstPlayer.PlayerId);

    private static void EnsureInitialized(GameSessionState state)
    {
        if (state.FirstPlayer is null || state.SecondPlayer is null)
        {
            throw new InvalidOperationException("The game session has not been initialized.");
        }
    }

    private static void EnsureNotCompleted(GameSessionState state)
    {
        if (state.Completion is not null)
        {
            throw new InvalidOperationException("The game session is already complete.");
        }
    }

    private static ParticipantState GetParticipant(GameSessionState state, string userId)
        => TryGetParticipant(state, userId) ?? throw new InvalidOperationException("The current user is not part of this game session.");

    private static ParticipantState? TryGetParticipant(GameSessionState state, string userId)
    {
        if (state.FirstPlayer is not null && string.Equals(state.FirstPlayer.UserId, userId, StringComparison.Ordinal))
        {
            return new ParticipantState(state.FirstPlayer, state.SecondPlayer!, state.FirstPlayerActive, IsFirstPlayer: true);
        }

        if (state.SecondPlayer is not null && string.Equals(state.SecondPlayer.UserId, userId, StringComparison.Ordinal))
        {
            return new ParticipantState(state.SecondPlayer, state.FirstPlayer!, state.SecondPlayerActive, IsFirstPlayer: false);
        }

        return null;
    }

    private static GameSessionGrainView CreatePlayerView(
        GameSessionState state,
        Guid gameId,
        string userId)
    {
        var participant = GetParticipant(state, userId);
        var currentPendingBatch = GetPendingBatch(state, participant.IsFirstPlayer);
        var opponentPendingBatch = GetPendingBatch(state, !participant.IsFirstPlayer);
        var canAcquireCard = state.Phase == GamePhase.Acquire
            && state.VisibleMarketCards.Count > 0
            && string.Equals(GetCurrentAcquireUserId(state), userId, StringComparison.Ordinal);
        var canLockBatch = state.Phase == GamePhase.Play && currentPendingBatch is null && participant.IsActive;
        var waitingForOpponent = state.Phase switch
        {
            GamePhase.Acquire => participant.IsActive
                && !canAcquireCard
                && state.VisibleMarketCards.Count > 0
                && (state.CurrentAcquireFirstUserId is not null || state.CurrentAcquireSecondUserId is not null),
            GamePhase.Play => currentPendingBatch is not null && opponentPendingBatch is null && (participant.IsFirstPlayer ? state.SecondPlayerActive : state.FirstPlayerActive),
            _ => false
        };

        return new GameSessionGrainView(
            gameId,
            state.RoundNumber,
            state.Phase,
            CreatePlayerStateView(state, participant.Player, participant.IsFirstPlayer, currentPendingBatch, revealLockedCards: true),
            CreatePlayerStateView(state, participant.Opponent, !participant.IsFirstPlayer, opponentPendingBatch, revealLockedCards: false),
            state.VisibleMarketCards.Select(CreateCardView).ToArray(),
            state.MarketDeck.Count,
            waitingForOpponent,
            canAcquireCard,
            canLockBatch,
            GameCardCatalog.MaxBatchSize,
            CreateResolvedBatchView(state),
            CreateCompletionView(state));
    }

    private sealed record ParticipantState(GameSessionParticipantGrainView Player, GameSessionParticipantGrainView Opponent, bool IsActive, bool IsFirstPlayer);

    private static GamePlayerStateGrainView CreatePlayerStateView(
        GameSessionState state,
        GameSessionParticipantGrainView participant,
        bool isFirstPlayer,
        PendingGameBatchState? pendingBatch,
        bool revealLockedCards)
        => new(
            participant,
            GetHand(state, isFirstPlayer).Select(CreateCardView).ToArray(),
            pendingBatch is not null,
            pendingBatch?.Cards.Count ?? 0,
            GetInitiativeScore(GetHand(state, isFirstPlayer)),
            isFirstPlayer ? state.FirstPlayerScoutOverride : state.SecondPlayerScoutOverride,
            string.Equals(state.CurrentAcquireFirstUserId, participant.UserId, StringComparison.Ordinal),
            revealLockedCards && pendingBatch is not null
                ? pendingBatch.Cards.Select(CreateBatchCardView).ToArray()
                : []);

    private static GameResolvedBatchGrainView? CreateResolvedBatchView(GameSessionState state)
        => state.LastResolvedBatch is null
            ? null
            : new GameResolvedBatchGrainView(
                state.LastResolvedBatch.RoundNumber,
                state.LastResolvedBatch.Players.Select(player => new GameResolvedPlayerBatchGrainView(
                    GetParticipant(state, player.UserId).Player,
                    player.Cards.Select(CreateBatchCardView).ToArray(),
                    player.ProducedVictory)).ToArray(),
                state.LastResolvedBatch.ResolvedAtUtc);

    private static GameCompletionGrainView? CreateCompletionView(GameSessionState state)
    {
        if (state.Completion is null)
        {
            return null;
        }

        var winner = state.Completion.WinnerUserId is null
            ? null
            : GetParticipant(state, state.Completion.WinnerUserId).Player;

        return new GameCompletionGrainView(state.Completion.Reason, winner, state.Completion.CompletedAtUtc);
    }

    private static GameCardInstanceGrainView CreateCardView(GameCardState card)
        => new(
            card.CardInstanceId,
            card.Definition,
            GameCardCatalog.GetDisplayName(card.Definition),
            GameCardCatalog.GetCategory(card.Definition),
            GameCardCatalog.GetResourceColor(card.Definition));

    private static GameBatchCardGrainView CreateBatchCardView(PendingGameBatchCardState card)
        => new(
            CreateCardView(card.Card),
            card.ChosenResourceColor,
            card.CraftedCardDefinition,
            card.TargetResourceColor,
            card.TargetCardInstanceId,
            card.ConsumedCards.Select(reference => new GameCardReferenceGrainView(reference.CardInstanceId, reference.ProducedByCardInstanceId, reference.ProducedCardDefinition)).ToArray());

    private static List<GameCardState> CreateInitialMarketDeck()
        => InitialMarketDeck.Select(definition => CreateCard(definition)).ToList();

    private static GameCardState CreateCard(GameCardDefinition definition)
        => new() { CardInstanceId = Guid.NewGuid(), Definition = definition };

    private static void StartAcquirePhase(GameSessionState state)
    {
        state.Phase = GamePhase.Acquire;
        state.FirstPlayerPendingBatch = null;
        state.SecondPlayerPendingBatch = null;
        state.RoundHadHandChange = false;
        state.AcquirePicksCompletedInPhase = 0;

        RefillVisibleMarket(state);

        state.CurrentAcquireFirstUserId = DetermineAcquireFirstUserId(state);
        state.CurrentAcquireSecondUserId = state.FirstPlayer is not null && state.SecondPlayer is not null
            ? GetOtherUserId(state, state.CurrentAcquireFirstUserId)
            : null;
        state.AcquireFirstCompleted = false;
        state.AcquireSecondCompleted = false;

        if (state.VisibleMarketCards.Count == 0)
        {
            state.AcquirePicksCompletedInPhase = AcquirePicksPerPhase;
            state.Phase = GamePhase.Play;
        }
    }

    private static void RefillVisibleMarket(GameSessionState state)
    {
        if (state.VisibleMarketCards.Count >= GameCardCatalog.MarketSize || state.MarketDeck.Count == 0)
        {
            return;
        }

        Shuffle(state.MarketDeck);

        while (state.VisibleMarketCards.Count < GameCardCatalog.MarketSize && state.MarketDeck.Count > 0)
        {
            var nextCard = state.MarketDeck[0];
            state.MarketDeck.RemoveAt(0);
            state.VisibleMarketCards.Add(nextCard);
        }
    }

    private static string? GetCurrentAcquireUserId(GameSessionState state)
    {
        if (state.Phase != GamePhase.Acquire || state.VisibleMarketCards.Count == 0 || state.AcquirePicksCompletedInPhase >= AcquirePicksPerPhase)
        {
            return null;
        }

        return state.AcquirePicksCompletedInPhase % 2 == 0
            ? state.CurrentAcquireFirstUserId
            : state.CurrentAcquireSecondUserId;
    }

    private static string DetermineAcquireFirstUserId(GameSessionState state)
    {
        EnsureInitialized(state);

        if (state.FirstPlayerScoutOverride && state.SecondPlayerScoutOverride)
        {
            state.FirstPlayerScoutOverride = false;
            state.SecondPlayerScoutOverride = false;
        }
        else if (state.FirstPlayerScoutOverride)
        {
            state.FirstPlayerScoutOverride = false;
            return state.FirstPlayer!.UserId;
        }
        else if (state.SecondPlayerScoutOverride)
        {
            state.SecondPlayerScoutOverride = false;
            return state.SecondPlayer!.UserId;
        }

        var firstScore = GetInitiativeScore(state.FirstPlayerHand);
        var secondScore = GetInitiativeScore(state.SecondPlayerHand);

        if (firstScore != secondScore)
        {
            return firstScore < secondScore ? state.FirstPlayer!.UserId : state.SecondPlayer!.UserId;
        }

        if (state.FirstPlayerHand.Count != state.SecondPlayerHand.Count)
        {
            return state.FirstPlayerHand.Count < state.SecondPlayerHand.Count ? state.FirstPlayer!.UserId : state.SecondPlayer!.UserId;
        }

        if (!string.IsNullOrWhiteSpace(state.PreviousAcquireSecondUserId))
        {
            return state.PreviousAcquireSecondUserId!;
        }

        return state.InitialTieBreakerFirstUserId ?? state.FirstPlayer!.UserId;
    }

    private static int GetInitiativeScore(IEnumerable<GameCardState> hand)
        => hand.Sum(card => GameCardCatalog.GetInitiativeWeight(card.Definition));

    private static string GetOtherUserId(GameSessionState state, string? userId)
        => string.Equals(state.FirstPlayer!.UserId, userId, StringComparison.Ordinal)
            ? state.SecondPlayer!.UserId
            : state.FirstPlayer!.UserId;

    private static List<GameCardState> GetHand(GameSessionState state, bool isFirstPlayer)
        => isFirstPlayer ? state.FirstPlayerHand : state.SecondPlayerHand;

    private static PendingGameBatchState? GetPendingBatch(GameSessionState state, bool isFirstPlayer)
        => isFirstPlayer ? state.FirstPlayerPendingBatch : state.SecondPlayerPendingBatch;

    private static void SetPendingBatch(GameSessionState state, bool isFirstPlayer, PendingGameBatchState batch)
    {
        if (isFirstPlayer)
        {
            state.FirstPlayerPendingBatch = batch;
            return;
        }

        state.SecondPlayerPendingBatch = batch;
    }

    private static void AddCardToHand(GameSessionState state, bool isFirstPlayer, GameCardState card)
    {
        GetHand(state, isFirstPlayer).Add(card);
        state.RoundHadHandChange = true;
    }

    private static bool TryRemoveCardFromHand(List<GameCardState> hand, Guid cardInstanceId, out GameCardState? removedCard)
    {
        var index = hand.FindIndex(card => card.CardInstanceId == cardInstanceId);
        if (index < 0)
        {
            removedCard = null;
            return false;
        }

        removedCard = hand[index];
        hand.RemoveAt(index);
        return true;
    }

    private static PendingGameBatchState BuildPendingBatch(
        SubmitPlayBatchGrainCommand command,
        List<GameCardState> ownHand,
        List<GameCardState> opponentHand)
    {
        if (command.Cards.Count > GameCardCatalog.MaxBatchSize)
        {
            throw new InvalidOperationException($"Players may lock at most {GameCardCatalog.MaxBatchSize} cards in a batch.");
        }

        var handById = ownHand.ToDictionary(card => card.CardInstanceId);
        var opponentHandById = opponentHand.ToDictionary(card => card.CardInstanceId);

        var selectedCardsById = new Dictionary<Guid, GameBatchCardGrainCommand>();
        foreach (var selectedCard in command.Cards)
        {
            if (!selectedCardsById.TryAdd(selectedCard.CardInstanceId, selectedCard))
            {
                throw new InvalidOperationException("A play batch cannot include the same card more than once.");
            }

            if (!handById.TryGetValue(selectedCard.CardInstanceId, out var handCard))
            {
                throw new InvalidOperationException("A selected card is no longer in this player's hand.");
            }

            if (!GameCardCatalog.IsPlayable(handCard.Definition))
            {
                throw new InvalidOperationException("Only action and effect cards may be locked into a play batch.");
            }
        }

        foreach (var selectedCard in command.Cards)
        {
            var handCard = handById[selectedCard.CardInstanceId];
            ValidateSelectedCard(selectedCard, handCard, selectedCardsById, handById, opponentHandById);
        }

        return new PendingGameBatchState
        {
            UserId = command.UserId,
            Cards = command.Cards.Select(selectedCard => new PendingGameBatchCardState
            {
                Card = handById[selectedCard.CardInstanceId],
                ChosenResourceColor = selectedCard.ChosenResourceColor,
                CraftedCardDefinition = selectedCard.CraftedCardDefinition,
                TargetResourceColor = selectedCard.TargetResourceColor,
                TargetCardInstanceId = selectedCard.TargetCardInstanceId,
                ConsumedCards = selectedCard.ConsumedCards.Select(reference => new GameCardReferenceState
                {
                    CardInstanceId = reference.CardInstanceId,
                    ProducedByCardInstanceId = reference.ProducedByCardInstanceId,
                    ProducedCardDefinition = reference.ProducedCardDefinition
                }).ToList()
            }).ToList()
        };
    }

    private static void ValidateSelectedCard(
        GameBatchCardGrainCommand selectedCard,
        GameCardState handCard,
        IReadOnlyDictionary<Guid, GameBatchCardGrainCommand> selectedCardsById,
        IReadOnlyDictionary<Guid, GameCardState> handById,
        IReadOnlyDictionary<Guid, GameCardState> opponentHandById)
    {
        EnsureUniqueConsumedReferences(selectedCard);

        switch (handCard.Definition)
        {
            case GameCardDefinition.Extract:
                ValidateExtract(selectedCard);
                break;
            case GameCardDefinition.Refine:
                ValidateRefine(selectedCard, selectedCardsById, handById);
                break;
            case GameCardDefinition.Produce:
                ValidateProduce(selectedCard);
                break;
            case GameCardDefinition.Sabotage:
                EnsureNoExtraChoices(selectedCard, allowCraftedCard: false, allowTargetCard: true, allowTargetResource: false, allowConsumedCards: false, allowChosenResource: false);
                break;
            case GameCardDefinition.Replicate:
                EnsureNoExtraChoices(selectedCard, allowCraftedCard: false, allowTargetCard: true, allowTargetResource: false, allowConsumedCards: false, allowChosenResource: false);
                break;
            case GameCardDefinition.Catalyst:
                EnsureNoExtraChoices(selectedCard, allowCraftedCard: false, allowTargetCard: true, allowTargetResource: true, allowConsumedCards: false, allowChosenResource: false);
                break;
            case GameCardDefinition.Corrupt:
                EnsureNoExtraChoices(selectedCard, allowCraftedCard: false, allowTargetCard: true, allowTargetResource: false, allowConsumedCards: false, allowChosenResource: false);
                break;
            case GameCardDefinition.Reclaim:
                EnsureNoExtraChoices(selectedCard, allowCraftedCard: false, allowTargetCard: true, allowTargetResource: false, allowConsumedCards: false, allowChosenResource: false);
                break;
            case GameCardDefinition.Scout:
                EnsureNoExtraChoices(selectedCard, allowCraftedCard: false, allowTargetCard: false, allowTargetResource: false, allowConsumedCards: false, allowChosenResource: false);
                break;
            default:
                throw new InvalidOperationException("This card cannot be played in a batch.");
        }
    }

    private static void ValidateExtract(GameBatchCardGrainCommand selectedCard)
        => EnsureNoExtraChoices(selectedCard, allowCraftedCard: false, allowTargetCard: false, allowTargetResource: false, allowConsumedCards: false, allowChosenResource: true);

    private static void ValidateRefine(
        GameBatchCardGrainCommand selectedCard,
        IReadOnlyDictionary<Guid, GameBatchCardGrainCommand> selectedCardsById,
        IReadOnlyDictionary<Guid, GameCardState> handById)
    {
        EnsureNoExtraChoices(selectedCard, allowCraftedCard: false, allowTargetCard: false, allowTargetResource: false, allowConsumedCards: true, allowChosenResource: false);

        foreach (var reference in selectedCard.ConsumedCards)
        {
            var consumedDefinition = ResolveReferenceDefinition(
                reference,
                selectedCardsById,
                handById,
                GameCardCatalog.GetResolutionStep(GameCardDefinition.Refine),
                new HashSet<Guid>());

            if (!GameCraftingRules.IsValidRefineInput(consumedDefinition))
            {
                throw new InvalidOperationException("Refine only accepts base resource inputs.");
            }
        }
    }

    private static void ValidateProduce(GameBatchCardGrainCommand selectedCard)
        => EnsureNoExtraChoices(selectedCard, allowCraftedCard: true, allowTargetCard: false, allowTargetResource: false, allowConsumedCards: true, allowChosenResource: false);

    private static void EnsureNoExtraChoices(
        GameBatchCardGrainCommand selectedCard,
        bool allowCraftedCard,
        bool allowTargetCard,
        bool allowTargetResource,
        bool allowConsumedCards,
        bool allowChosenResource)
    {
        if (!allowCraftedCard && selectedCard.CraftedCardDefinition is not null)
        {
            throw new InvalidOperationException("This card does not accept a crafted card choice.");
        }

        if (!allowTargetCard && selectedCard.TargetCardInstanceId is not null)
        {
            throw new InvalidOperationException("This card does not accept a target card choice.");
        }

        if (!allowTargetResource && selectedCard.TargetResourceColor is not null)
        {
            throw new InvalidOperationException("This card does not accept a target resource choice.");
        }

        if (!allowConsumedCards && selectedCard.ConsumedCards.Count > 0)
        {
            throw new InvalidOperationException("This card does not consume resource inputs.");
        }

        if (!allowChosenResource && selectedCard.ChosenResourceColor is not null)
        {
            throw new InvalidOperationException("This card does not accept a chosen resource color.");
        }
    }

    private static void EnsureUniqueConsumedReferences(GameBatchCardGrainCommand selectedCard)
    {
        var seenExistingCards = new HashSet<Guid>();
        var seenProducedCards = new HashSet<Guid>();

        foreach (var reference in selectedCard.ConsumedCards)
        {
            var hasExistingCard = reference.CardInstanceId.HasValue;
            var hasProducedCard = reference.ProducedByCardInstanceId.HasValue;
            if (hasExistingCard == hasProducedCard)
            {
                throw new InvalidOperationException("A consumed card reference must point to either a hand card or a previously produced card.");
            }

            if (hasExistingCard && !seenExistingCards.Add(reference.CardInstanceId!.Value))
            {
                throw new InvalidOperationException("A resource input cannot be consumed more than once by the same card.");
            }

            if (hasProducedCard && !seenProducedCards.Add(reference.ProducedByCardInstanceId!.Value))
            {
                throw new InvalidOperationException("A previously produced card cannot be consumed more than once by the same card.");
            }
        }
    }

    private static GameCardDefinition ResolveReferenceDefinition(
        GameCardReferenceGrainCommand reference,
        IReadOnlyDictionary<Guid, GameBatchCardGrainCommand> selectedCardsById,
        IReadOnlyDictionary<Guid, GameCardState> handById,
        int currentStep,
        ISet<Guid> visitedCards)
    {
        if (reference.CardInstanceId is { } cardInstanceId)
        {
            return handById.TryGetValue(cardInstanceId, out var handCard)
                ? handCard.Definition
                : throw new InvalidOperationException("A referenced hand card is no longer available.");
        }

        if (reference.ProducedByCardInstanceId is not { } producedByCardInstanceId)
        {
            throw new InvalidOperationException("A consumed card reference is missing its source.");
        }

        if (!selectedCardsById.TryGetValue(producedByCardInstanceId, out var sourceCard))
        {
            throw new InvalidOperationException("A consumed produced-card reference points to a card outside this batch.");
        }

        if (!visitedCards.Add(producedByCardInstanceId))
        {
            throw new InvalidOperationException("Produced card references cannot form a cycle.");
        }

        if (GameCardCatalog.GetResolutionStep(handById[sourceCard.CardInstanceId].Definition) >= currentStep)
        {
            throw new InvalidOperationException("Only cards from an earlier resolution step can provide resource inputs.");
        }

        var producedDefinition = ResolveProducedDefinition(sourceCard, handById[sourceCard.CardInstanceId].Definition, selectedCardsById, handById, visitedCards);
        if (reference.ProducedCardDefinition is not null && reference.ProducedCardDefinition != producedDefinition)
        {
            throw new InvalidOperationException("A produced card reference does not match the declared output.");
        }

        visitedCards.Remove(producedByCardInstanceId);
        return producedDefinition;
    }

    private static GameCardDefinition ResolveProducedDefinition(
        GameBatchCardGrainCommand selectedCard,
        GameCardDefinition sourceDefinition,
        IReadOnlyDictionary<Guid, GameBatchCardGrainCommand> selectedCardsById,
        IReadOnlyDictionary<Guid, GameCardState> handById,
        ISet<Guid> visitedCards)
        => sourceDefinition switch
        {
            GameCardDefinition.Extract when selectedCard.ChosenResourceColor is { } chosenColor && GameCardCatalog.TryGetBaseDefinition(chosenColor, out var extractedDefinition)
                => extractedDefinition,
            GameCardDefinition.Extract => throw new InvalidOperationException("Extract must declare a base resource color."),
            GameCardDefinition.Refine => ResolveRefineOutput(selectedCard, selectedCardsById, handById, visitedCards),
            GameCardDefinition.Produce when GameCraftingRules.TryGetProduceResult(selectedCard.CraftedCardDefinition, out var producedDefinition) => producedDefinition,
            _ => throw new InvalidOperationException("This card does not produce a consumable output.")
        };

    private static GameCardDefinition ResolveRefineOutput(
        GameBatchCardGrainCommand selectedCard,
        IReadOnlyDictionary<Guid, GameBatchCardGrainCommand> selectedCardsById,
        IReadOnlyDictionary<Guid, GameCardState> handById,
        ISet<Guid> visitedCards)
    {
        var consumedDefinitions = selectedCard.ConsumedCards
            .Select(reference => ResolveReferenceDefinition(reference, selectedCardsById, handById, GameCardCatalog.GetResolutionStep(GameCardDefinition.Refine), visitedCards))
            .ToArray();

        if (!GameCraftingRules.TryGetRefineResult(consumedDefinitions, out var output))
        {
            throw new InvalidOperationException("Refine requires a valid pair of base resource inputs.");
        }

        return output;
    }

    private static IReadOnlyList<GameplayEvent> ResolveRound(GameSessionState state, DateTime nowUtc)
    {
        var firstBatch = state.FirstPlayerPendingBatch ?? throw new InvalidOperationException("Both players must lock a batch before resolution.");
        var secondBatch = state.SecondPlayerPendingBatch ?? throw new InvalidOperationException("Both players must lock a batch before resolution.");
        var firstCreatedCards = new Dictionary<Guid, GameCardState>();
        var secondCreatedCards = new Dictionary<Guid, GameCardState>();
        var events = new List<GameplayEvent>();
        var firstProducedVictory = false;
        var secondProducedVictory = false;

        ResolveEffects(state, firstBatch, true, firstCreatedCards, events);
        ResolveEffects(state, secondBatch, false, secondCreatedCards, events);
        ResolveExtracts(state, firstBatch, true, firstCreatedCards, events);
        ResolveExtracts(state, secondBatch, false, secondCreatedCards, events);
        ResolveRefines(state, firstBatch, true, firstCreatedCards, events);
        ResolveRefines(state, secondBatch, false, secondCreatedCards, events);
        firstProducedVictory = ResolveProduces(state, firstBatch, true, firstCreatedCards, events);
        secondProducedVictory = ResolveProduces(state, secondBatch, false, secondCreatedCards, events);

        CleanupBatch(state, firstBatch, true, events);
        CleanupBatch(state, secondBatch, false, events);

        state.LastResolvedBatch = new ResolvedGameBatchState
        {
            RoundNumber = state.RoundNumber,
            ResolvedAtUtc = nowUtc,
            Players =
            [
                new ResolvedGamePlayerBatchState { UserId = firstBatch.UserId, Cards = CloneBatchCards(firstBatch.Cards), ProducedVictory = firstProducedVictory },
                new ResolvedGamePlayerBatchState { UserId = secondBatch.UserId, Cards = CloneBatchCards(secondBatch.Cards), ProducedVictory = secondProducedVictory }
            ]
        };

        UpdateCompletionState(state, firstBatch, secondBatch, firstProducedVictory, secondProducedVictory, nowUtc);
        state.FirstPlayerPendingBatch = null;
        state.SecondPlayerPendingBatch = null;
        return events.ToArray();
    }

    private static void ResolveEffects(
        GameSessionState state,
        PendingGameBatchState batch,
        bool isFirstPlayer,
        Dictionary<Guid, GameCardState> createdCards,
        List<GameplayEvent> events)
    {
        foreach (var playedCard in batch.Cards.Where(card => GameCardCatalog.GetResolutionStep(card.Card.Definition) == 0))
        {
            switch (playedCard.Card.Definition)
            {
                case GameCardDefinition.Sabotage:
                    ResolveTargetedDiscard(state, isFirstPlayer, playedCard, GameCardCatalog.IsBaseResource, "Sabotage", events);
                    break;
                case GameCardDefinition.Replicate:
                    ResolveReplicate(state, isFirstPlayer, playedCard, createdCards, events);
                    break;
                case GameCardDefinition.Catalyst:
                    ResolveCatalyst(state, isFirstPlayer, playedCard, createdCards, events);
                    break;
                case GameCardDefinition.Corrupt:
                    ResolveTargetedDiscard(state, isFirstPlayer, playedCard, GameCardCatalog.IsRefinedResource, "Corrupt", events);
                    break;
                case GameCardDefinition.Reclaim:
                    ResolveReclaim(state, batch, playedCard, events);
                    break;
                case GameCardDefinition.Scout:
                    ResolveScout(state, isFirstPlayer, events);
                    break;
            }
        }
    }

    private static void ResolveTargetedDiscard(
        GameSessionState state,
        bool isFirstPlayer,
        PendingGameBatchCardState playedCard,
        Func<GameCardDefinition, bool> predicate,
        string effectName,
        List<GameplayEvent> events)
    {
        var opponentHand = GetHand(state, !isFirstPlayer);
        if (playedCard.TargetCardInstanceId is not { } targetCardId
            || !TryRemoveCardFromHand(opponentHand, targetCardId, out var removedCard)
            || removedCard is null
            || !predicate(removedCard.Definition))
        {
            events.Add(CreateFizzledEvent(GetParticipantName(state, isFirstPlayer), playedCard.Card.Definition));
            return;
        }

        state.RoundHadHandChange = true;
        events.Add(CreateDiscardedEvent(GetParticipantName(state, isFirstPlayer), playedCard.Card.Definition, GetParticipantName(state, !isFirstPlayer), removedCard.Definition));
    }

    private static void ResolveReplicate(
        GameSessionState state,
        bool isFirstPlayer,
        PendingGameBatchCardState playedCard,
        Dictionary<Guid, GameCardState> createdCards,
        List<GameplayEvent> events)
    {
        var hand = GetHand(state, isFirstPlayer);
        if (playedCard.TargetCardInstanceId is not { } targetCardId)
        {
            events.Add(CreateFizzledEvent(GetParticipantName(state, isFirstPlayer), playedCard.Card.Definition));
            return;
        }

        var targetCard = hand.FirstOrDefault(card => card.CardInstanceId == targetCardId);
        if (targetCard is null || !GameCardCatalog.IsBaseResource(targetCard.Definition))
        {
            events.Add(CreateFizzledEvent(GetParticipantName(state, isFirstPlayer), playedCard.Card.Definition));
            return;
        }

        var createdCard = CreateCard(targetCard.Definition);
        AddCardToHand(state, isFirstPlayer, createdCard);
        createdCards[playedCard.Card.CardInstanceId] = createdCard;
        events.Add(CreateCreatedEvent(GetParticipantName(state, isFirstPlayer), playedCard.Card.Definition, createdCard.Definition));
    }

    private static void ResolveCatalyst(
        GameSessionState state,
        bool isFirstPlayer,
        PendingGameBatchCardState playedCard,
        Dictionary<Guid, GameCardState> createdCards,
        List<GameplayEvent> events)
    {
        var hand = GetHand(state, isFirstPlayer);
        if (playedCard.TargetCardInstanceId is not { } targetCardId
            || playedCard.TargetResourceColor is not { } targetColor
            || !TryRemoveCardFromHand(hand, targetCardId, out var removedCard)
            || removedCard is null
            || !GameCardCatalog.IsBaseResource(removedCard.Definition))
        {
            events.Add(CreateFizzledEvent(GetParticipantName(state, isFirstPlayer), playedCard.Card.Definition));
            return;
        }

        if (!GameCardCatalog.TryGetBaseDefinition(targetColor, out var convertedDefinition))
        {
            events.Add(CreateFizzledEvent(GetParticipantName(state, isFirstPlayer), playedCard.Card.Definition));
            return;
        }

        state.RoundHadHandChange = true;
        var createdCard = CreateCard(convertedDefinition);
        AddCardToHand(state, isFirstPlayer, createdCard);
        createdCards[playedCard.Card.CardInstanceId] = createdCard;
        events.Add(CreateConvertedEvent(GetParticipantName(state, isFirstPlayer), playedCard.Card.Definition, removedCard.Definition, createdCard.Definition));
    }

    private static void ResolveReclaim(GameSessionState state, PendingGameBatchState batch, PendingGameBatchCardState playedCard, List<GameplayEvent> events)
    {
        if (playedCard.TargetCardInstanceId is not { } targetCardId)
        {
            events.Add(CreateFizzledEvent(GetParticipantName(state, batch.UserId), playedCard.Card.Definition));
            return;
        }

        var targetCard = batch.Cards.FirstOrDefault(card => card.Card.CardInstanceId == targetCardId);
        if (targetCard is null || targetCard.Card.CardInstanceId == playedCard.Card.CardInstanceId || !GameCardCatalog.IsMarketCard(targetCard.Card.Definition))
        {
            events.Add(CreateFizzledEvent(GetParticipantName(state, batch.UserId), playedCard.Card.Definition));
            return;
        }

        targetCard.ReturnToHand = true;
        events.Add(CreateScheduledReturnToHandEvent(GetParticipantName(state, batch.UserId), playedCard.Card.Definition, targetCard.Card.Definition));
    }

    private static void ResolveScout(GameSessionState state, bool isFirstPlayer, List<GameplayEvent> events)
    {
        if (isFirstPlayer)
        {
            state.FirstPlayerScoutOverride = true;
        }
        else
        {
            state.SecondPlayerScoutOverride = true;
        }

        events.Add(CreateResolvedEvent(GetParticipantName(state, isFirstPlayer), GameCardDefinition.Scout));
    }

    private static void ResolveExtracts(
        GameSessionState state,
        PendingGameBatchState batch,
        bool isFirstPlayer,
        Dictionary<Guid, GameCardState> createdCards,
        List<GameplayEvent> events)
    {
        foreach (var playedCard in batch.Cards.Where(card => card.Card.Definition == GameCardDefinition.Extract))
        {
            var createdDefinition = playedCard.ChosenResourceColor switch
            {
                GameResourceColor.Red => GameCardDefinition.Red,
                GameResourceColor.Yellow => GameCardDefinition.Yellow,
                GameResourceColor.Blue => GameCardDefinition.Blue,
                _ => (GameCardDefinition?)null
            };

            if (createdDefinition is null)
            {
                events.Add(CreateFizzledEvent(GetParticipantName(state, isFirstPlayer), playedCard.Card.Definition));
                continue;
            }

            var createdCard = CreateCard(createdDefinition.Value);
            AddCardToHand(state, isFirstPlayer, createdCard);
            createdCards[playedCard.Card.CardInstanceId] = createdCard;
            events.Add(CreateCreatedEvent(GetParticipantName(state, isFirstPlayer), playedCard.Card.Definition, createdCard.Definition));
        }
    }

    private static void ResolveRefines(
        GameSessionState state,
        PendingGameBatchState batch,
        bool isFirstPlayer,
        Dictionary<Guid, GameCardState> createdCards,
        List<GameplayEvent> events)
    {
        foreach (var playedCard in batch.Cards.Where(card => card.Card.Definition == GameCardDefinition.Refine))
        {
            var hand = GetHand(state, isFirstPlayer);
            if (!TryResolveConsumedCards(hand, playedCard, createdCards, out var consumedCards)
                || consumedCards.Count != 2
                || GameCardCatalog.TryGetRefineOutput(consumedCards[0].Definition, consumedCards[1].Definition) is not { } createdDefinition)
            {
                events.Add(CreateFizzledEvent(GetParticipantName(state, isFirstPlayer), playedCard.Card.Definition));
                continue;
            }

            RemoveConsumedCards(state, isFirstPlayer, consumedCards);
            var createdCard = CreateCard(createdDefinition);
            AddCardToHand(state, isFirstPlayer, createdCard);
            createdCards[playedCard.Card.CardInstanceId] = createdCard;
            events.Add(CreateCreatedEvent(GetParticipantName(state, isFirstPlayer), playedCard.Card.Definition, createdDefinition));
        }
    }

    private static bool ResolveProduces(
        GameSessionState state,
        PendingGameBatchState batch,
        bool isFirstPlayer,
        Dictionary<Guid, GameCardState> createdCards,
        List<GameplayEvent> events)
    {
        var producedVictory = false;

        foreach (var playedCard in batch.Cards.Where(card => card.Card.Definition == GameCardDefinition.Produce))
        {
            if (!GameCraftingRules.TryGetProduceResult(playedCard.CraftedCardDefinition, out var craftedDefinition))
            {
                events.Add(CreateFizzledEvent(GetParticipantName(state, isFirstPlayer), playedCard.Card.Definition));
                continue;
            }

            var hand = GetHand(state, isFirstPlayer);
            if (!TryResolveConsumedCards(hand, playedCard, createdCards, out var consumedCards)
                || !GameCraftingRules.MatchesProduceRecipe(craftedDefinition, consumedCards.Select(card => card.Definition).ToArray()))
            {
                events.Add(CreateFizzledEvent(GetParticipantName(state, isFirstPlayer), playedCard.Card.Definition));
                continue;
            }

            RemoveConsumedCards(state, isFirstPlayer, consumedCards);
            var createdCard = CreateCard(craftedDefinition);
            AddCardToHand(state, isFirstPlayer, createdCard);
            createdCards[playedCard.Card.CardInstanceId] = createdCard;
            producedVictory |= craftedDefinition == GameCardDefinition.Victory;
            events.Add(CreateCreatedEvent(GetParticipantName(state, isFirstPlayer), playedCard.Card.Definition, craftedDefinition));
        }

        return producedVictory;
    }

    private static bool TryResolveConsumedCards(
        List<GameCardState> hand,
        PendingGameBatchCardState playedCard,
        IReadOnlyDictionary<Guid, GameCardState> createdCards,
        out List<GameCardState> consumedCards)
    {
        consumedCards = [];

        foreach (var reference in playedCard.ConsumedCards)
        {
            GameCardState? resolvedCard = null;

            if (reference.CardInstanceId is { } cardInstanceId)
            {
                resolvedCard = hand.FirstOrDefault(card => card.CardInstanceId == cardInstanceId);
            }
            else if (reference.ProducedByCardInstanceId is { } producedByCardInstanceId)
            {
                if (!createdCards.TryGetValue(producedByCardInstanceId, out var createdCard))
                {
                    return false;
                }

                resolvedCard = hand.FirstOrDefault(card => card.CardInstanceId == createdCard.CardInstanceId);
            }

            if (resolvedCard is null)
            {
                return false;
            }

            if (reference.ProducedCardDefinition is not null && reference.ProducedCardDefinition != resolvedCard.Definition)
            {
                return false;
            }

            consumedCards.Add(resolvedCard);
        }

        return true;
    }

    private static void RemoveConsumedCards(GameSessionState state, bool isFirstPlayer, IEnumerable<GameCardState> cards)
    {
        var hand = GetHand(state, isFirstPlayer);
        foreach (var card in cards)
        {
            if (TryRemoveCardFromHand(hand, card.CardInstanceId, out _))
            {
                state.RoundHadHandChange = true;
            }
        }
    }

    private static void CleanupBatch(GameSessionState state, PendingGameBatchState batch, bool isFirstPlayer, List<GameplayEvent> events)
    {
        foreach (var playedCard in batch.Cards)
        {
            if (!GameCardCatalog.IsMarketCard(playedCard.Card.Definition))
            {
                continue;
            }

            if (playedCard.ReturnToHand)
            {
                AddCardToHand(state, isFirstPlayer, playedCard.Card);
                events.Add(CreateReturnedToHandEvent(GetParticipantName(state, isFirstPlayer), playedCard.Card.Definition));
                continue;
            }

            state.MarketDeck.Add(playedCard.Card);
        }
    }

    private static void UpdateCompletionState(
        GameSessionState state,
        PendingGameBatchState firstBatch,
        PendingGameBatchState secondBatch,
        bool firstProducedVictory,
        bool secondProducedVictory,
        DateTime nowUtc)
    {
        var firstHasVictory = state.FirstPlayerHand.Any(card => card.Definition == GameCardDefinition.Victory);
        var secondHasVictory = state.SecondPlayerHand.Any(card => card.Definition == GameCardDefinition.Victory);

        if (firstHasVictory && secondHasVictory)
        {
            state.Completion = new GameCompletionState
            {
                Reason = GameCompletionReason.Draw,
                CompletedAtUtc = nowUtc
            };
            state.Phase = GamePhase.Completed;
            return;
        }

        if (firstHasVictory || secondHasVictory)
        {
            state.Completion = new GameCompletionState
            {
                Reason = GameCompletionReason.Victory,
                WinnerUserId = firstHasVictory ? state.FirstPlayer!.UserId : state.SecondPlayer!.UserId,
                CompletedAtUtc = nowUtc
            };
            state.Phase = GamePhase.Completed;
            return;
        }

        var bothPlayersPassed = firstBatch.Cards.Count == 0 && secondBatch.Cards.Count == 0;
        if (bothPlayersPassed && !state.RoundHadHandChange)
        {
            state.ConsecutiveStalemateRounds++;
        }
        else
        {
            state.ConsecutiveStalemateRounds = 0;
        }

        if (state.ConsecutiveStalemateRounds >= 2)
        {
            state.Completion = new GameCompletionState
            {
                Reason = GameCompletionReason.Draw,
                CompletedAtUtc = nowUtc
            };
            state.Phase = GamePhase.Completed;
        }
    }

    private static List<PendingGameBatchCardState> CloneBatchCards(IEnumerable<PendingGameBatchCardState> cards)
        => cards.Select(card => new PendingGameBatchCardState
        {
            Card = CloneCard(card.Card),
            ChosenResourceColor = card.ChosenResourceColor,
            CraftedCardDefinition = card.CraftedCardDefinition,
            TargetResourceColor = card.TargetResourceColor,
            TargetCardInstanceId = card.TargetCardInstanceId,
            ReturnToHand = card.ReturnToHand,
            ConsumedCards = card.ConsumedCards.Select(CloneReference).ToList()
        }).ToList();

    private static GameCardState CloneCard(GameCardState card)
        => new() { CardInstanceId = card.CardInstanceId, Definition = card.Definition };

    private static GameCardReferenceState CloneReference(GameCardReferenceState reference)
        => new()
        {
            CardInstanceId = reference.CardInstanceId,
            ProducedByCardInstanceId = reference.ProducedByCardInstanceId,
            ProducedCardDefinition = reference.ProducedCardDefinition
        };

    private static PendingGameBatchState ClonePendingBatch(PendingGameBatchState batch)
        => new()
        {
            UserId = batch.UserId,
            Cards = CloneBatchCards(batch.Cards)
        };

    private static ResolvedGameBatchState CloneResolvedBatch(ResolvedGameBatchState batch)
        => new()
        {
            RoundNumber = batch.RoundNumber,
            ResolvedAtUtc = batch.ResolvedAtUtc,
            Players = batch.Players.Select(player => new ResolvedGamePlayerBatchState
            {
                UserId = player.UserId,
                ProducedVictory = player.ProducedVictory,
                Cards = CloneBatchCards(player.Cards)
            }).ToList()
        };

    private static string GetParticipantName(GameSessionState state, bool isFirstPlayer)
        => GetParticipantName(isFirstPlayer ? state.FirstPlayer : state.SecondPlayer);

    private static string GetParticipantName(GameSessionState state, string userId)
        => TryGetParticipant(state, userId) is { } participant
            ? GetParticipantName(participant.Player)
            : userId;

    private static string GetParticipantName(GameSessionParticipantGrainView? participant)
        => participant is null
            ? string.Empty
            : participant.UserId;

    private static GameplayEvent CreateFizzledEvent(string actorUserId, GameCardDefinition sourceCardDefinition)
        => new(GameplayEventKind.Fizzled, actorUserId, sourceCardDefinition, null, null, null);

    private static GameplayEvent CreateDiscardedEvent(string actorUserId, GameCardDefinition sourceCardDefinition, string targetUserId, GameCardDefinition targetCardDefinition)
        => new(GameplayEventKind.DiscardedCard, actorUserId, sourceCardDefinition, targetUserId, targetCardDefinition, null);

    private static GameplayEvent CreateCreatedEvent(string actorUserId, GameCardDefinition sourceCardDefinition, GameCardDefinition producedCardDefinition)
        => new(GameplayEventKind.CreatedCard, actorUserId, sourceCardDefinition, null, null, producedCardDefinition);

    private static GameplayEvent CreateConvertedEvent(string actorUserId, GameCardDefinition sourceCardDefinition, GameCardDefinition targetCardDefinition, GameCardDefinition producedCardDefinition)
        => new(GameplayEventKind.ConvertedCard, actorUserId, sourceCardDefinition, null, targetCardDefinition, producedCardDefinition);

    private static GameplayEvent CreateScheduledReturnToHandEvent(string actorUserId, GameCardDefinition sourceCardDefinition, GameCardDefinition targetCardDefinition)
        => new(GameplayEventKind.ScheduledReturnToHand, actorUserId, sourceCardDefinition, null, targetCardDefinition, null);

    private static GameplayEvent CreateReturnedToHandEvent(string actorUserId, GameCardDefinition sourceCardDefinition)
        => new(GameplayEventKind.ReturnedToHand, actorUserId, sourceCardDefinition, null, null, null);

    private static GameplayEvent CreateResolvedEvent(string actorUserId, GameCardDefinition sourceCardDefinition)
        => new(GameplayEventKind.Resolved, actorUserId, sourceCardDefinition, null, null, null);

    private static void Shuffle<T>(IList<T> list)
    {
        for (var index = list.Count - 1; index > 0; index--)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (list[index], list[swapIndex]) = (list[swapIndex], list[index]);
        }
    }
}
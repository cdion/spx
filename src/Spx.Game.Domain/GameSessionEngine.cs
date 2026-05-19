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
        GameCardDefinition.Produce,
    ];

    public static void Initialize(GameSessionState state, InitializeGameSessionCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        if (command.FirstPlayer.PlayerId == command.SecondPlayer.PlayerId)
        {
            throw new InvalidOperationException("A game session requires two distinct players.");
        }

        if (
            state.FirstPlayer is not null
            && state.SecondPlayer is not null
            && HasSameRoster(
                state.FirstPlayer,
                state.SecondPlayer,
                command.FirstPlayer,
                command.SecondPlayer
            )
        )
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
        state.PreviousAcquireSecondPlayerId = null;
        state.InitialTieBreakerFirstPlayerId =
            Random.Shared.Next(2) == 0
                ? command.FirstPlayer.PlayerId
                : command.SecondPlayer.PlayerId;
        state.Completion = null;
        state.ConsecutiveStalemateRounds = 0;
        state.AcquirePicksCompletedInPhase = 0;
        StartAcquirePhase(state);
    }

    public static GameSessionCommandResult SubmitAcquire(
        GameSessionState state,
        Guid gameId,
        SubmitAcquireCommand command
    )
    {
        try
        {
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(command);

            EnsureInitialized(state);
            EnsureNotCompleted(state);

            var participant = GetParticipant(state, command.PlayerId);
            if (!participant.IsActive)
            {
                throw new InvalidOperationException("Inactive players cannot acquire cards.");
            }

            if (command.ExpectedRoundNumber != state.RoundNumber)
            {
                throw new InvalidOperationException(
                    "The submitted acquire pick does not match the current round."
                );
            }

            if (state.Phase != GamePhase.Acquire)
            {
                throw new InvalidOperationException("The game is not in the acquire phase.");
            }

            if (state.VisibleMarketCards.Count == 0)
            {
                state.AcquirePicksCompletedInPhase = AcquirePicksPerPhase;
                state.Phase = GamePhase.Play;
                return new GameSessionCommandSucceededResult(
                    CreatePlayerView(state, gameId, participant.Player.PlayerId)
                );
            }

            var currentAcquirePlayerId = GetCurrentAcquirePlayerId(state);
            if (currentAcquirePlayerId != command.PlayerId)
            {
                throw new InvalidOperationException(
                    "It is not this player's turn to acquire a card."
                );
            }

            var marketCard =
                state.VisibleMarketCards.FirstOrDefault(card =>
                    card.CardInstanceId == command.MarketCardInstanceId
                )
                ?? throw new InvalidOperationException(
                    "The selected market card is no longer available."
                );

            state.VisibleMarketCards.Remove(marketCard);
            AddCardToHand(state, participant.IsFirstPlayer, marketCard);

            state.AcquirePicksCompletedInPhase++;

            if (
                state.VisibleMarketCards.Count == 0
                || state.AcquirePicksCompletedInPhase >= AcquirePicksPerPhase
            )
            {
                state.PreviousAcquireSecondPlayerId = state.CurrentAcquireSecondPlayerId;
                state.Phase = GamePhase.Play;
            }

            return new GameSessionCommandSucceededResult(
                CreatePlayerView(state, gameId, participant.Player.PlayerId)
            );
        }
        catch (InvalidOperationException exception)
        {
            return new GameSessionCommandRejectedResult(exception.Message);
        }
    }

    public static GameSessionCommandResult SubmitPlayBatch(
        GameSessionState state,
        Guid gameId,
        SubmitPlayBatchCommand command,
        DateTime nowUtc
    )
    {
        try
        {
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(command);

            EnsureInitialized(state);
            EnsureNotCompleted(state);

            var participant = GetParticipant(state, command.PlayerId);
            if (!participant.IsActive)
            {
                throw new InvalidOperationException("Inactive players cannot submit play batches.");
            }

            if (command.ExpectedRoundNumber != state.RoundNumber)
            {
                throw new InvalidOperationException(
                    "The submitted play batch does not match the current round."
                );
            }

            if (state.Phase != GamePhase.Play)
            {
                throw new InvalidOperationException("The game is not in the play phase.");
            }

            if (GetPendingBatch(state, participant.IsFirstPlayer) is not null)
            {
                throw new InvalidOperationException(
                    "This player's batch is already locked for the current round."
                );
            }

            var ownHand = GetHand(state, participant.IsFirstPlayer);
            var opponentHand = GetHand(state, !participant.IsFirstPlayer);
            var pendingBatch = BuildPendingBatch(command, ownHand, opponentHand);

            foreach (var playedCard in pendingBatch.Cards)
            {
                if (!TryRemoveCardFromHand(ownHand, playedCard.Card.CardInstanceId, out _))
                {
                    throw new InvalidOperationException(
                        "A selected card is no longer in this player's hand."
                    );
                }

                state.RoundHadHandChange = true;
            }

            SetPendingBatch(state, participant.IsFirstPlayer, pendingBatch);

            IReadOnlyList<GameplayEvent> gameplayEvents = [];

            if (
                state.FirstPlayerPendingBatch is not null
                && state.SecondPlayerPendingBatch is not null
            )
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

            return new GameSessionCommandSucceededResult(
                CreatePlayerView(state, gameId, participant.Player.PlayerId),
                gameplayEvents
            );
        }
        catch (InvalidOperationException exception)
        {
            return new GameSessionCommandRejectedResult(exception.Message);
        }
    }

    public static GameSessionView? GetSessionView(
        GameSessionState state,
        Guid gameId,
        GetGameSessionQuery query
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(query);

        if (state.FirstPlayer is null || state.SecondPlayer is null)
        {
            return null;
        }

        return TryGetParticipant(state, query.PlayerId) is { } participant
            ? CreatePlayerView(state, gameId, participant.Player.PlayerId)
            : null;
    }

    public static GameSessionView AbandonPlayer(
        GameSessionState state,
        Guid gameId,
        AbandonGameSessionCommand command,
        DateTime nowUtc
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        EnsureInitialized(state);

        var participant = GetParticipant(state, command.PlayerId);
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
            WinnerPlayerId = participant.Opponent.PlayerId,
            CompletedAtUtc = nowUtc,
        };

        return CreatePlayerView(state, gameId, participant.Player.PlayerId);
    }

    private static bool HasSameRoster(
        GameSessionParticipant existingFirstPlayer,
        GameSessionParticipant existingSecondPlayer,
        GameSessionParticipant incomingFirstPlayer,
        GameSessionParticipant incomingSecondPlayer
    ) =>
        (
            existingFirstPlayer.PlayerId == incomingFirstPlayer.PlayerId
            && existingSecondPlayer.PlayerId == incomingSecondPlayer.PlayerId
        )
        || (
            existingFirstPlayer.PlayerId == incomingSecondPlayer.PlayerId
            && existingSecondPlayer.PlayerId == incomingFirstPlayer.PlayerId
        );

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

    private static ParticipantState GetParticipant(GameSessionState state, Guid playerId) =>
        TryGetParticipant(state, playerId)
        ?? throw new InvalidOperationException(
            "The current user is not part of this game session."
        );

    private static ParticipantState? TryGetParticipant(GameSessionState state, Guid playerId)
    {
        if (state.FirstPlayer is not null && state.FirstPlayer.PlayerId == playerId)
        {
            return new ParticipantState(
                state.FirstPlayer,
                state.SecondPlayer!,
                state.FirstPlayerActive,
                IsFirstPlayer: true
            );
        }

        if (state.SecondPlayer is not null && state.SecondPlayer.PlayerId == playerId)
        {
            return new ParticipantState(
                state.SecondPlayer,
                state.FirstPlayer!,
                state.SecondPlayerActive,
                IsFirstPlayer: false
            );
        }

        return null;
    }

    private static GameSessionView CreatePlayerView(
        GameSessionState state,
        Guid gameId,
        Guid playerId
    )
    {
        var participant = GetParticipant(state, playerId);
        var currentPendingBatch = GetPendingBatch(state, participant.IsFirstPlayer);
        var opponentPendingBatch = GetPendingBatch(state, !participant.IsFirstPlayer);
        var canAcquireCard =
            state.Phase == GamePhase.Acquire
            && state.VisibleMarketCards.Count > 0
            && GetCurrentAcquirePlayerId(state) == playerId;
        var canLockBatch =
            state.Phase == GamePhase.Play && currentPendingBatch is null && participant.IsActive;
        var waitingForOpponent = state.Phase switch
        {
            GamePhase.Acquire => participant.IsActive
                && !canAcquireCard
                && state.VisibleMarketCards.Count > 0
                && (
                    state.CurrentAcquireFirstPlayerId.HasValue
                    || state.CurrentAcquireSecondPlayerId.HasValue
                ),
            GamePhase.Play => currentPendingBatch is not null
                && opponentPendingBatch is null
                && (participant.IsFirstPlayer ? state.SecondPlayerActive : state.FirstPlayerActive),
            _ => false,
        };

        return new GameSessionView(
            gameId,
            state.RoundNumber,
            state.Phase,
            CreatePlayerStateView(
                state,
                participant.Player,
                participant.IsFirstPlayer,
                currentPendingBatch,
                revealLockedCards: true
            ),
            CreatePlayerStateView(
                state,
                participant.Opponent,
                !participant.IsFirstPlayer,
                opponentPendingBatch,
                revealLockedCards: false
            ),
            state.VisibleMarketCards.Select(CreateCardView).ToArray(),
            state.MarketDeck.Count,
            waitingForOpponent,
            canAcquireCard,
            canLockBatch,
            GameCardCatalog.MaxBatchSize,
            CreateResolvedBatchView(state),
            CreateCompletionView(state)
        );
    }

    private sealed record ParticipantState(
        GameSessionParticipant Player,
        GameSessionParticipant Opponent,
        bool IsActive,
        bool IsFirstPlayer
    );

    private static GamePlayerStateView CreatePlayerStateView(
        GameSessionState state,
        GameSessionParticipant participant,
        bool isFirstPlayer,
        PendingGameBatchState? pendingBatch,
        bool revealLockedCards
    ) =>
        new(
            participant,
            GetHand(state, isFirstPlayer).Select(CreateCardView).ToArray(),
            pendingBatch is not null,
            pendingBatch?.Cards.Count ?? 0,
            GetInitiativeScore(GetHand(state, isFirstPlayer)),
            isFirstPlayer ? state.FirstPlayerScoutOverride : state.SecondPlayerScoutOverride,
            state.CurrentAcquireFirstPlayerId == participant.PlayerId,
            revealLockedCards && pendingBatch is not null
                ? pendingBatch.Cards.Select(CreateBatchCardView).ToArray()
                : []
        );

    private static GameResolvedBatchView? CreateResolvedBatchView(GameSessionState state) =>
        state.LastResolvedBatch is null
            ? null
            : new GameResolvedBatchView(
                state.LastResolvedBatch.RoundNumber,
                state
                    .LastResolvedBatch.Players.Select(player => new GameResolvedPlayerBatchView(
                        GetParticipant(state, player.PlayerId).Player,
                        player.Cards.Select(CreateBatchCardView).ToArray(),
                        player.ProducedVictory
                    ))
                    .ToArray(),
                state.LastResolvedBatch.ResolvedAtUtc
            );

    private static GameCompletionView? CreateCompletionView(GameSessionState state)
    {
        if (state.Completion is null)
        {
            return null;
        }

        var winner = state.Completion.WinnerPlayerId is null
            ? null
            : GetParticipant(state, state.Completion.WinnerPlayerId.Value).Player;

        return new GameCompletionView(
            state.Completion.Reason,
            winner,
            state.Completion.CompletedAtUtc
        );
    }

    private static GameCardView CreateCardView(GameCardState card) =>
        new(
            card.CardInstanceId,
            card.Definition,
            GameCardCatalog.GetDisplayName(card.Definition),
            GameCardCatalog.GetCategory(card.Definition),
            GameCardCatalog.GetResourceColor(card.Definition)
        );

    private static GameBatchCardView CreateBatchCardView(PendingGameBatchCardState card) =>
        new(
            CreateCardView(card.Card),
            card.ChosenResourceColor,
            card.CraftedCardDefinition,
            card.TargetResourceColor,
            card.TargetCardInstanceId,
            card.ConsumedCards.Select(reference => new GameCardReferenceView(
                    reference.CardInstanceId,
                    reference.ProducedByCardInstanceId,
                    reference.ProducedCardDefinition
                ))
                .ToArray()
        );

    private static List<GameCardState> CreateInitialMarketDeck() =>
        InitialMarketDeck.Select(definition => CreateCard(definition)).ToList();

    private static GameCardState CreateCard(GameCardDefinition definition) =>
        new() { CardInstanceId = Guid.NewGuid(), Definition = definition };

    private static void StartAcquirePhase(GameSessionState state)
    {
        state.Phase = GamePhase.Acquire;
        state.FirstPlayerPendingBatch = null;
        state.SecondPlayerPendingBatch = null;
        state.RoundHadHandChange = false;
        state.AcquirePicksCompletedInPhase = 0;

        RefillVisibleMarket(state);

        state.CurrentAcquireFirstPlayerId =
            TryConsumeScoutOverride(state) ?? DetermineAcquireFirstPlayerId(state);
        state.CurrentAcquireSecondPlayerId =
            state.FirstPlayer is not null && state.SecondPlayer is not null
                ? GetOtherPlayerId(state, state.CurrentAcquireFirstPlayerId)
                : null;
        if (state.VisibleMarketCards.Count == 0)
        {
            state.AcquirePicksCompletedInPhase = AcquirePicksPerPhase;
            state.Phase = GamePhase.Play;
        }
    }

    private static void RefillVisibleMarket(GameSessionState state)
    {
        if (
            state.VisibleMarketCards.Count >= GameCardCatalog.MarketSize
            || state.MarketDeck.Count == 0
        )
        {
            return;
        }

        Shuffle(state.MarketDeck);

        while (
            state.VisibleMarketCards.Count < GameCardCatalog.MarketSize
            && state.MarketDeck.Count > 0
        )
        {
            var nextCard = state.MarketDeck[0];
            state.MarketDeck.RemoveAt(0);
            state.VisibleMarketCards.Add(nextCard);
        }
    }

    private static Guid? GetCurrentAcquirePlayerId(GameSessionState state)
    {
        if (
            state.Phase != GamePhase.Acquire
            || state.VisibleMarketCards.Count == 0
            || state.AcquirePicksCompletedInPhase >= AcquirePicksPerPhase
        )
        {
            return null;
        }

        return state.AcquirePicksCompletedInPhase % 2 == 0
            ? state.CurrentAcquireFirstPlayerId
            : state.CurrentAcquireSecondPlayerId;
    }

    private static Guid? TryConsumeScoutOverride(GameSessionState state)
    {
        if (state.FirstPlayerScoutOverride && state.SecondPlayerScoutOverride)
        {
            state.FirstPlayerScoutOverride = false;
            state.SecondPlayerScoutOverride = false;
            return null;
        }

        if (state.FirstPlayerScoutOverride)
        {
            state.FirstPlayerScoutOverride = false;
            return state.FirstPlayer!.PlayerId;
        }

        if (state.SecondPlayerScoutOverride)
        {
            state.SecondPlayerScoutOverride = false;
            return state.SecondPlayer!.PlayerId;
        }

        return null;
    }

    private static Guid DetermineAcquireFirstPlayerId(GameSessionState state)
    {
        EnsureInitialized(state);

        var firstScore = GetInitiativeScore(state.FirstPlayerHand);
        var secondScore = GetInitiativeScore(state.SecondPlayerHand);

        if (firstScore != secondScore)
        {
            return firstScore < secondScore
                ? state.FirstPlayer!.PlayerId
                : state.SecondPlayer!.PlayerId;
        }

        if (state.FirstPlayerHand.Count != state.SecondPlayerHand.Count)
        {
            return state.FirstPlayerHand.Count < state.SecondPlayerHand.Count
                ? state.FirstPlayer!.PlayerId
                : state.SecondPlayer!.PlayerId;
        }

        if (state.PreviousAcquireSecondPlayerId.HasValue)
        {
            return state.PreviousAcquireSecondPlayerId.Value;
        }

        return state.InitialTieBreakerFirstPlayerId ?? state.FirstPlayer!.PlayerId;
    }

    private static int GetInitiativeScore(IEnumerable<GameCardState> hand) =>
        hand.Sum(card => GameCardCatalog.GetInitiativeWeight(card.Definition));

    private static Guid GetOtherPlayerId(GameSessionState state, Guid? playerId) =>
        state.FirstPlayer!.PlayerId == playerId
            ? state.SecondPlayer!.PlayerId
            : state.FirstPlayer!.PlayerId;

    private static List<GameCardState> GetHand(GameSessionState state, bool isFirstPlayer) =>
        isFirstPlayer ? state.FirstPlayerHand : state.SecondPlayerHand;

    private static PendingGameBatchState? GetPendingBatch(
        GameSessionState state,
        bool isFirstPlayer
    ) => isFirstPlayer ? state.FirstPlayerPendingBatch : state.SecondPlayerPendingBatch;

    private static void SetPendingBatch(
        GameSessionState state,
        bool isFirstPlayer,
        PendingGameBatchState batch
    )
    {
        if (isFirstPlayer)
        {
            state.FirstPlayerPendingBatch = batch;
            return;
        }

        state.SecondPlayerPendingBatch = batch;
    }

    private static void AddCardToHand(
        GameSessionState state,
        bool isFirstPlayer,
        GameCardState card
    )
    {
        GetHand(state, isFirstPlayer).Add(card);
        state.RoundHadHandChange = true;
    }

    private static bool TryRemoveCardFromHand(
        List<GameCardState> hand,
        Guid cardInstanceId,
        out GameCardState? removedCard
    )
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
        SubmitPlayBatchCommand command,
        List<GameCardState> ownHand,
        List<GameCardState> opponentHand
    )
    {
        if (command.Cards.Count > GameCardCatalog.MaxBatchSize)
        {
            throw new InvalidOperationException(
                $"Players may lock at most {GameCardCatalog.MaxBatchSize} cards in a batch."
            );
        }

        var handById = ownHand.ToDictionary(card => card.CardInstanceId);
        var opponentHandById = opponentHand.ToDictionary(card => card.CardInstanceId);

        var selectedCardsById = new Dictionary<Guid, GameBatchCardCommand>();
        foreach (var selectedCard in command.Cards)
        {
            if (!selectedCardsById.TryAdd(selectedCard.CardInstanceId, selectedCard))
            {
                throw new InvalidOperationException(
                    "A play batch cannot include the same card more than once."
                );
            }

            if (!handById.TryGetValue(selectedCard.CardInstanceId, out var handCard))
            {
                throw new InvalidOperationException(
                    "A selected card is no longer in this player's hand."
                );
            }

            if (!GameCardCatalog.IsPlayable(handCard.Definition))
            {
                throw new InvalidOperationException(
                    "Only action and effect cards may be locked into a play batch."
                );
            }
        }

        foreach (var selectedCard in command.Cards)
        {
            var handCard = handById[selectedCard.CardInstanceId];
            ValidateSelectedCard(
                selectedCard,
                handCard,
                selectedCardsById,
                handById,
                opponentHandById
            );
        }

        return new PendingGameBatchState
        {
            PlayerId = command.PlayerId,
            Cards = command
                .Cards.Select(selectedCard => new PendingGameBatchCardState
                {
                    Card = handById[selectedCard.CardInstanceId],
                    ChosenResourceColor = selectedCard.ChosenResourceColor,
                    CraftedCardDefinition = selectedCard.CraftedCardDefinition,
                    TargetResourceColor = selectedCard.TargetResourceColor,
                    TargetCardInstanceId = selectedCard.TargetCardInstanceId,
                    ConsumedCards = selectedCard
                        .ConsumedCards.Select(reference => new GameCardReferenceState
                        {
                            CardInstanceId = reference.CardInstanceId,
                            ProducedByCardInstanceId = reference.ProducedByCardInstanceId,
                            ProducedCardDefinition = reference.ProducedCardDefinition,
                        })
                        .ToList(),
                })
                .ToList(),
        };
    }

    private static void ValidateSelectedCard(
        GameBatchCardCommand selectedCard,
        GameCardState handCard,
        IReadOnlyDictionary<Guid, GameBatchCardCommand> selectedCardsById,
        IReadOnlyDictionary<Guid, GameCardState> handById,
        IReadOnlyDictionary<Guid, GameCardState> opponentHandById
    )
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
                EnsureNoExtraChoices(
                    selectedCard,
                    allowCraftedCard: false,
                    allowTargetCard: true,
                    allowTargetResource: false,
                    allowConsumedCards: false,
                    allowChosenResource: false
                );
                break;
            case GameCardDefinition.Replicate:
                EnsureNoExtraChoices(
                    selectedCard,
                    allowCraftedCard: false,
                    allowTargetCard: true,
                    allowTargetResource: false,
                    allowConsumedCards: false,
                    allowChosenResource: false
                );
                break;
            case GameCardDefinition.Catalyst:
                EnsureNoExtraChoices(
                    selectedCard,
                    allowCraftedCard: false,
                    allowTargetCard: true,
                    allowTargetResource: true,
                    allowConsumedCards: false,
                    allowChosenResource: false
                );
                break;
            case GameCardDefinition.Corrupt:
                EnsureNoExtraChoices(
                    selectedCard,
                    allowCraftedCard: false,
                    allowTargetCard: true,
                    allowTargetResource: false,
                    allowConsumedCards: false,
                    allowChosenResource: false
                );
                break;
            case GameCardDefinition.Reclaim:
                EnsureNoExtraChoices(
                    selectedCard,
                    allowCraftedCard: false,
                    allowTargetCard: true,
                    allowTargetResource: false,
                    allowConsumedCards: false,
                    allowChosenResource: false
                );
                break;
            case GameCardDefinition.Scout:
                EnsureNoExtraChoices(
                    selectedCard,
                    allowCraftedCard: false,
                    allowTargetCard: false,
                    allowTargetResource: false,
                    allowConsumedCards: false,
                    allowChosenResource: false
                );
                break;
            default:
                throw new InvalidOperationException("This card cannot be played in a batch.");
        }
    }

    private static void ValidateExtract(GameBatchCardCommand selectedCard) =>
        EnsureNoExtraChoices(
            selectedCard,
            allowCraftedCard: false,
            allowTargetCard: false,
            allowTargetResource: false,
            allowConsumedCards: false,
            allowChosenResource: true
        );

    private static void ValidateRefine(
        GameBatchCardCommand selectedCard,
        IReadOnlyDictionary<Guid, GameBatchCardCommand> selectedCardsById,
        IReadOnlyDictionary<Guid, GameCardState> handById
    )
    {
        EnsureNoExtraChoices(
            selectedCard,
            allowCraftedCard: false,
            allowTargetCard: false,
            allowTargetResource: false,
            allowConsumedCards: true,
            allowChosenResource: false
        );

        foreach (var reference in selectedCard.ConsumedCards)
        {
            var consumedDefinition = ResolveReferenceDefinition(
                reference,
                selectedCardsById,
                handById,
                GameCardCatalog.GetResolutionStep(GameCardDefinition.Refine),
                new HashSet<Guid>()
            );

            if (!GameCraftingRules.IsValidRefineInput(consumedDefinition))
            {
                throw new InvalidOperationException("Refine only accepts base resource inputs.");
            }
        }
    }

    private static void ValidateProduce(GameBatchCardCommand selectedCard) =>
        EnsureNoExtraChoices(
            selectedCard,
            allowCraftedCard: true,
            allowTargetCard: false,
            allowTargetResource: false,
            allowConsumedCards: true,
            allowChosenResource: false
        );

    private static void EnsureNoExtraChoices(
        GameBatchCardCommand selectedCard,
        bool allowCraftedCard,
        bool allowTargetCard,
        bool allowTargetResource,
        bool allowConsumedCards,
        bool allowChosenResource
    )
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
            throw new InvalidOperationException(
                "This card does not accept a target resource choice."
            );
        }

        if (!allowConsumedCards && selectedCard.ConsumedCards.Count > 0)
        {
            throw new InvalidOperationException("This card does not consume resource inputs.");
        }

        if (!allowChosenResource && selectedCard.ChosenResourceColor is not null)
        {
            throw new InvalidOperationException(
                "This card does not accept a chosen resource color."
            );
        }
    }

    private static void EnsureUniqueConsumedReferences(GameBatchCardCommand selectedCard)
    {
        var seenExistingCards = new HashSet<Guid>();
        var seenProducedCards = new HashSet<Guid>();

        foreach (var reference in selectedCard.ConsumedCards)
        {
            var hasExistingCard = reference.CardInstanceId.HasValue;
            var hasProducedCard = reference.ProducedByCardInstanceId.HasValue;
            if (hasExistingCard == hasProducedCard)
            {
                throw new InvalidOperationException(
                    "A consumed card reference must point to either a hand card or a previously produced card."
                );
            }

            if (hasExistingCard && !seenExistingCards.Add(reference.CardInstanceId!.Value))
            {
                throw new InvalidOperationException(
                    "A resource input cannot be consumed more than once by the same card."
                );
            }

            if (
                hasProducedCard && !seenProducedCards.Add(reference.ProducedByCardInstanceId!.Value)
            )
            {
                throw new InvalidOperationException(
                    "A previously produced card cannot be consumed more than once by the same card."
                );
            }
        }
    }

    private static GameCardDefinition ResolveReferenceDefinition(
        GameCardReferenceCommand reference,
        IReadOnlyDictionary<Guid, GameBatchCardCommand> selectedCardsById,
        IReadOnlyDictionary<Guid, GameCardState> handById,
        int currentStep,
        ISet<Guid> visitedCards
    )
    {
        if (reference.CardInstanceId is { } cardInstanceId)
        {
            return handById.TryGetValue(cardInstanceId, out var handCard)
                ? handCard.Definition
                : throw new InvalidOperationException(
                    "A referenced hand card is no longer available."
                );
        }

        if (reference.ProducedByCardInstanceId is not { } producedByCardInstanceId)
        {
            throw new InvalidOperationException("A consumed card reference is missing its source.");
        }

        if (!selectedCardsById.TryGetValue(producedByCardInstanceId, out var sourceCard))
        {
            throw new InvalidOperationException(
                "A consumed produced-card reference points to a card outside this batch."
            );
        }

        if (!visitedCards.Add(producedByCardInstanceId))
        {
            throw new InvalidOperationException("Produced card references cannot form a cycle.");
        }

        if (
            GameCardCatalog.GetResolutionStep(handById[sourceCard.CardInstanceId].Definition)
            >= currentStep
        )
        {
            throw new InvalidOperationException(
                "Only cards from an earlier resolution step can provide resource inputs."
            );
        }

        var producedDefinition = ResolveProducedDefinition(
            sourceCard,
            handById[sourceCard.CardInstanceId].Definition,
            selectedCardsById,
            handById,
            visitedCards
        );
        if (
            reference.ProducedCardDefinition is not null
            && reference.ProducedCardDefinition != producedDefinition
        )
        {
            throw new InvalidOperationException(
                "A produced card reference does not match the declared output."
            );
        }

        visitedCards.Remove(producedByCardInstanceId);
        return producedDefinition;
    }

    private static GameCardDefinition ResolveProducedDefinition(
        GameBatchCardCommand selectedCard,
        GameCardDefinition sourceDefinition,
        IReadOnlyDictionary<Guid, GameBatchCardCommand> selectedCardsById,
        IReadOnlyDictionary<Guid, GameCardState> handById,
        ISet<Guid> visitedCards
    ) =>
        sourceDefinition switch
        {
            GameCardDefinition.Extract
                when selectedCard.ChosenResourceColor is { } chosenColor
                    && GameCardCatalog.TryGetBaseDefinition(
                        chosenColor,
                        out var extractedDefinition
                    ) => extractedDefinition,
            GameCardDefinition.Extract => throw new InvalidOperationException(
                "Extract must declare a base resource color."
            ),
            GameCardDefinition.Refine => ResolveRefineOutput(
                selectedCard,
                selectedCardsById,
                handById,
                visitedCards
            ),
            GameCardDefinition.Produce
                when GameCraftingRules.TryGetProduceResult(
                    selectedCard.CraftedCardDefinition,
                    out var producedDefinition
                ) => producedDefinition,
            _ => throw new InvalidOperationException(
                "This card does not produce a consumable output."
            ),
        };

    private static GameCardDefinition ResolveRefineOutput(
        GameBatchCardCommand selectedCard,
        IReadOnlyDictionary<Guid, GameBatchCardCommand> selectedCardsById,
        IReadOnlyDictionary<Guid, GameCardState> handById,
        ISet<Guid> visitedCards
    )
    {
        var consumedDefinitions = selectedCard
            .ConsumedCards.Select(reference =>
                ResolveReferenceDefinition(
                    reference,
                    selectedCardsById,
                    handById,
                    GameCardCatalog.GetResolutionStep(GameCardDefinition.Refine),
                    visitedCards
                )
            )
            .ToArray();

        if (!GameCraftingRules.TryGetRefineResult(consumedDefinitions, out var output))
        {
            throw new InvalidOperationException(
                "Refine requires a valid pair of base resource inputs."
            );
        }

        return output;
    }

    private static GameplayEvent[] ResolveRound(GameSessionState state, DateTime nowUtc)
    {
        var firstBatch =
            state.FirstPlayerPendingBatch
            ?? throw new InvalidOperationException(
                "Both players must lock a batch before resolution."
            );
        var secondBatch =
            state.SecondPlayerPendingBatch
            ?? throw new InvalidOperationException(
                "Both players must lock a batch before resolution."
            );
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
        secondProducedVictory = ResolveProduces(
            state,
            secondBatch,
            false,
            secondCreatedCards,
            events
        );

        CleanupBatch(state, firstBatch, true, events);
        CleanupBatch(state, secondBatch, false, events);

        state.LastResolvedBatch = new ResolvedGameBatchState
        {
            RoundNumber = state.RoundNumber,
            ResolvedAtUtc = nowUtc,
            Players =
            [
                new ResolvedGamePlayerBatchState
                {
                    PlayerId = firstBatch.PlayerId,
                    Cards = CloneBatchCards(firstBatch.Cards),
                    ProducedVictory = firstProducedVictory,
                },
                new ResolvedGamePlayerBatchState
                {
                    PlayerId = secondBatch.PlayerId,
                    Cards = CloneBatchCards(secondBatch.Cards),
                    ProducedVictory = secondProducedVictory,
                },
            ],
        };

        UpdateCompletionState(
            state,
            firstBatch,
            secondBatch,
            firstProducedVictory,
            secondProducedVictory,
            nowUtc
        );
        state.FirstPlayerPendingBatch = null;
        state.SecondPlayerPendingBatch = null;
        return events.ToArray();
    }

    private static void ResolveEffects(
        GameSessionState state,
        PendingGameBatchState batch,
        bool isFirstPlayer,
        Dictionary<Guid, GameCardState> createdCards,
        List<GameplayEvent> events
    )
    {
        foreach (
            var playedCard in batch.Cards.Where(card =>
                GameCardCatalog.GetResolutionStep(card.Card.Definition) == 0
            )
        )
        {
            switch (playedCard.Card.Definition)
            {
                case GameCardDefinition.Sabotage:
                    ResolveTargetedDiscard(
                        state,
                        isFirstPlayer,
                        playedCard,
                        GameCardCatalog.IsBaseResource,
                        "Sabotage",
                        events
                    );
                    break;
                case GameCardDefinition.Replicate:
                    ResolveReplicate(state, isFirstPlayer, playedCard, createdCards, events);
                    break;
                case GameCardDefinition.Catalyst:
                    ResolveCatalyst(state, isFirstPlayer, playedCard, createdCards, events);
                    break;
                case GameCardDefinition.Corrupt:
                    ResolveTargetedDiscard(
                        state,
                        isFirstPlayer,
                        playedCard,
                        GameCardCatalog.IsRefinedResource,
                        "Corrupt",
                        events
                    );
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
        List<GameplayEvent> events
    )
    {
        var opponentHand = GetHand(state, !isFirstPlayer);
        if (
            playedCard.TargetCardInstanceId is not { } targetCardId
            || !TryRemoveCardFromHand(opponentHand, targetCardId, out var removedCard)
            || removedCard is null
            || !predicate(removedCard.Definition)
        )
        {
            events.Add(
                CreateFizzledEvent(
                    GetParticipantPlayerId(state, isFirstPlayer),
                    playedCard.Card.Definition
                )
            );
            return;
        }

        state.RoundHadHandChange = true;
        events.Add(
            CreateDiscardedEvent(
                GetParticipantPlayerId(state, isFirstPlayer),
                playedCard.Card.Definition,
                GetParticipantPlayerId(state, !isFirstPlayer),
                removedCard.Definition
            )
        );
    }

    private static void ResolveReplicate(
        GameSessionState state,
        bool isFirstPlayer,
        PendingGameBatchCardState playedCard,
        Dictionary<Guid, GameCardState> createdCards,
        List<GameplayEvent> events
    )
    {
        var hand = GetHand(state, isFirstPlayer);
        if (playedCard.TargetCardInstanceId is not { } targetCardId)
        {
            events.Add(
                CreateFizzledEvent(
                    GetParticipantPlayerId(state, isFirstPlayer),
                    playedCard.Card.Definition
                )
            );
            return;
        }

        var targetCard = hand.FirstOrDefault(card => card.CardInstanceId == targetCardId);
        if (targetCard is null || !GameCardCatalog.IsBaseResource(targetCard.Definition))
        {
            events.Add(
                CreateFizzledEvent(
                    GetParticipantPlayerId(state, isFirstPlayer),
                    playedCard.Card.Definition
                )
            );
            return;
        }

        var createdCard = CreateCard(targetCard.Definition);
        AddCardToHand(state, isFirstPlayer, createdCard);
        createdCards[playedCard.Card.CardInstanceId] = createdCard;
        events.Add(
            CreateCreatedEvent(
                GetParticipantPlayerId(state, isFirstPlayer),
                playedCard.Card.Definition,
                createdCard.Definition
            )
        );
    }

    private static void ResolveCatalyst(
        GameSessionState state,
        bool isFirstPlayer,
        PendingGameBatchCardState playedCard,
        Dictionary<Guid, GameCardState> createdCards,
        List<GameplayEvent> events
    )
    {
        var hand = GetHand(state, isFirstPlayer);
        if (
            playedCard.TargetCardInstanceId is not { } targetCardId
            || playedCard.TargetResourceColor is not { } targetColor
            || !TryRemoveCardFromHand(hand, targetCardId, out var removedCard)
            || removedCard is null
            || !GameCardCatalog.IsBaseResource(removedCard.Definition)
        )
        {
            events.Add(
                CreateFizzledEvent(
                    GetParticipantPlayerId(state, isFirstPlayer),
                    playedCard.Card.Definition
                )
            );
            return;
        }

        if (!GameCardCatalog.TryGetBaseDefinition(targetColor, out var convertedDefinition))
        {
            events.Add(
                CreateFizzledEvent(
                    GetParticipantPlayerId(state, isFirstPlayer),
                    playedCard.Card.Definition
                )
            );
            return;
        }

        state.RoundHadHandChange = true;
        var createdCard = CreateCard(convertedDefinition);
        AddCardToHand(state, isFirstPlayer, createdCard);
        createdCards[playedCard.Card.CardInstanceId] = createdCard;
        events.Add(
            CreateConvertedEvent(
                GetParticipantPlayerId(state, isFirstPlayer),
                playedCard.Card.Definition,
                removedCard.Definition,
                createdCard.Definition
            )
        );
    }

    private static void ResolveReclaim(
        GameSessionState state,
        PendingGameBatchState batch,
        PendingGameBatchCardState playedCard,
        List<GameplayEvent> events
    )
    {
        if (playedCard.TargetCardInstanceId is not { } targetCardId)
        {
            events.Add(
                CreateFizzledEvent(
                    GetBatchParticipantPlayerId(state, batch.PlayerId),
                    playedCard.Card.Definition
                )
            );
            return;
        }

        var targetCard = batch.Cards.FirstOrDefault(card =>
            card.Card.CardInstanceId == targetCardId
        );
        if (
            targetCard is null
            || targetCard.Card.CardInstanceId == playedCard.Card.CardInstanceId
            || !GameCardCatalog.IsMarketCard(targetCard.Card.Definition)
        )
        {
            events.Add(
                CreateFizzledEvent(
                    GetBatchParticipantPlayerId(state, batch.PlayerId),
                    playedCard.Card.Definition
                )
            );
            return;
        }

        targetCard.ReturnToHand = true;
        events.Add(
            CreateScheduledReturnToHandEvent(
                GetBatchParticipantPlayerId(state, batch.PlayerId),
                playedCard.Card.Definition,
                targetCard.Card.Definition
            )
        );
    }

    private static void ResolveScout(
        GameSessionState state,
        bool isFirstPlayer,
        List<GameplayEvent> events
    )
    {
        if (isFirstPlayer)
        {
            state.FirstPlayerScoutOverride = true;
        }
        else
        {
            state.SecondPlayerScoutOverride = true;
        }

        events.Add(
            CreateResolvedEvent(
                GetParticipantPlayerId(state, isFirstPlayer),
                GameCardDefinition.Scout
            )
        );
    }

    private static void ResolveExtracts(
        GameSessionState state,
        PendingGameBatchState batch,
        bool isFirstPlayer,
        Dictionary<Guid, GameCardState> createdCards,
        List<GameplayEvent> events
    )
    {
        foreach (
            var playedCard in batch.Cards.Where(card =>
                card.Card.Definition == GameCardDefinition.Extract
            )
        )
        {
            var createdDefinition = playedCard.ChosenResourceColor switch
            {
                GameResourceColor.Red => GameCardDefinition.Red,
                GameResourceColor.Yellow => GameCardDefinition.Yellow,
                GameResourceColor.Blue => GameCardDefinition.Blue,
                _ => (GameCardDefinition?)null,
            };

            if (createdDefinition is null)
            {
                events.Add(
                    CreateFizzledEvent(
                        GetParticipantPlayerId(state, isFirstPlayer),
                        playedCard.Card.Definition
                    )
                );
                continue;
            }

            var createdCard = CreateCard(createdDefinition.Value);
            AddCardToHand(state, isFirstPlayer, createdCard);
            createdCards[playedCard.Card.CardInstanceId] = createdCard;
            events.Add(
                CreateCreatedEvent(
                    GetParticipantPlayerId(state, isFirstPlayer),
                    playedCard.Card.Definition,
                    createdCard.Definition
                )
            );
        }
    }

    private static void ResolveRefines(
        GameSessionState state,
        PendingGameBatchState batch,
        bool isFirstPlayer,
        Dictionary<Guid, GameCardState> createdCards,
        List<GameplayEvent> events
    )
    {
        foreach (
            var playedCard in batch.Cards.Where(card =>
                card.Card.Definition == GameCardDefinition.Refine
            )
        )
        {
            var hand = GetHand(state, isFirstPlayer);
            if (
                !TryResolveConsumedCards(hand, playedCard, createdCards, out var consumedCards)
                || consumedCards.Count != 2
                || GameCardCatalog.TryGetRefineOutput(
                    consumedCards[0].Definition,
                    consumedCards[1].Definition
                )
                    is not { } createdDefinition
            )
            {
                events.Add(
                    CreateFizzledEvent(
                        GetParticipantPlayerId(state, isFirstPlayer),
                        playedCard.Card.Definition
                    )
                );
                continue;
            }

            RemoveConsumedCards(state, isFirstPlayer, consumedCards);
            var createdCard = CreateCard(createdDefinition);
            AddCardToHand(state, isFirstPlayer, createdCard);
            createdCards[playedCard.Card.CardInstanceId] = createdCard;
            events.Add(
                CreateCreatedEvent(
                    GetParticipantPlayerId(state, isFirstPlayer),
                    playedCard.Card.Definition,
                    createdDefinition
                )
            );
        }
    }

    private static bool ResolveProduces(
        GameSessionState state,
        PendingGameBatchState batch,
        bool isFirstPlayer,
        Dictionary<Guid, GameCardState> createdCards,
        List<GameplayEvent> events
    )
    {
        var producedVictory = false;

        foreach (
            var playedCard in batch.Cards.Where(card =>
                card.Card.Definition == GameCardDefinition.Produce
            )
        )
        {
            if (
                !GameCraftingRules.TryGetProduceResult(
                    playedCard.CraftedCardDefinition,
                    out var craftedDefinition
                )
            )
            {
                events.Add(
                    CreateFizzledEvent(
                        GetParticipantPlayerId(state, isFirstPlayer),
                        playedCard.Card.Definition
                    )
                );
                continue;
            }

            var hand = GetHand(state, isFirstPlayer);
            if (
                !TryResolveConsumedCards(hand, playedCard, createdCards, out var consumedCards)
                || !GameCraftingRules.MatchesProduceRecipe(
                    craftedDefinition,
                    consumedCards.Select(card => card.Definition).ToArray()
                )
            )
            {
                events.Add(
                    CreateFizzledEvent(
                        GetParticipantPlayerId(state, isFirstPlayer),
                        playedCard.Card.Definition
                    )
                );
                continue;
            }

            RemoveConsumedCards(state, isFirstPlayer, consumedCards);
            var createdCard = CreateCard(craftedDefinition);
            AddCardToHand(state, isFirstPlayer, createdCard);
            createdCards[playedCard.Card.CardInstanceId] = createdCard;
            producedVictory |= craftedDefinition == GameCardDefinition.Victory;
            events.Add(
                CreateCreatedEvent(
                    GetParticipantPlayerId(state, isFirstPlayer),
                    playedCard.Card.Definition,
                    craftedDefinition
                )
            );
        }

        return producedVictory;
    }

    private static bool TryResolveConsumedCards(
        List<GameCardState> hand,
        PendingGameBatchCardState playedCard,
        Dictionary<Guid, GameCardState> createdCards,
        out List<GameCardState> consumedCards
    )
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

                resolvedCard = hand.FirstOrDefault(card =>
                    card.CardInstanceId == createdCard.CardInstanceId
                );
            }

            if (resolvedCard is null)
            {
                return false;
            }

            if (
                reference.ProducedCardDefinition is not null
                && reference.ProducedCardDefinition != resolvedCard.Definition
            )
            {
                return false;
            }

            consumedCards.Add(resolvedCard);
        }

        return true;
    }

    private static void RemoveConsumedCards(
        GameSessionState state,
        bool isFirstPlayer,
        IEnumerable<GameCardState> cards
    )
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

    private static void CleanupBatch(
        GameSessionState state,
        PendingGameBatchState batch,
        bool isFirstPlayer,
        List<GameplayEvent> events
    )
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
                events.Add(
                    CreateReturnedToHandEvent(
                        GetParticipantPlayerId(state, isFirstPlayer),
                        playedCard.Card.Definition
                    )
                );
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
        DateTime nowUtc
    )
    {
        var firstHasVictory = state.FirstPlayerHand.Any(card =>
            card.Definition == GameCardDefinition.Victory
        );
        var secondHasVictory = state.SecondPlayerHand.Any(card =>
            card.Definition == GameCardDefinition.Victory
        );

        if (firstHasVictory && secondHasVictory)
        {
            state.Completion = new GameCompletionState
            {
                Reason = GameCompletionReason.Draw,
                CompletedAtUtc = nowUtc,
            };
            state.Phase = GamePhase.Completed;
            return;
        }

        if (firstHasVictory || secondHasVictory)
        {
            state.Completion = new GameCompletionState
            {
                Reason = GameCompletionReason.Victory,
                WinnerPlayerId = firstHasVictory
                    ? state.FirstPlayer!.PlayerId
                    : state.SecondPlayer!.PlayerId,
                CompletedAtUtc = nowUtc,
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
                CompletedAtUtc = nowUtc,
            };
            state.Phase = GamePhase.Completed;
        }
    }

    private static List<PendingGameBatchCardState> CloneBatchCards(
        IEnumerable<PendingGameBatchCardState> cards
    ) =>
        cards
            .Select(card => new PendingGameBatchCardState
            {
                Card = CloneCard(card.Card),
                ChosenResourceColor = card.ChosenResourceColor,
                CraftedCardDefinition = card.CraftedCardDefinition,
                TargetResourceColor = card.TargetResourceColor,
                TargetCardInstanceId = card.TargetCardInstanceId,
                ReturnToHand = card.ReturnToHand,
                ConsumedCards = card.ConsumedCards.Select(CloneReference).ToList(),
            })
            .ToList();

    private static GameCardState CloneCard(GameCardState card) =>
        new() { CardInstanceId = card.CardInstanceId, Definition = card.Definition };

    private static GameCardReferenceState CloneReference(GameCardReferenceState reference) =>
        new()
        {
            CardInstanceId = reference.CardInstanceId,
            ProducedByCardInstanceId = reference.ProducedByCardInstanceId,
            ProducedCardDefinition = reference.ProducedCardDefinition,
        };

    private static PendingGameBatchState ClonePendingBatch(PendingGameBatchState batch) =>
        new() { PlayerId = batch.PlayerId, Cards = CloneBatchCards(batch.Cards) };

    private static ResolvedGameBatchState CloneResolvedBatch(ResolvedGameBatchState batch) =>
        new()
        {
            RoundNumber = batch.RoundNumber,
            ResolvedAtUtc = batch.ResolvedAtUtc,
            Players = batch
                .Players.Select(player => new ResolvedGamePlayerBatchState
                {
                    PlayerId = player.PlayerId,
                    ProducedVictory = player.ProducedVictory,
                    Cards = CloneBatchCards(player.Cards),
                })
                .ToList(),
        };

    private static Guid GetParticipantPlayerId(GameSessionState state, bool isFirstPlayer) =>
        (isFirstPlayer ? state.FirstPlayer : state.SecondPlayer)?.PlayerId ?? Guid.Empty;

    private static Guid GetBatchParticipantPlayerId(GameSessionState state, Guid playerId) =>
        TryGetParticipant(state, playerId)?.Player.PlayerId ?? playerId;

    private static GameplayEvent CreateFizzledEvent(
        Guid actorPlayerId,
        GameCardDefinition sourceCardDefinition
    ) => new(GameplayEventKind.Fizzled, actorPlayerId, sourceCardDefinition, null, null, null);

    private static GameplayEvent CreateDiscardedEvent(
        Guid actorPlayerId,
        GameCardDefinition sourceCardDefinition,
        Guid targetPlayerId,
        GameCardDefinition targetCardDefinition
    ) =>
        new(
            GameplayEventKind.DiscardedCard,
            actorPlayerId,
            sourceCardDefinition,
            targetPlayerId,
            targetCardDefinition,
            null
        );

    private static GameplayEvent CreateCreatedEvent(
        Guid actorPlayerId,
        GameCardDefinition sourceCardDefinition,
        GameCardDefinition producedCardDefinition
    ) =>
        new(
            GameplayEventKind.CreatedCard,
            actorPlayerId,
            sourceCardDefinition,
            null,
            null,
            producedCardDefinition
        );

    private static GameplayEvent CreateConvertedEvent(
        Guid actorPlayerId,
        GameCardDefinition sourceCardDefinition,
        GameCardDefinition targetCardDefinition,
        GameCardDefinition producedCardDefinition
    ) =>
        new(
            GameplayEventKind.ConvertedCard,
            actorPlayerId,
            sourceCardDefinition,
            null,
            targetCardDefinition,
            producedCardDefinition
        );

    private static GameplayEvent CreateScheduledReturnToHandEvent(
        Guid actorPlayerId,
        GameCardDefinition sourceCardDefinition,
        GameCardDefinition targetCardDefinition
    ) =>
        new(
            GameplayEventKind.ScheduledReturnToHand,
            actorPlayerId,
            sourceCardDefinition,
            null,
            targetCardDefinition,
            null
        );

    private static GameplayEvent CreateReturnedToHandEvent(
        Guid actorPlayerId,
        GameCardDefinition sourceCardDefinition
    ) =>
        new(
            GameplayEventKind.ReturnedToHand,
            actorPlayerId,
            sourceCardDefinition,
            null,
            null,
            null
        );

    private static GameplayEvent CreateResolvedEvent(
        Guid actorPlayerId,
        GameCardDefinition sourceCardDefinition
    ) => new(GameplayEventKind.Resolved, actorPlayerId, sourceCardDefinition, null, null, null);

    private static void Shuffle(List<GameCardState> list)
    {
        for (var index = list.Count - 1; index > 0; index--)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (list[index], list[swapIndex]) = (list[swapIndex], list[index]);
        }
    }
}

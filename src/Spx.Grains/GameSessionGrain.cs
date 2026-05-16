using Orleans;
using Orleans.Runtime;
using Spx.Contracts;

namespace Spx.Grains;

public sealed class GameSessionGrain(
    [PersistentState("game-session")] IPersistentState<GameSessionGrainState> sessionState)
    : Grain, IGameSessionGrain
{
    public async Task InitializeAsync(InitializeGameSessionCommand command)
    {
        GameSessionEngine.Initialize(sessionState.State, command);
        await sessionState.WriteStateAsync();
    }

    public async Task<GameSessionView> SubmitAcquireAsync(SubmitAcquireCardCommand command)
    {
        var view = GameSessionEngine.SubmitAcquire(
            sessionState.State,
            this.GetPrimaryKey(),
            command);

        await sessionState.WriteStateAsync();
        return view;
    }

    public async Task<SubmitPlayBatchResult> SubmitPlayBatchAsync(SubmitPlayBatchCommand command)
    {
        var view = GameSessionEngine.SubmitPlayBatch(
            sessionState.State,
            this.GetPrimaryKey(),
            command,
            DateTime.UtcNow);

        await sessionState.WriteStateAsync();
        return view;
    }

    public Task<GameSessionView?> GetPlayerViewAsync(GetGameSessionViewQuery query)
        => Task.FromResult(GameSessionEngine.GetSessionView(sessionState.State, this.GetPrimaryKey(), query));

    public async Task<GameSessionView> AbandonAsync(AbandonGameSessionCommand command)
    {
        var view = GameSessionEngine.AbandonPlayer(sessionState.State, this.GetPrimaryKey(), command, DateTime.UtcNow);
        await sessionState.WriteStateAsync();
        return view;
    }
}

[GenerateSerializer]
public sealed class GameSessionGrainState
{
    [Id(0)] public GameSessionParticipantView? FirstPlayer { get; set; }

    [Id(1)] public GameSessionParticipantView? SecondPlayer { get; set; }

    [Id(2)] public bool FirstPlayerActive { get; set; }

    [Id(3)] public bool SecondPlayerActive { get; set; }

    [Id(4)] public int RoundNumber { get; set; } = 1;

    [Id(5)] public GamePhase Phase { get; set; } = GamePhase.Acquire;

    [Id(6)] public List<GameCardState> MarketDeck { get; set; } = [];

    [Id(7)] public List<GameCardState> VisibleMarketCards { get; set; } = [];

    [Id(8)] public List<GameCardState> FirstPlayerHand { get; set; } = [];

    [Id(9)] public List<GameCardState> SecondPlayerHand { get; set; } = [];

    [Id(10)] public PendingGameBatchState? FirstPlayerPendingBatch { get; set; }

    [Id(11)] public PendingGameBatchState? SecondPlayerPendingBatch { get; set; }

    [Id(12)] public ResolvedGameBatchState? LastResolvedBatch { get; set; }

    [Id(13)] public bool FirstPlayerScoutOverride { get; set; }

    [Id(14)] public bool SecondPlayerScoutOverride { get; set; }

    [Id(15)] public string? CurrentAcquireFirstUserId { get; set; }

    [Id(16)] public string? CurrentAcquireSecondUserId { get; set; }

    [Id(17)] public bool AcquireFirstCompleted { get; set; }

    [Id(18)] public bool AcquireSecondCompleted { get; set; }

    [Id(19)] public string? PreviousAcquireSecondUserId { get; set; }

    [Id(20)] public string? InitialTieBreakerFirstUserId { get; set; }

    [Id(21)] public GameCompletionState? Completion { get; set; }

    [Id(22)] public int ConsecutiveStalemateRounds { get; set; }

    [Id(23)] public bool RoundHadHandChange { get; set; }

    [Id(24)] public int AcquirePicksCompletedInPhase { get; set; }
}

[GenerateSerializer]
public sealed class GameCardState
{
    [Id(0)] public Guid CardInstanceId { get; set; }

    [Id(1)] public GameCardDefinition Definition { get; set; }
}

[GenerateSerializer]
public sealed class GameCardReferenceState
{
    [Id(0)] public Guid? CardInstanceId { get; set; }

    [Id(1)] public Guid? ProducedByCardInstanceId { get; set; }

    [Id(2)] public GameCardDefinition? ProducedCardDefinition { get; set; }
}

[GenerateSerializer]
public sealed class PendingGameBatchState
{
    [Id(0)] public string UserId { get; set; } = string.Empty;

    [Id(1)] public List<PendingGameBatchCardState> Cards { get; set; } = [];
}

[GenerateSerializer]
public sealed class PendingGameBatchCardState
{
    [Id(0)] public GameCardState Card { get; set; } = new();

    [Id(1)] public GameResourceColor? ChosenResourceColor { get; set; }

    [Id(2)] public GameCardDefinition? CraftedCardDefinition { get; set; }

    [Id(3)] public GameResourceColor? TargetResourceColor { get; set; }

    [Id(4)] public Guid? TargetCardInstanceId { get; set; }

    [Id(5)] public List<GameCardReferenceState> ConsumedCards { get; set; } = [];

    [Id(6)] public bool ReturnToHand { get; set; }
}

[GenerateSerializer]
public sealed class ResolvedGameBatchState
{
    [Id(0)] public int RoundNumber { get; set; }

    [Id(1)] public List<ResolvedGamePlayerBatchState> Players { get; set; } = [];

    [Id(3)] public DateTime ResolvedAtUtc { get; set; }
}

[GenerateSerializer]
public sealed class ResolvedGamePlayerBatchState
{
    [Id(0)] public string UserId { get; set; } = string.Empty;

    [Id(1)] public List<PendingGameBatchCardState> Cards { get; set; } = [];

    [Id(2)] public bool ProducedVictory { get; set; }
}

[GenerateSerializer]
public sealed class GameCompletionState
{
    [Id(0)] public GameCompletionReason Reason { get; set; }

    [Id(1)] public string? WinnerUserId { get; set; }

    [Id(2)] public DateTime CompletedAtUtc { get; set; }
}

internal static class GameSessionEngine
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

    public static void Initialize(GameSessionGrainState state, InitializeGameSessionCommand command)
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

    public static GameSessionView SubmitAcquire(
        GameSessionGrainState state,
        Guid gameId,
        SubmitAcquireCardCommand command)
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
            return CreatePlayerView(state, gameId, participant.Player.UserId);
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

        return CreatePlayerView(state, gameId, participant.Player.UserId);
    }

    public static SubmitPlayBatchResult SubmitPlayBatch(
        GameSessionGrainState state,
        Guid gameId,
        SubmitPlayBatchCommand command,
        DateTime nowUtc)
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

        IReadOnlyList<string> gameplayEvents = [];

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

        return new SubmitPlayBatchResult(CreatePlayerView(state, gameId, participant.Player.UserId), gameplayEvents);
    }

    public static GameSessionView? GetSessionView(
        GameSessionGrainState state,
        Guid gameId,
        GetGameSessionViewQuery query)
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

    public static GameSessionView AbandonPlayer(
        GameSessionGrainState state,
        Guid gameId,
        AbandonGameSessionCommand command,
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
        GameSessionParticipantView existingFirstPlayer,
        GameSessionParticipantView existingSecondPlayer,
        GameSessionParticipantView incomingFirstPlayer,
        GameSessionParticipantView incomingSecondPlayer)
        => (existingFirstPlayer.PlayerId == incomingFirstPlayer.PlayerId && existingSecondPlayer.PlayerId == incomingSecondPlayer.PlayerId)
            || (existingFirstPlayer.PlayerId == incomingSecondPlayer.PlayerId && existingSecondPlayer.PlayerId == incomingFirstPlayer.PlayerId);

    private static void EnsureInitialized(GameSessionGrainState state)
    {
        if (state.FirstPlayer is null || state.SecondPlayer is null)
        {
            throw new InvalidOperationException("The game session has not been initialized.");
        }
    }

    private static void EnsureNotCompleted(GameSessionGrainState state)
    {
        if (state.Completion is not null)
        {
            throw new InvalidOperationException("The game session is already complete.");
        }
    }

    private static ParticipantState GetParticipant(GameSessionGrainState state, string userId)
        => TryGetParticipant(state, userId) ?? throw new InvalidOperationException("The current user is not part of this game session.");

    private static ParticipantState? TryGetParticipant(GameSessionGrainState state, string userId)
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

    private static GameSessionView CreatePlayerView(
        GameSessionGrainState state,
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

        return new GameSessionView(
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

    private sealed record ParticipantState(GameSessionParticipantView Player, GameSessionParticipantView Opponent, bool IsActive, bool IsFirstPlayer);

    private static GamePlayerStateView CreatePlayerStateView(
        GameSessionGrainState state,
        GameSessionParticipantView participant,
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

    private static GameResolvedBatchView? CreateResolvedBatchView(GameSessionGrainState state)
        => state.LastResolvedBatch is null
            ? null
            : new GameResolvedBatchView(
                state.LastResolvedBatch.RoundNumber,
                state.LastResolvedBatch.Players.Select(player => new GameResolvedPlayerBatchView(
                    GetParticipant(state, player.UserId).Player,
                    player.Cards.Select(CreateBatchCardView).ToArray(),
                    player.ProducedVictory)).ToArray(),
                state.LastResolvedBatch.ResolvedAtUtc);

    private static GameCompletionView? CreateCompletionView(GameSessionGrainState state)
    {
        if (state.Completion is null)
        {
            return null;
        }

        var winner = state.Completion.WinnerUserId is null
            ? null
            : GetParticipant(state, state.Completion.WinnerUserId).Player;

        return new GameCompletionView(state.Completion.Reason, winner, state.Completion.CompletedAtUtc);
    }

    private static GameCardInstanceView CreateCardView(GameCardState card)
        => new(
            card.CardInstanceId,
            card.Definition,
            GameCardCatalog.GetDisplayName(card.Definition),
            GameCardCatalog.GetCategory(card.Definition),
            GameCardCatalog.GetResourceColor(card.Definition));

    private static GameBatchCardView CreateBatchCardView(PendingGameBatchCardState card)
        => new(
            CreateCardView(card.Card),
            card.ChosenResourceColor,
            card.CraftedCardDefinition,
            card.TargetResourceColor,
            card.TargetCardInstanceId,
            card.ConsumedCards.Select(reference => new GameCardReferenceView(reference.CardInstanceId, reference.ProducedByCardInstanceId, reference.ProducedCardDefinition)).ToArray());

    private static List<GameCardState> CreateInitialMarketDeck()
        => InitialMarketDeck.Select(definition => CreateCard(definition)).ToList();

    private static GameCardState CreateCard(GameCardDefinition definition)
        => new() { CardInstanceId = Guid.NewGuid(), Definition = definition };

    private static void StartAcquirePhase(GameSessionGrainState state)
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

    private static void RefillVisibleMarket(GameSessionGrainState state)
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

    private static string? GetCurrentAcquireUserId(GameSessionGrainState state)
    {
        if (state.Phase != GamePhase.Acquire || state.VisibleMarketCards.Count == 0 || state.AcquirePicksCompletedInPhase >= AcquirePicksPerPhase)
        {
            return null;
        }

        return state.AcquirePicksCompletedInPhase % 2 == 0
            ? state.CurrentAcquireFirstUserId
            : state.CurrentAcquireSecondUserId;
    }

    private static string DetermineAcquireFirstUserId(GameSessionGrainState state)
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

    private static string GetOtherUserId(GameSessionGrainState state, string? userId)
        => string.Equals(state.FirstPlayer!.UserId, userId, StringComparison.Ordinal)
            ? state.SecondPlayer!.UserId
            : state.FirstPlayer!.UserId;

    private static List<GameCardState> GetHand(GameSessionGrainState state, bool isFirstPlayer)
        => isFirstPlayer ? state.FirstPlayerHand : state.SecondPlayerHand;

    private static PendingGameBatchState? GetPendingBatch(GameSessionGrainState state, bool isFirstPlayer)
        => isFirstPlayer ? state.FirstPlayerPendingBatch : state.SecondPlayerPendingBatch;

    private static void SetPendingBatch(GameSessionGrainState state, bool isFirstPlayer, PendingGameBatchState batch)
    {
        if (isFirstPlayer)
        {
            state.FirstPlayerPendingBatch = batch;
            return;
        }

        state.SecondPlayerPendingBatch = batch;
    }

    private static void AddCardToHand(GameSessionGrainState state, bool isFirstPlayer, GameCardState card)
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
        SubmitPlayBatchCommand command,
        List<GameCardState> ownHand,
        List<GameCardState> opponentHand)
    {
        if (command.Cards.Count > GameCardCatalog.MaxBatchSize)
        {
            throw new InvalidOperationException($"Players may lock at most {GameCardCatalog.MaxBatchSize} cards in a batch.");
        }

        var handById = ownHand.ToDictionary(card => card.CardInstanceId);
        var opponentHandById = opponentHand.ToDictionary(card => card.CardInstanceId);

        var selectedCardsById = new Dictionary<Guid, GameBatchCardCommand>();
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
        GameBatchCardCommand selectedCard,
        GameCardState handCard,
        IReadOnlyDictionary<Guid, GameBatchCardCommand> selectedCardsById,
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
                ValidateRefine(selectedCard);
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

    private static void ValidateExtract(GameBatchCardCommand selectedCard)
        => EnsureNoExtraChoices(selectedCard, allowCraftedCard: false, allowTargetCard: false, allowTargetResource: false, allowConsumedCards: false, allowChosenResource: true);

    private static void ValidateRefine(GameBatchCardCommand selectedCard)
        => EnsureNoExtraChoices(selectedCard, allowCraftedCard: false, allowTargetCard: false, allowTargetResource: false, allowConsumedCards: true, allowChosenResource: false);

    private static void ValidateProduce(GameBatchCardCommand selectedCard)
        => EnsureNoExtraChoices(selectedCard, allowCraftedCard: true, allowTargetCard: false, allowTargetResource: false, allowConsumedCards: true, allowChosenResource: false);

    private static void EnsureNoExtraChoices(
        GameBatchCardCommand selectedCard,
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
        GameCardReferenceCommand reference,
        IReadOnlyDictionary<Guid, GameBatchCardCommand> selectedCardsById,
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
        GameBatchCardCommand selectedCard,
        GameCardDefinition sourceDefinition,
        IReadOnlyDictionary<Guid, GameBatchCardCommand> selectedCardsById,
        IReadOnlyDictionary<Guid, GameCardState> handById,
        ISet<Guid> visitedCards)
        => sourceDefinition switch
        {
            GameCardDefinition.Extract when selectedCard.ChosenResourceColor is { } chosenColor && GameCardCatalog.TryGetBaseDefinition(chosenColor, out var extractedDefinition)
                => extractedDefinition,
            GameCardDefinition.Extract => throw new InvalidOperationException("Extract must declare a base resource color."),
            GameCardDefinition.Refine => ResolveRefineOutput(selectedCard, selectedCardsById, handById, visitedCards),
            GameCardDefinition.Produce when selectedCard.CraftedCardDefinition is { } craftedDefinition && GameCardCatalog.TryGetProduceRecipe(craftedDefinition, out _) => craftedDefinition,
            _ => throw new InvalidOperationException("This card does not produce a consumable output.")
        };

    private static GameCardDefinition ResolveRefineOutput(
        GameBatchCardCommand selectedCard,
        IReadOnlyDictionary<Guid, GameBatchCardCommand> selectedCardsById,
        IReadOnlyDictionary<Guid, GameCardState> handById,
        ISet<Guid> visitedCards)
    {
        var consumedDefinitions = selectedCard.ConsumedCards
            .Select(reference => ResolveReferenceDefinition(reference, selectedCardsById, handById, GameCardCatalog.GetResolutionStep(GameCardDefinition.Refine), visitedCards))
            .ToArray();

        if (consumedDefinitions.Length != 2 || GameCardCatalog.TryGetRefineOutput(consumedDefinitions[0], consumedDefinitions[1]) is not { } output)
        {
            throw new InvalidOperationException("Refine requires a valid pair of base resource inputs.");
        }

        return output;
    }

    private static IReadOnlyList<string> ResolveRound(GameSessionGrainState state, DateTime nowUtc)
    {
        var firstBatch = state.FirstPlayerPendingBatch ?? throw new InvalidOperationException("Both players must lock a batch before resolution.");
        var secondBatch = state.SecondPlayerPendingBatch ?? throw new InvalidOperationException("Both players must lock a batch before resolution.");
        var firstCreatedCards = new Dictionary<Guid, GameCardState>();
        var secondCreatedCards = new Dictionary<Guid, GameCardState>();
        var events = new List<string>();
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
        GameSessionGrainState state,
        PendingGameBatchState batch,
        bool isFirstPlayer,
        Dictionary<Guid, GameCardState> createdCards,
        List<string> events)
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
        GameSessionGrainState state,
        bool isFirstPlayer,
        PendingGameBatchCardState playedCard,
        Func<GameCardDefinition, bool> predicate,
        string effectName,
        List<string> events)
    {
        var opponentHand = GetHand(state, !isFirstPlayer);
        if (playedCard.TargetCardInstanceId is not { } targetCardId
            || !TryRemoveCardFromHand(opponentHand, targetCardId, out var removedCard)
            || removedCard is null
            || !predicate(removedCard.Definition))
        {
            events.Add($"{GetParticipantName(state, isFirstPlayer)}'s {effectName} fizzled.");
            return;
        }

        state.RoundHadHandChange = true;
        events.Add($"{GetParticipantName(state, isFirstPlayer)} resolved {effectName} and discarded {removedCard.Definition} from {GetParticipantName(state, !isFirstPlayer)}.");
    }

    private static void ResolveReplicate(
        GameSessionGrainState state,
        bool isFirstPlayer,
        PendingGameBatchCardState playedCard,
        Dictionary<Guid, GameCardState> createdCards,
        List<string> events)
    {
        var hand = GetHand(state, isFirstPlayer);
        if (playedCard.TargetCardInstanceId is not { } targetCardId)
        {
            events.Add($"{GetParticipantName(state, isFirstPlayer)}'s Replicate fizzled.");
            return;
        }

        var targetCard = hand.FirstOrDefault(card => card.CardInstanceId == targetCardId);
        if (targetCard is null || !GameCardCatalog.IsBaseResource(targetCard.Definition))
        {
            events.Add($"{GetParticipantName(state, isFirstPlayer)}'s Replicate fizzled.");
            return;
        }

        var createdCard = CreateCard(targetCard.Definition);
        AddCardToHand(state, isFirstPlayer, createdCard);
        createdCards[playedCard.Card.CardInstanceId] = createdCard;
        events.Add($"{GetParticipantName(state, isFirstPlayer)} resolved Replicate and created {createdCard.Definition}.");
    }

    private static void ResolveCatalyst(
        GameSessionGrainState state,
        bool isFirstPlayer,
        PendingGameBatchCardState playedCard,
        Dictionary<Guid, GameCardState> createdCards,
        List<string> events)
    {
        var hand = GetHand(state, isFirstPlayer);
        if (playedCard.TargetCardInstanceId is not { } targetCardId
            || playedCard.TargetResourceColor is not { } targetColor
            || !TryRemoveCardFromHand(hand, targetCardId, out var removedCard)
            || removedCard is null
            || !GameCardCatalog.IsBaseResource(removedCard.Definition))
        {
            events.Add($"{GetParticipantName(state, isFirstPlayer)}'s Catalyst fizzled.");
            return;
        }

        if (!GameCardCatalog.TryGetBaseDefinition(targetColor, out var convertedDefinition))
        {
            events.Add($"{GetParticipantName(state, isFirstPlayer)}'s Catalyst fizzled.");
            return;
        }

        state.RoundHadHandChange = true;
        var createdCard = CreateCard(convertedDefinition);
        AddCardToHand(state, isFirstPlayer, createdCard);
        createdCards[playedCard.Card.CardInstanceId] = createdCard;
        events.Add($"{GetParticipantName(state, isFirstPlayer)} resolved Catalyst and converted {removedCard.Definition} into {createdCard.Definition}.");
    }

    private static void ResolveReclaim(GameSessionGrainState state, PendingGameBatchState batch, PendingGameBatchCardState playedCard, List<string> events)
    {
        if (playedCard.TargetCardInstanceId is not { } targetCardId)
        {
            events.Add($"{GetParticipantName(state, batch.UserId)}'s Reclaim fizzled.");
            return;
        }

        var targetCard = batch.Cards.FirstOrDefault(card => card.Card.CardInstanceId == targetCardId);
        if (targetCard is null || targetCard.Card.CardInstanceId == playedCard.Card.CardInstanceId || !GameCardCatalog.IsMarketCard(targetCard.Card.Definition))
        {
            events.Add($"{GetParticipantName(state, batch.UserId)}'s Reclaim fizzled.");
            return;
        }

        targetCard.ReturnToHand = true;
        events.Add($"{GetParticipantName(state, batch.UserId)} resolved Reclaim and will return {targetCard.Card.Definition} to hand.");
    }

    private static void ResolveScout(GameSessionGrainState state, bool isFirstPlayer, List<string> events)
    {
        if (isFirstPlayer)
        {
            state.FirstPlayerScoutOverride = true;
        }
        else
        {
            state.SecondPlayerScoutOverride = true;
        }

        events.Add($"{GetParticipantName(state, isFirstPlayer)} resolved Scout.");
    }

    private static void ResolveExtracts(
        GameSessionGrainState state,
        PendingGameBatchState batch,
        bool isFirstPlayer,
        Dictionary<Guid, GameCardState> createdCards,
        List<string> events)
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
                events.Add($"{GetParticipantName(state, isFirstPlayer)}'s Extract fizzled.");
                continue;
            }

            var createdCard = CreateCard(createdDefinition.Value);
            AddCardToHand(state, isFirstPlayer, createdCard);
            createdCards[playedCard.Card.CardInstanceId] = createdCard;
            events.Add($"{GetParticipantName(state, isFirstPlayer)} extracted {createdCard.Definition}.");
        }
    }

    private static void ResolveRefines(
        GameSessionGrainState state,
        PendingGameBatchState batch,
        bool isFirstPlayer,
        Dictionary<Guid, GameCardState> createdCards,
        List<string> events)
    {
        foreach (var playedCard in batch.Cards.Where(card => card.Card.Definition == GameCardDefinition.Refine))
        {
            var hand = GetHand(state, isFirstPlayer);
            if (!TryResolveConsumedCards(hand, playedCard, createdCards, out var consumedCards)
                || consumedCards.Count != 2
                || GameCardCatalog.TryGetRefineOutput(consumedCards[0].Definition, consumedCards[1].Definition) is not { } createdDefinition)
            {
                events.Add($"{GetParticipantName(state, isFirstPlayer)}'s Refine fizzled.");
                continue;
            }

            RemoveConsumedCards(state, isFirstPlayer, consumedCards);
            var createdCard = CreateCard(createdDefinition);
            AddCardToHand(state, isFirstPlayer, createdCard);
            createdCards[playedCard.Card.CardInstanceId] = createdCard;
            events.Add($"{GetParticipantName(state, isFirstPlayer)} refined {createdDefinition}.");
        }
    }

    private static bool ResolveProduces(
        GameSessionGrainState state,
        PendingGameBatchState batch,
        bool isFirstPlayer,
        Dictionary<Guid, GameCardState> createdCards,
        List<string> events)
    {
        var producedVictory = false;

        foreach (var playedCard in batch.Cards.Where(card => card.Card.Definition == GameCardDefinition.Produce))
        {
            if (playedCard.CraftedCardDefinition is not { } craftedDefinition || !GameCardCatalog.TryGetProduceRecipe(craftedDefinition, out var recipe))
            {
                events.Add($"{GetParticipantName(state, isFirstPlayer)}'s Produce fizzled.");
                continue;
            }

            var hand = GetHand(state, isFirstPlayer);
            if (!TryResolveConsumedCards(hand, playedCard, createdCards, out var consumedCards)
                || !GameCardCatalog.MatchesRecipe(consumedCards.Select(card => card.Definition).ToArray(), recipe))
            {
                events.Add($"{GetParticipantName(state, isFirstPlayer)}'s Produce fizzled.");
                continue;
            }

            RemoveConsumedCards(state, isFirstPlayer, consumedCards);
            var createdCard = CreateCard(craftedDefinition);
            AddCardToHand(state, isFirstPlayer, createdCard);
            createdCards[playedCard.Card.CardInstanceId] = createdCard;
            producedVictory |= craftedDefinition == GameCardDefinition.Victory;
            events.Add($"{GetParticipantName(state, isFirstPlayer)} produced {craftedDefinition}.");
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

    private static void RemoveConsumedCards(GameSessionGrainState state, bool isFirstPlayer, IEnumerable<GameCardState> cards)
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

    private static void CleanupBatch(GameSessionGrainState state, PendingGameBatchState batch, bool isFirstPlayer, List<string> events)
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
                events.Add($"{GetParticipantName(state, isFirstPlayer)} returned {playedCard.Card.Definition} to hand.");
                continue;
            }

            state.MarketDeck.Add(playedCard.Card);
        }
    }

    private static void UpdateCompletionState(
        GameSessionGrainState state,
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
            Card = new GameCardState { CardInstanceId = card.Card.CardInstanceId, Definition = card.Card.Definition },
            ChosenResourceColor = card.ChosenResourceColor,
            CraftedCardDefinition = card.CraftedCardDefinition,
            TargetResourceColor = card.TargetResourceColor,
            TargetCardInstanceId = card.TargetCardInstanceId,
            ReturnToHand = card.ReturnToHand,
            ConsumedCards = card.ConsumedCards.Select(reference => new GameCardReferenceState
            {
                CardInstanceId = reference.CardInstanceId,
                ProducedByCardInstanceId = reference.ProducedByCardInstanceId,
                ProducedCardDefinition = reference.ProducedCardDefinition
            }).ToList()
        }).ToList();

    private static string GetParticipantName(GameSessionGrainState state, bool isFirstPlayer)
        => GetParticipantName(isFirstPlayer ? state.FirstPlayer : state.SecondPlayer);

    private static string GetParticipantName(GameSessionGrainState state, string userId)
        => TryGetParticipant(state, userId) is { } participant
            ? GetParticipantName(participant.Player)
            : userId;

    private static string GetParticipantName(GameSessionParticipantView? participant)
        => participant is null
            ? string.Empty
            : participant.UserId;

    private static void Shuffle<T>(IList<T> list)
    {
        for (var index = list.Count - 1; index > 0; index--)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (list[index], list[swapIndex]) = (list[swapIndex], list[index]);
        }
    }
}
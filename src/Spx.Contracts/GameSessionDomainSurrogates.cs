using Orleans;
using Orleans.Serialization.Codecs;
using Spx.Game.Domain;

namespace Spx.Contracts;

// GameSessionParticipant

[GenerateSerializer]
public struct GameSessionParticipantSurrogate
{
    [Id(0)]
    public Guid PlayerId { get; set; }
}

[RegisterConverter]
public sealed class GameSessionParticipantConverter
    : IConverter<GameSessionParticipant, GameSessionParticipantSurrogate>
{
    public GameSessionParticipant ConvertFromSurrogate(
        in GameSessionParticipantSurrogate surrogate
    ) => new(surrogate.PlayerId);

    public GameSessionParticipantSurrogate ConvertToSurrogate(in GameSessionParticipant value) =>
        new() { PlayerId = value.PlayerId };
}

// GameplayEvent

[GenerateSerializer]
public struct GameplayEventSurrogate
{
    [Id(0)]
    public GameplayEventKind Kind { get; set; }

    [Id(1)]
    public Guid ActorPlayerId { get; set; }

    [Id(2)]
    public GameCardDefinition SourceCardDefinition { get; set; }

    [Id(3)]
    public Guid? TargetPlayerId { get; set; }

    [Id(4)]
    public GameCardDefinition? TargetCardDefinition { get; set; }

    [Id(5)]
    public GameCardDefinition? ProducedCardDefinition { get; set; }
}

[RegisterConverter]
public sealed class GameplayEventConverter : IConverter<GameplayEvent, GameplayEventSurrogate>
{
    public GameplayEvent ConvertFromSurrogate(in GameplayEventSurrogate surrogate) =>
        new(
            surrogate.Kind,
            surrogate.ActorPlayerId,
            surrogate.SourceCardDefinition,
            surrogate.TargetPlayerId,
            surrogate.TargetCardDefinition,
            surrogate.ProducedCardDefinition
        );

    public GameplayEventSurrogate ConvertToSurrogate(in GameplayEvent value) =>
        new()
        {
            Kind = value.Kind,
            ActorPlayerId = value.ActorPlayerId,
            SourceCardDefinition = value.SourceCardDefinition,
            TargetPlayerId = value.TargetPlayerId,
            TargetCardDefinition = value.TargetCardDefinition,
            ProducedCardDefinition = value.ProducedCardDefinition,
        };
}

// InitializeGameSessionCommand

[GenerateSerializer]
public struct InitializeGameSessionCommandSurrogate
{
    [Id(0)]
    public GameSessionParticipant FirstPlayer { get; set; }

    [Id(1)]
    public GameSessionParticipant SecondPlayer { get; set; }
}

[RegisterConverter]
public sealed class InitializeGameSessionCommandConverter
    : IConverter<InitializeGameSessionCommand, InitializeGameSessionCommandSurrogate>
{
    public InitializeGameSessionCommand ConvertFromSurrogate(
        in InitializeGameSessionCommandSurrogate surrogate
    ) => new(surrogate.FirstPlayer, surrogate.SecondPlayer);

    public InitializeGameSessionCommandSurrogate ConvertToSurrogate(
        in InitializeGameSessionCommand value
    ) => new() { FirstPlayer = value.FirstPlayer, SecondPlayer = value.SecondPlayer };
}

// SubmitAcquireCommand

[GenerateSerializer]
public struct SubmitAcquireCommandSurrogate
{
    [Id(0)]
    public Guid PlayerId { get; set; }

    [Id(1)]
    public int ExpectedRoundNumber { get; set; }

    [Id(2)]
    public Guid MarketCardInstanceId { get; set; }
}

[RegisterConverter]
public sealed class SubmitAcquireCommandConverter
    : IConverter<SubmitAcquireCommand, SubmitAcquireCommandSurrogate>
{
    public SubmitAcquireCommand ConvertFromSurrogate(in SubmitAcquireCommandSurrogate surrogate) =>
        new(surrogate.PlayerId, surrogate.ExpectedRoundNumber, surrogate.MarketCardInstanceId);

    public SubmitAcquireCommandSurrogate ConvertToSurrogate(in SubmitAcquireCommand value) =>
        new()
        {
            PlayerId = value.PlayerId,
            ExpectedRoundNumber = value.ExpectedRoundNumber,
            MarketCardInstanceId = value.MarketCardInstanceId,
        };
}

// GameCardReferenceCommand

[GenerateSerializer]
public struct GameCardReferenceCommandSurrogate
{
    [Id(0)]
    public Guid? CardInstanceId { get; set; }

    [Id(1)]
    public Guid? ProducedByCardInstanceId { get; set; }

    [Id(2)]
    public GameCardDefinition? ProducedCardDefinition { get; set; }
}

[RegisterConverter]
public sealed class GameCardReferenceCommandConverter
    : IConverter<GameCardReferenceCommand, GameCardReferenceCommandSurrogate>
{
    public GameCardReferenceCommand ConvertFromSurrogate(
        in GameCardReferenceCommandSurrogate surrogate
    ) =>
        new(
            surrogate.CardInstanceId,
            surrogate.ProducedByCardInstanceId,
            surrogate.ProducedCardDefinition
        );

    public GameCardReferenceCommandSurrogate ConvertToSurrogate(
        in GameCardReferenceCommand value
    ) =>
        new()
        {
            CardInstanceId = value.CardInstanceId,
            ProducedByCardInstanceId = value.ProducedByCardInstanceId,
            ProducedCardDefinition = value.ProducedCardDefinition,
        };
}

// GameBatchCardCommand

[GenerateSerializer]
public struct GameBatchCardCommandSurrogate
{
    [Id(0)]
    public Guid CardInstanceId { get; set; }

    [Id(1)]
    public GameResourceColor? ChosenResourceColor { get; set; }

    [Id(2)]
    public GameCardDefinition? CraftedCardDefinition { get; set; }

    [Id(3)]
    public GameResourceColor? TargetResourceColor { get; set; }

    [Id(4)]
    public Guid? TargetCardInstanceId { get; set; }

    [Id(5)]
    public GameCardReferenceCommand[] ConsumedCards { get; set; }
}

[RegisterConverter]
public sealed class GameBatchCardCommandConverter
    : IConverter<GameBatchCardCommand, GameBatchCardCommandSurrogate>
{
    public GameBatchCardCommand ConvertFromSurrogate(in GameBatchCardCommandSurrogate surrogate) =>
        new(
            surrogate.CardInstanceId,
            surrogate.ChosenResourceColor,
            surrogate.CraftedCardDefinition,
            surrogate.TargetResourceColor,
            surrogate.TargetCardInstanceId,
            surrogate.ConsumedCards
        );

    public GameBatchCardCommandSurrogate ConvertToSurrogate(in GameBatchCardCommand value) =>
        new()
        {
            CardInstanceId = value.CardInstanceId,
            ChosenResourceColor = value.ChosenResourceColor,
            CraftedCardDefinition = value.CraftedCardDefinition,
            TargetResourceColor = value.TargetResourceColor,
            TargetCardInstanceId = value.TargetCardInstanceId,
            ConsumedCards = value.ConsumedCards.ToArray(),
        };
}

// SubmitPlayBatchCommand

[GenerateSerializer]
public struct SubmitPlayBatchCommandSurrogate
{
    [Id(0)]
    public Guid PlayerId { get; set; }

    [Id(1)]
    public int ExpectedRoundNumber { get; set; }

    [Id(2)]
    public GameBatchCardCommand[] Cards { get; set; }
}

[RegisterConverter]
public sealed class SubmitPlayBatchCommandConverter
    : IConverter<SubmitPlayBatchCommand, SubmitPlayBatchCommandSurrogate>
{
    public SubmitPlayBatchCommand ConvertFromSurrogate(
        in SubmitPlayBatchCommandSurrogate surrogate
    ) => new(surrogate.PlayerId, surrogate.ExpectedRoundNumber, surrogate.Cards);

    public SubmitPlayBatchCommandSurrogate ConvertToSurrogate(in SubmitPlayBatchCommand value) =>
        new()
        {
            PlayerId = value.PlayerId,
            ExpectedRoundNumber = value.ExpectedRoundNumber,
            Cards = value.Cards.ToArray(),
        };
}

// GetGameSessionQuery

[GenerateSerializer]
public struct GetGameSessionQuerySurrogate
{
    [Id(0)]
    public Guid PlayerId { get; set; }
}

[RegisterConverter]
public sealed class GetGameSessionQueryConverter
    : IConverter<GetGameSessionQuery, GetGameSessionQuerySurrogate>
{
    public GetGameSessionQuery ConvertFromSurrogate(in GetGameSessionQuerySurrogate surrogate) =>
        new(surrogate.PlayerId);

    public GetGameSessionQuerySurrogate ConvertToSurrogate(in GetGameSessionQuery value) =>
        new() { PlayerId = value.PlayerId };
}

// AbandonGameSessionCommand

[GenerateSerializer]
public struct AbandonGameSessionCommandSurrogate
{
    [Id(0)]
    public Guid PlayerId { get; set; }
}

[RegisterConverter]
public sealed class AbandonGameSessionCommandConverter
    : IConverter<AbandonGameSessionCommand, AbandonGameSessionCommandSurrogate>
{
    public AbandonGameSessionCommand ConvertFromSurrogate(
        in AbandonGameSessionCommandSurrogate surrogate
    ) => new(surrogate.PlayerId);

    public AbandonGameSessionCommandSurrogate ConvertToSurrogate(
        in AbandonGameSessionCommand value
    ) => new() { PlayerId = value.PlayerId };
}

// GameCardView

[GenerateSerializer]
public struct GameCardViewSurrogate
{
    [Id(0)]
    public Guid CardInstanceId { get; set; }

    [Id(1)]
    public GameCardDefinition Definition { get; set; }

    [Id(2)]
    public string? DisplayName { get; set; }

    [Id(3)]
    public GameCardCategory Category { get; set; }

    [Id(4)]
    public GameResourceColor? ResourceColor { get; set; }
}

[RegisterConverter]
public sealed class GameCardViewConverter : IConverter<GameCardView, GameCardViewSurrogate>
{
    public GameCardView ConvertFromSurrogate(in GameCardViewSurrogate surrogate) =>
        new(
            surrogate.CardInstanceId,
            surrogate.Definition,
            surrogate.DisplayName!,
            surrogate.Category,
            surrogate.ResourceColor
        );

    public GameCardViewSurrogate ConvertToSurrogate(in GameCardView value) =>
        new()
        {
            CardInstanceId = value.CardInstanceId,
            Definition = value.Definition,
            DisplayName = value.DisplayName,
            Category = value.Category,
            ResourceColor = value.ResourceColor,
        };
}

// GameCardReferenceView

[GenerateSerializer]
public struct GameCardReferenceViewSurrogate
{
    [Id(0)]
    public Guid? CardInstanceId { get; set; }

    [Id(1)]
    public Guid? ProducedByCardInstanceId { get; set; }

    [Id(2)]
    public GameCardDefinition? ProducedCardDefinition { get; set; }
}

[RegisterConverter]
public sealed class GameCardReferenceViewConverter
    : IConverter<GameCardReferenceView, GameCardReferenceViewSurrogate>
{
    public GameCardReferenceView ConvertFromSurrogate(
        in GameCardReferenceViewSurrogate surrogate
    ) =>
        new(
            surrogate.CardInstanceId,
            surrogate.ProducedByCardInstanceId,
            surrogate.ProducedCardDefinition
        );

    public GameCardReferenceViewSurrogate ConvertToSurrogate(in GameCardReferenceView value) =>
        new()
        {
            CardInstanceId = value.CardInstanceId,
            ProducedByCardInstanceId = value.ProducedByCardInstanceId,
            ProducedCardDefinition = value.ProducedCardDefinition,
        };
}

// GameBatchCardView

[GenerateSerializer]
public struct GameBatchCardViewSurrogate
{
    [Id(0)]
    public GameCardView Card { get; set; }

    [Id(1)]
    public GameResourceColor? ChosenResourceColor { get; set; }

    [Id(2)]
    public GameCardDefinition? CraftedCardDefinition { get; set; }

    [Id(3)]
    public GameResourceColor? TargetResourceColor { get; set; }

    [Id(4)]
    public Guid? TargetCardInstanceId { get; set; }

    [Id(5)]
    public GameCardReferenceView[] ConsumedCards { get; set; }
}

[RegisterConverter]
public sealed class GameBatchCardViewConverter
    : IConverter<GameBatchCardView, GameBatchCardViewSurrogate>
{
    public GameBatchCardView ConvertFromSurrogate(in GameBatchCardViewSurrogate surrogate) =>
        new(
            surrogate.Card,
            surrogate.ChosenResourceColor,
            surrogate.CraftedCardDefinition,
            surrogate.TargetResourceColor,
            surrogate.TargetCardInstanceId,
            surrogate.ConsumedCards
        );

    public GameBatchCardViewSurrogate ConvertToSurrogate(in GameBatchCardView value) =>
        new()
        {
            Card = value.Card,
            ChosenResourceColor = value.ChosenResourceColor,
            CraftedCardDefinition = value.CraftedCardDefinition,
            TargetResourceColor = value.TargetResourceColor,
            TargetCardInstanceId = value.TargetCardInstanceId,
            ConsumedCards = value.ConsumedCards.ToArray(),
        };
}

// GamePlayerStateView

[GenerateSerializer]
public struct GamePlayerStateViewSurrogate
{
    [Id(0)]
    public GameSessionParticipant Participant { get; set; }

    [Id(1)]
    public GameCardView[] Hand { get; set; }

    [Id(2)]
    public bool HasLockedBatch { get; set; }

    [Id(3)]
    public int LockedBatchCount { get; set; }

    [Id(4)]
    public int InitiativeScore { get; set; }

    [Id(5)]
    public bool HasScoutOverride { get; set; }

    [Id(6)]
    public bool PicksFirstInAcquirePhase { get; set; }

    [Id(7)]
    public GameBatchCardView[] VisibleLockedCards { get; set; }
}

[RegisterConverter]
public sealed class GamePlayerStateViewConverter
    : IConverter<GamePlayerStateView, GamePlayerStateViewSurrogate>
{
    public GamePlayerStateView ConvertFromSurrogate(in GamePlayerStateViewSurrogate surrogate) =>
        new(
            surrogate.Participant,
            surrogate.Hand,
            surrogate.HasLockedBatch,
            surrogate.LockedBatchCount,
            surrogate.InitiativeScore,
            surrogate.HasScoutOverride,
            surrogate.PicksFirstInAcquirePhase,
            surrogate.VisibleLockedCards
        );

    public GamePlayerStateViewSurrogate ConvertToSurrogate(in GamePlayerStateView value) =>
        new()
        {
            Participant = value.Participant,
            Hand = value.Hand.ToArray(),
            HasLockedBatch = value.HasLockedBatch,
            LockedBatchCount = value.LockedBatchCount,
            InitiativeScore = value.InitiativeScore,
            HasScoutOverride = value.HasScoutOverride,
            PicksFirstInAcquirePhase = value.PicksFirstInAcquirePhase,
            VisibleLockedCards = value.VisibleLockedCards.ToArray(),
        };
}

// GameCompletionView

[GenerateSerializer]
public struct GameCompletionViewSurrogate
{
    [Id(0)]
    public GameCompletionReason Reason { get; set; }

    [Id(1)]
    public GameSessionParticipant? Winner { get; set; }

    [Id(2)]
    public DateTime CompletedAtUtc { get; set; }
}

[RegisterConverter]
public sealed class GameCompletionViewConverter
    : IConverter<GameCompletionView, GameCompletionViewSurrogate>
{
    public GameCompletionView ConvertFromSurrogate(in GameCompletionViewSurrogate surrogate) =>
        new(surrogate.Reason, surrogate.Winner, surrogate.CompletedAtUtc);

    public GameCompletionViewSurrogate ConvertToSurrogate(in GameCompletionView value) =>
        new()
        {
            Reason = value.Reason,
            Winner = value.Winner,
            CompletedAtUtc = value.CompletedAtUtc,
        };
}

// GameResolvedPlayerBatchView

[GenerateSerializer]
public struct GameResolvedPlayerBatchViewSurrogate
{
    [Id(0)]
    public GameSessionParticipant Participant { get; set; }

    [Id(1)]
    public GameBatchCardView[] PlayedCards { get; set; }

    [Id(2)]
    public bool ProducedVictory { get; set; }
}

[RegisterConverter]
public sealed class GameResolvedPlayerBatchViewConverter
    : IConverter<GameResolvedPlayerBatchView, GameResolvedPlayerBatchViewSurrogate>
{
    public GameResolvedPlayerBatchView ConvertFromSurrogate(
        in GameResolvedPlayerBatchViewSurrogate surrogate
    ) => new(surrogate.Participant, surrogate.PlayedCards, surrogate.ProducedVictory);

    public GameResolvedPlayerBatchViewSurrogate ConvertToSurrogate(
        in GameResolvedPlayerBatchView value
    ) =>
        new()
        {
            Participant = value.Participant,
            PlayedCards = value.PlayedCards.ToArray(),
            ProducedVictory = value.ProducedVictory,
        };
}

// GameResolvedBatchView

[GenerateSerializer]
public struct GameResolvedBatchViewSurrogate
{
    [Id(0)]
    public int RoundNumber { get; set; }

    [Id(1)]
    public GameResolvedPlayerBatchView[] Players { get; set; }

    [Id(2)]
    public DateTime ResolvedAtUtc { get; set; }
}

[RegisterConverter]
public sealed class GameResolvedBatchViewConverter
    : IConverter<GameResolvedBatchView, GameResolvedBatchViewSurrogate>
{
    public GameResolvedBatchView ConvertFromSurrogate(
        in GameResolvedBatchViewSurrogate surrogate
    ) => new(surrogate.RoundNumber, surrogate.Players, surrogate.ResolvedAtUtc);

    public GameResolvedBatchViewSurrogate ConvertToSurrogate(in GameResolvedBatchView value) =>
        new()
        {
            RoundNumber = value.RoundNumber,
            Players = value.Players.ToArray(),
            ResolvedAtUtc = value.ResolvedAtUtc,
        };
}

// GameSessionView

[GenerateSerializer]
public struct GameSessionViewSurrogate
{
    [Id(0)]
    public Guid GameId { get; set; }

    [Id(1)]
    public int RoundNumber { get; set; }

    [Id(2)]
    public GamePhase Phase { get; set; }

    [Id(3)]
    public GamePlayerStateView CurrentPlayer { get; set; }

    [Id(4)]
    public GamePlayerStateView OpponentPlayer { get; set; }

    [Id(5)]
    public GameCardView[] VisibleMarketCards { get; set; }

    [Id(6)]
    public int MarketDeckCount { get; set; }

    [Id(7)]
    public bool WaitingForOpponent { get; set; }

    [Id(8)]
    public bool CanAcquireCard { get; set; }

    [Id(9)]
    public bool CanLockBatch { get; set; }

    [Id(10)]
    public int MaxBatchSize { get; set; }

    [Id(11)]
    public GameResolvedBatchView? LastResolvedBatch { get; set; }

    [Id(12)]
    public GameCompletionView? Completion { get; set; }
}

[RegisterConverter]
public sealed class GameSessionViewConverter : IConverter<GameSessionView, GameSessionViewSurrogate>
{
    public GameSessionView ConvertFromSurrogate(in GameSessionViewSurrogate surrogate) =>
        new(
            surrogate.GameId,
            surrogate.RoundNumber,
            surrogate.Phase,
            surrogate.CurrentPlayer,
            surrogate.OpponentPlayer,
            surrogate.VisibleMarketCards,
            surrogate.MarketDeckCount,
            surrogate.WaitingForOpponent,
            surrogate.CanAcquireCard,
            surrogate.CanLockBatch,
            surrogate.MaxBatchSize,
            surrogate.LastResolvedBatch,
            surrogate.Completion
        );

    public GameSessionViewSurrogate ConvertToSurrogate(in GameSessionView value) =>
        new()
        {
            GameId = value.GameId,
            RoundNumber = value.RoundNumber,
            Phase = value.Phase,
            CurrentPlayer = value.CurrentPlayer,
            OpponentPlayer = value.OpponentPlayer,
            VisibleMarketCards = value.VisibleMarketCards.ToArray(),
            MarketDeckCount = value.MarketDeckCount,
            WaitingForOpponent = value.WaitingForOpponent,
            CanAcquireCard = value.CanAcquireCard,
            CanLockBatch = value.CanLockBatch,
            MaxBatchSize = value.MaxBatchSize,
            LastResolvedBatch = value.LastResolvedBatch,
            Completion = value.Completion,
        };
}

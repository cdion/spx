using Orleans;

namespace Spx.Contracts;

public enum GamePhase
{
    Acquire = 0,
    Play = 1,
    Resolve = 2,
    Completed = 3
}

public enum GameCardDefinition
{
    Extract = 0,
    Refine = 1,
    Produce = 2,
    Red = 3,
    Yellow = 4,
    Blue = 5,
    Purple = 6,
    Green = 7,
    Orange = 8,
    Sabotage = 9,
    Replicate = 10,
    Catalyst = 11,
    Corrupt = 12,
    Reclaim = 13,
    Scout = 14,
    Victory = 15
}

public enum GameCardCategory
{
    Action = 0,
    Resource = 1,
    Effect = 2,
    Victory = 3
}

public enum GameResourceColor
{
    Red = 0,
    Yellow = 1,
    Blue = 2,
    Purple = 3,
    Green = 4,
    Orange = 5
}

public enum GameCompletionReason
{
    Victory = 0,
    Draw = 1,
    Abandoned = 2
}

public static class GameCardCatalog
{
    public const int MarketSize = 5;

    public const int MaxBatchSize = 3;

    public static string GetDisplayName(GameCardDefinition definition)
        => definition.ToString();

    public static GameCardCategory GetCategory(GameCardDefinition definition)
        => definition switch
        {
            GameCardDefinition.Extract or GameCardDefinition.Refine or GameCardDefinition.Produce => GameCardCategory.Action,
            GameCardDefinition.Red or GameCardDefinition.Yellow or GameCardDefinition.Blue
                or GameCardDefinition.Purple or GameCardDefinition.Green or GameCardDefinition.Orange => GameCardCategory.Resource,
            GameCardDefinition.Victory => GameCardCategory.Victory,
            _ => GameCardCategory.Effect
        };

    public static GameResourceColor? GetResourceColor(GameCardDefinition definition)
        => definition switch
        {
            GameCardDefinition.Red => GameResourceColor.Red,
            GameCardDefinition.Yellow => GameResourceColor.Yellow,
            GameCardDefinition.Blue => GameResourceColor.Blue,
            GameCardDefinition.Purple => GameResourceColor.Purple,
            GameCardDefinition.Green => GameResourceColor.Green,
            GameCardDefinition.Orange => GameResourceColor.Orange,
            _ => null
        };

    public static bool IsMarketCard(GameCardDefinition definition)
        => definition is GameCardDefinition.Extract or GameCardDefinition.Refine or GameCardDefinition.Produce;

    public static bool IsPlayable(GameCardDefinition definition)
        => GetCategory(definition) is GameCardCategory.Action or GameCardCategory.Effect;

    public static bool IsBaseResource(GameCardDefinition definition)
        => definition is GameCardDefinition.Red or GameCardDefinition.Yellow or GameCardDefinition.Blue;

    public static bool IsRefinedResource(GameCardDefinition definition)
        => definition is GameCardDefinition.Purple or GameCardDefinition.Green or GameCardDefinition.Orange;

    public static int GetInitiativeWeight(GameCardDefinition definition)
        => GetCategory(definition) switch
        {
            GameCardCategory.Action => 1,
            GameCardCategory.Resource when IsBaseResource(definition) => 1,
            GameCardCategory.Resource => 2,
            GameCardCategory.Effect => 3,
            _ => 4
        };

    public static int GetResolutionStep(GameCardDefinition definition)
        => definition switch
        {
            GameCardDefinition.Extract => 1,
            GameCardDefinition.Refine => 2,
            GameCardDefinition.Produce => 3,
            GameCardDefinition.Sabotage or GameCardDefinition.Replicate or GameCardDefinition.Catalyst or GameCardDefinition.Corrupt or GameCardDefinition.Reclaim or GameCardDefinition.Scout => 0,
            _ => int.MaxValue
        };

    public static bool TryGetBaseDefinition(GameResourceColor color, out GameCardDefinition definition)
    {
        definition = color switch
        {
            GameResourceColor.Red => GameCardDefinition.Red,
            GameResourceColor.Yellow => GameCardDefinition.Yellow,
            GameResourceColor.Blue => GameCardDefinition.Blue,
            _ => default
        };

        return definition is GameCardDefinition.Red or GameCardDefinition.Yellow or GameCardDefinition.Blue;
    }

    public static GameCardDefinition? TryGetRefineOutput(GameCardDefinition first, GameCardDefinition second)
    {
        var pair = new[] { first, second }.OrderBy(value => value).ToArray();
        return (pair[0], pair[1]) switch
        {
            (GameCardDefinition.Red, GameCardDefinition.Blue) => GameCardDefinition.Purple,
            (GameCardDefinition.Blue, GameCardDefinition.Yellow) => GameCardDefinition.Green,
            (GameCardDefinition.Red, GameCardDefinition.Yellow) => GameCardDefinition.Orange,
            _ => null
        };
    }

    public static bool TryGetProduceRecipe(GameCardDefinition definition, out GameCardDefinition[] recipe)
    {
        recipe = definition switch
        {
            GameCardDefinition.Sabotage => [GameCardDefinition.Red, GameCardDefinition.Yellow],
            GameCardDefinition.Replicate => [GameCardDefinition.Blue, GameCardDefinition.Yellow],
            GameCardDefinition.Catalyst => [GameCardDefinition.Red, GameCardDefinition.Blue],
            GameCardDefinition.Corrupt => [GameCardDefinition.Orange, GameCardDefinition.Blue],
            GameCardDefinition.Reclaim => [GameCardDefinition.Green, GameCardDefinition.Red],
            GameCardDefinition.Scout => [GameCardDefinition.Purple, GameCardDefinition.Yellow],
            GameCardDefinition.Victory => [GameCardDefinition.Red, GameCardDefinition.Yellow, GameCardDefinition.Blue, GameCardDefinition.Purple, GameCardDefinition.Green, GameCardDefinition.Orange],
            _ => []
        };

        return recipe.Length > 0;
    }

    public static bool MatchesRecipe(IReadOnlyCollection<GameCardDefinition> consumedCards, IReadOnlyCollection<GameCardDefinition> recipe)
        => consumedCards.OrderBy(definition => definition).SequenceEqual(recipe.OrderBy(definition => definition));
}

[GenerateSerializer]
public sealed record GameSessionParticipantView(
    [property: Id(0)] Guid PlayerId,
    [property: Id(1)] string UserId);

[GenerateSerializer]
public sealed record InitializeGameSessionCommand(
    [property: Id(0)] GameSessionParticipantView FirstPlayer,
    [property: Id(1)] GameSessionParticipantView SecondPlayer);

[GenerateSerializer]
public sealed record SubmitAcquireCardCommand(
    [property: Id(0)] string UserId,
    [property: Id(1)] int ExpectedRoundNumber,
    [property: Id(2)] Guid MarketCardInstanceId);

[GenerateSerializer]
public sealed record GameCardReferenceCommand(
    [property: Id(0)] Guid? CardInstanceId,
    [property: Id(1)] Guid? ProducedByCardInstanceId,
    [property: Id(2)] GameCardDefinition? ProducedCardDefinition);

[GenerateSerializer]
public sealed record GameBatchCardCommand(
    [property: Id(0)] Guid CardInstanceId,
    [property: Id(1)] GameResourceColor? ChosenResourceColor,
    [property: Id(2)] GameCardDefinition? CraftedCardDefinition,
    [property: Id(3)] GameResourceColor? TargetResourceColor,
    [property: Id(4)] Guid? TargetCardInstanceId,
    [property: Id(5)] IReadOnlyList<GameCardReferenceCommand> ConsumedCards);

[GenerateSerializer]
public sealed record SubmitPlayBatchCommand(
    [property: Id(0)] string UserId,
    [property: Id(1)] int ExpectedRoundNumber,
    [property: Id(2)] IReadOnlyList<GameBatchCardCommand> Cards);

[GenerateSerializer]
public sealed record GetGameSessionViewQuery(
    [property: Id(0)] string UserId);

[GenerateSerializer]
public sealed record AbandonGameSessionCommand(
    [property: Id(0)] string UserId);

[GenerateSerializer]
public sealed record GameCardInstanceView(
    [property: Id(0)] Guid CardInstanceId,
    [property: Id(1)] GameCardDefinition Definition,
    [property: Id(2)] string DisplayName,
    [property: Id(3)] GameCardCategory Category,
    [property: Id(4)] GameResourceColor? ResourceColor);

[GenerateSerializer]
public sealed record GameCardReferenceView(
    [property: Id(0)] Guid? CardInstanceId,
    [property: Id(1)] Guid? ProducedByCardInstanceId,
    [property: Id(2)] GameCardDefinition? ProducedCardDefinition);

[GenerateSerializer]
public sealed record GameBatchCardView(
    [property: Id(0)] GameCardInstanceView Card,
    [property: Id(1)] GameResourceColor? ChosenResourceColor,
    [property: Id(2)] GameCardDefinition? CraftedCardDefinition,
    [property: Id(3)] GameResourceColor? TargetResourceColor,
    [property: Id(4)] Guid? TargetCardInstanceId,
    [property: Id(5)] IReadOnlyList<GameCardReferenceView> ConsumedCards);

[GenerateSerializer]
public sealed record GamePlayerStateView(
    [property: Id(0)] GameSessionParticipantView Participant,
    [property: Id(1)] IReadOnlyList<GameCardInstanceView> Hand,
    [property: Id(2)] bool HasLockedBatch,
    [property: Id(3)] int LockedBatchCount,
    [property: Id(4)] int InitiativeScore,
    [property: Id(5)] bool HasScoutOverride,
    [property: Id(6)] bool PicksFirstInAcquirePhase,
    [property: Id(7)] IReadOnlyList<GameBatchCardView> VisibleLockedCards);

[GenerateSerializer]
public sealed record GameResolvedPlayerBatchView(
    [property: Id(0)] GameSessionParticipantView Participant,
    [property: Id(1)] IReadOnlyList<GameBatchCardView> PlayedCards,
    [property: Id(2)] bool ProducedVictory);

[GenerateSerializer]
public sealed record GameResolvedBatchView(
    [property: Id(0)] int RoundNumber,
    [property: Id(1)] IReadOnlyList<GameResolvedPlayerBatchView> Players,
    [property: Id(3)] DateTime ResolvedAtUtc);

[GenerateSerializer]
public sealed record GameCompletionView(
    [property: Id(0)] GameCompletionReason Reason,
    [property: Id(1)] GameSessionParticipantView? Winner,
    [property: Id(2)] DateTime CompletedAtUtc);

[GenerateSerializer]
public sealed record GameSessionView(
    [property: Id(0)] Guid GameId,
    [property: Id(1)] int RoundNumber,
    [property: Id(2)] GamePhase Phase,
    [property: Id(3)] GamePlayerStateView CurrentPlayer,
    [property: Id(4)] GamePlayerStateView OpponentPlayer,
    [property: Id(5)] IReadOnlyList<GameCardInstanceView> VisibleMarketCards,
    [property: Id(6)] int MarketDeckCount,
    [property: Id(7)] bool WaitingForOpponent,
    [property: Id(8)] bool CanAcquireCard,
    [property: Id(9)] bool CanLockBatch,
    [property: Id(10)] int MaxBatchSize,
    [property: Id(11)] GameResolvedBatchView? LastResolvedBatch,
    [property: Id(12)] GameCompletionView? Completion);

[GenerateSerializer]
public sealed record SubmitPlayBatchResult(
    [property: Id(0)] GameSessionView Session,
    [property: Id(1)] IReadOnlyList<string> GameplayEvents);
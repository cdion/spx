namespace Spx.Game.Domain;

public enum GamePhase
{
    Acquire = 0,
    Play = 1,
    Resolve = 2,
    Completed = 3,
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
    Victory = 15,
}

public enum GameCardCategory
{
    Action = 0,
    Resource = 1,
    Effect = 2,
    Victory = 3,
}

public enum GameResourceColor
{
    Red = 0,
    Yellow = 1,
    Blue = 2,
    Purple = 3,
    Green = 4,
    Orange = 5,
}

public enum GameCompletionReason
{
    Victory = 0,
    Draw = 1,
    Abandoned = 2,
}

public enum GameplayEventKind
{
    Fizzled = 0,
    DiscardedCard = 1,
    CreatedCard = 2,
    ConvertedCard = 3,
    ScheduledReturnToHand = 4,
    ReturnedToHand = 5,
    Resolved = 6,
}

public sealed record GameplayEvent(
    GameplayEventKind Kind,
    Guid ActorPlayerId,
    GameCardDefinition SourceCardDefinition,
    Guid? TargetPlayerId,
    GameCardDefinition? TargetCardDefinition,
    GameCardDefinition? ProducedCardDefinition
);

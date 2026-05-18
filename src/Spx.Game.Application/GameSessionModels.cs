namespace Spx.Game.Application;

public sealed record SubmitAcquireRequest(
    Guid PlayerId,
    int ExpectedRoundNumber,
    Guid MarketCardInstanceId);

public sealed record GameCardReferenceSelection(
    Guid? CardInstanceId,
    Guid? ProducedByCardInstanceId,
    GameCardDefinition? ProducedCardDefinition);

public sealed record GameBatchCardSelection(
    Guid CardInstanceId,
    GameResourceColor? ChosenResourceColor,
    GameCardDefinition? CraftedCardDefinition,
    GameResourceColor? TargetResourceColor,
    Guid? TargetCardInstanceId,
    IReadOnlyList<GameCardReferenceSelection> ConsumedCards);

public sealed record SubmitPlayBatchRequest(
    Guid PlayerId,
    int ExpectedRoundNumber,
    IReadOnlyList<GameBatchCardSelection> Cards);
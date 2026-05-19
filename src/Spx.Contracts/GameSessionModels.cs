using Orleans;
using Spx.Game.Domain;

namespace Spx.Contracts;

[GenerateSerializer]
public sealed record PendingGameplayEventBatchGrainView(
    [property: Id(0)] Guid BatchId,
    [property: Id(1)] Guid GameId,
    [property: Id(2)] GameResolvedBatchView? LastResolvedBatch,
    [property: Id(3)] GameCompletionView? Completion,
    [property: Id(4)] IReadOnlyList<GameplayEvent> GameplayEvents
);

[GenerateSerializer]
public sealed record AcknowledgeGameplayEventBatchesGrainCommand(
    [property: Id(0)] IReadOnlyList<Guid> BatchIds
);

[GenerateSerializer]
public abstract record GameSessionGrainCommandResult;

[GenerateSerializer]
public sealed record GameSessionGrainCommandSucceededResult(
    [property: Id(0)] GameSessionView Session,
    [property: Id(1)] IReadOnlyList<GameplayEvent> GameplayEvents,
    [property: Id(2)] Guid? PendingGameplayEventBatchId = null
) : GameSessionGrainCommandResult
{
    public GameSessionGrainCommandSucceededResult(GameSessionView Session)
        : this(Session, []) { }
}

[GenerateSerializer]
public sealed record GameSessionGrainCommandRejectedResult([property: Id(0)] string ErrorMessage)
    : GameSessionGrainCommandResult;

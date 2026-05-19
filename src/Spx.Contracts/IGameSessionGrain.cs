using Spx.Game.Domain;

namespace Spx.Contracts;

public interface IGameSessionGrain : IGrainWithGuidKey
{
    Task InitializeAsync(InitializeGameSessionCommand command);

    Task<GameSessionGrainCommandResult> SubmitAcquireAsync(SubmitAcquireCommand command);

    Task<GameSessionGrainCommandResult> SubmitPlayBatchAsync(SubmitPlayBatchCommand command);

    Task<GameSessionView?> GetPlayerViewAsync(GetGameSessionQuery query);

    Task<IReadOnlyList<PendingGameplayEventBatchGrainView>> GetPendingGameplayEventBatchesAsync();

    Task AcknowledgeGameplayEventBatchesAsync(AcknowledgeGameplayEventBatchesGrainCommand command);

    Task<GameSessionView> AbandonAsync(AbandonGameSessionCommand command);
}

namespace Spx.Contracts;

public interface IGameSessionGrain : IGrainWithGuidKey
{
    Task InitializeAsync(InitializeGameSessionGrainCommand command);

    Task<GameSessionGrainCommandResult> SubmitAcquireAsync(SubmitAcquireGrainCommand command);

    Task<GameSessionGrainCommandResult> SubmitPlayBatchAsync(SubmitPlayBatchGrainCommand command);

    Task<GameSessionGrainView?> GetPlayerViewAsync(GetGameSessionGrainQuery query);

    Task<IReadOnlyList<PendingGameplayEventBatchGrainView>> GetPendingGameplayEventBatchesAsync();

    Task AcknowledgeGameplayEventBatchesAsync(AcknowledgeGameplayEventBatchesGrainCommand command);

    Task<GameSessionGrainView> AbandonAsync(AbandonGameSessionGrainCommand command);
}
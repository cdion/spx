namespace Spx.Contracts;

public interface IGameSessionGrain : IGrainWithGuidKey
{
    Task InitializeAsync(InitializeGameSessionCommand command);

    Task<GameSessionView> SubmitAcquireAsync(SubmitAcquireCardCommand command);

    Task<SubmitPlayBatchResult> SubmitPlayBatchAsync(SubmitPlayBatchCommand command);

    Task<GameSessionView?> GetPlayerViewAsync(GetGameSessionViewQuery query);

    Task<GameSessionView> AbandonAsync(AbandonGameSessionCommand command);
}
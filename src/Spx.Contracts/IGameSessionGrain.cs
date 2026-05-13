namespace Spx.Contracts;

public interface IGameSessionGrain : IGrainWithGuidKey
{
    Task InitializeAsync(InitializeGameSessionCommand command);

    Task<GameSessionView> SubmitMoveAsync(SubmitGameMoveCommand command);

    Task<GameSessionView?> GetPlayerViewAsync(GetGameSessionViewQuery query);

    Task<GameSessionView> AbandonAsync(AbandonGameSessionCommand command);
}
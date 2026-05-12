namespace Spx.Contracts;

public interface IGameSessionGrain : IGrainWithGuidKey
{
    Task InitializeAsync(InitializeGameSessionCommand command);

    Task<GameSessionPlayerView> SubmitMoveAsync(SubmitGameMoveCommand command);

    Task<GameSessionPlayerView?> GetPlayerViewAsync(GetGameSessionPlayerViewQuery query);

    Task<GameSessionPlayerView> AbandonAsync(AbandonGameSessionPlayerCommand command);
}
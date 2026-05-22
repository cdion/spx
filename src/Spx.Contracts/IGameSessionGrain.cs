using Spx.Game.Domain;

namespace Spx.Contracts;

public interface IGameSessionGrain : IGrainWithGuidKey
{
    Task InitializeAsync(InitializeNexusGameCommand command);

    Task<NexusTurnOrdersResult> SubmitOrdersAsync(NexusTurnOrdersCommand command);

    Task<NexusGameView?> GetViewAsync(Guid playerId);

    Task AbandonAsync(Guid playerId);
}

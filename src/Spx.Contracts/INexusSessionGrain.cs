using Spx.Nexus.Domain;

namespace Spx.Contracts;

public interface INexusSessionGrain : IGrainWithGuidKey
{
    Task InitializeAsync(InitializeNexusGameCommand command);

    Task<NexusTurnOrdersResult> SubmitOrdersAsync(NexusTurnOrdersCommand command);

    Task<NexusGameView?> GetViewAsync(Guid playerId);

    Task AbandonAsync(Guid playerId);
}

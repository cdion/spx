using Spx.Nexus.Domain;

namespace Spx.Game.Application.Nexus.Features.SubmitOrders;

public interface ISubmitOrdersHandler
{
    Task<GameSessionCommandOutcome> HandleAsync(
        Guid gameId,
        NexusTurnOrdersCommand command,
        CancellationToken cancellationToken = default
    );
}

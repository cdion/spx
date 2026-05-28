namespace Spx.Nexus.Application.Features.SubmitOrders;

public interface ISubmitOrdersHandler
{
    Task<GameSessionCommandOutcome> HandleAsync(
        Guid gameId,
        NexusTurnOrdersCommand command,
        CancellationToken cancellationToken = default
    );
}

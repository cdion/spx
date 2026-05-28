namespace Spx.Game.Application.Nexus.Features.SubmitOrders;

public interface ISubmitOrdersHandler
{
    Task<GameSessionCommandOutcome> HandleAsync(
        Guid gameId,
        NexusSubmitTurnCommand command,
        CancellationToken cancellationToken = default
    );
}

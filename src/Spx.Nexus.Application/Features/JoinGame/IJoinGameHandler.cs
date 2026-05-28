namespace Spx.Nexus.Application.Features.JoinGame;

public interface IJoinGameHandler
{
    Task<GameCommandOutcome> HandleAsync(
        string userId,
        JoinGameRequest request,
        CancellationToken cancellationToken = default
    );
}

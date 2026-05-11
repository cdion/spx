namespace Spx.Games.Features.JoinGame;

public interface IJoinGameHandler
{
    Task<GameCommandResult> HandleAsync(string userId, JoinGameRequest request, CancellationToken cancellationToken = default);
}
namespace Spx.Games.Features.CreateGame;

public interface ICreateGameHandler
{
    Task<GameCommandResult> HandleAsync(string userId, CreateGameRequest request, CancellationToken cancellationToken = default);
}
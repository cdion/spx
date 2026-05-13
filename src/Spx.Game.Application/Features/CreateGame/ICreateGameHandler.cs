namespace Spx.Game.Application.Features.CreateGame;

public interface ICreateGameHandler
{
    Task<GameCommandOutcome> HandleAsync(string userId, CreateGameRequest request, CancellationToken cancellationToken = default);
}
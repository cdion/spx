namespace Spx.Games.Features.CreateGame;

internal sealed class CreateGameHandler(
    IGamePersistence gamePersistence,
    IGameLobbyEventsPublisher gameLobbyEventsPublisher,
    IGameMessageEventsPublisher gameMessageEventsPublisher)
    : ICreateGameHandler
{
    private const int MaxCreateAttempts = 10;

    public async Task<GameCommandResult> HandleAsync(string userId, CreateGameRequest request, CancellationToken cancellationToken = default)
    {
        if (!GameInputValidation.TryNormalizeGameName(request.GameName, out var gameName, out var gameNameError))
        {
            return GameCommandResult.Failure(gameNameError);
        }

        if (!GameInputValidation.TryNormalizePlayerName(request.PlayerName, out var playerName, out var playerNameLookup, out var playerNameError))
        {
            return GameCommandResult.Failure(playerNameError);
        }

        for (var attempt = 0; attempt < MaxCreateAttempts; attempt++)
        {
            var gameId = await gamePersistence.TryCreateGameAsync(
                new CreateGamePersistenceRequest(userId, gameName, playerName, playerNameLookup, InviteCodeGenerator.Generate()),
                cancellationToken);

            if (gameId.HasValue)
            {
                await gameLobbyEventsPublisher.PublishLobbyChangedAsync(gameId.Value, cancellationToken);
                await gameMessageEventsPublisher.PublishMessagesChangedAsync(gameId.Value, cancellationToken);
                return GameCommandResult.Success(gameId.Value);
            }
        }

        return GameCommandResult.Failure("A unique invite code could not be reserved. Please try again.");
    }
}
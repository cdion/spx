namespace Spx.Game.Application.Features.CreateGame;

internal sealed class CreateGameHandler(
    IGamePersistence gamePersistence,
    ILobbyInvalidationPublisher gameLobbyInvalidationPublisher,
    IGameMessageInvalidationPublisher gameMessageInvalidationPublisher
) : ICreateGameHandler
{
    private const int MaxCreateAttempts = 10;

    public async Task<GameCommandOutcome> HandleAsync(
        string userId,
        CreateGameRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (
            !GameInputValidation.TryNormalizeGameName(
                request.GameName,
                out var gameName,
                out var gameNameError
            )
        )
        {
            return new GameCommandFailed(gameNameError);
        }

        if (
            !GameInputValidation.TryNormalizePlayerName(
                request.PlayerName,
                out var playerName,
                out var playerNameLookup,
                out var playerNameError
            )
        )
        {
            return new GameCommandFailed(playerNameError);
        }

        for (var attempt = 0; attempt < MaxCreateAttempts; attempt++)
        {
            var gameId = await gamePersistence.TryCreateGameAsync(
                new CreateGamePersistenceRequest(
                    userId,
                    gameName,
                    playerName,
                    playerNameLookup,
                    InviteCodeGenerator.Generate()
                ),
                cancellationToken
            );

            if (gameId.HasValue)
            {
                await gameLobbyInvalidationPublisher.PublishLobbyInvalidatedAsync(
                    gameId.Value,
                    cancellationToken
                );
                await gameMessageInvalidationPublisher.PublishMessagesInvalidatedAsync(
                    gameId.Value,
                    cancellationToken
                );
                return new GameCommandSucceeded(gameId.Value);
            }
        }

        return new GameCommandFailed(
            "A unique invite code could not be reserved. Please try again."
        );
    }
}

using Spx.Contracts;
using Spx.Game.Application.Features.EnsureGameSession;

namespace Spx.Game.Application.Features.JoinGame;

internal sealed class JoinGameHandler(
    IGamePersistence gamePersistence,
    IEnsureGameSessionHandler ensureGameSessionHandler,
    IGameLobbyInvalidationPublisher gameLobbyInvalidationPublisher,
    IGameMessageInvalidationPublisher gameMessageInvalidationPublisher)
    : IJoinGameHandler
{
    public async Task<GameCommandOutcome> HandleAsync(string userId, JoinGameRequest request, CancellationToken cancellationToken = default)
    {
        if (!GameInputValidation.TryNormalizePlayerName(request.PlayerName, out var playerName, out var playerNameLookup, out var playerNameError))
        {
            return new GameCommandFailed(playerNameError);
        }

        var inviteCode = InviteCodeGenerator.NormalizeInviteCode(request.InviteCode);
        if (inviteCode.Length != 6)
        {
            return new GameCommandFailed("Invite codes must be six characters long.");
        }

        var joinResult = await gamePersistence.JoinGameAsync(
            new JoinGamePersistenceRequest(userId, inviteCode, playerName, playerNameLookup),
            cancellationToken);

        if (joinResult.GameIdToPublish.HasValue)
        {
            var gameId = joinResult.GameIdToPublish.Value;
            if (joinResult.PublishMessagesChanged)
            {
                await ensureGameSessionHandler.HandleAsync(gameId, cancellationToken);
            }

            await gameLobbyInvalidationPublisher.PublishLobbyInvalidatedAsync(gameId, cancellationToken);
            if (joinResult.PublishMessagesChanged)
            {
                await gameMessageInvalidationPublisher.PublishMessagesInvalidatedAsync(gameId, cancellationToken);
            }
        }

        return joinResult.Result;
    }
}
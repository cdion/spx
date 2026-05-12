using Spx.Contracts;

namespace Spx.Games.Features.JoinGame;

internal sealed class JoinGameHandler(
    IGamePersistence gamePersistence,
    IGameSessionService gameSessionService,
    IGameLobbyEventsPublisher gameLobbyEventsPublisher,
    IGameMessageEventsPublisher gameMessageEventsPublisher)
    : IJoinGameHandler
{
    public async Task<GameCommandResult> HandleAsync(string userId, JoinGameRequest request, CancellationToken cancellationToken = default)
    {
        if (!GameInputValidation.TryNormalizePlayerName(request.PlayerName, out var playerName, out var playerNameLookup, out var playerNameError))
        {
            return GameCommandResult.Failure(playerNameError);
        }

        var inviteCode = InviteCodeGenerator.NormalizeInviteCode(request.InviteCode);
        if (inviteCode.Length != 6)
        {
            return GameCommandResult.Failure("Invite codes must be six characters long.");
        }

        var joinResult = await gamePersistence.JoinGameAsync(
            new JoinGamePersistenceRequest(userId, inviteCode, playerName, playerNameLookup),
            cancellationToken);

        if (joinResult.GameIdToPublish.HasValue)
        {
            var gameId = joinResult.GameIdToPublish.Value;
            var activePlayers = await gamePersistence.GetActiveSessionPlayersAsync(gameId, cancellationToken);
            if (activePlayers is { Count: 2 })
            {
                // Session initialization is secondary to the persisted join. Keep the
                // authoritative SQL result even if Orleans needs to recover later.
                await gameSessionService.TryInitializeAsync(gameId, activePlayers, cancellationToken);
            }

            await gameLobbyEventsPublisher.PublishLobbyChangedAsync(gameId, cancellationToken);
            if (joinResult.PublishMessagesChanged)
            {
                await gameMessageEventsPublisher.PublishMessagesChangedAsync(gameId, cancellationToken);
            }
        }

        return joinResult.Result;
    }
}
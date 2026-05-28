using Spx.Nexus.Application.Features.EnsureNexusSession;

namespace Spx.Nexus.Application.Features.JoinGame;

internal sealed class JoinGameHandler(
    IGamePersistence gamePersistence,
    IEnsureNexusSessionHandler ensureGameSessionHandler,
    ILobbyInvalidationPublisher gameLobbyInvalidationPublisher,
    IGameMessageInvalidationPublisher gameMessageInvalidationPublisher
) : IJoinGameHandler
{
    public async Task<GameCommandOutcome> HandleAsync(
        string userId,
        JoinGameRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (
            !NexusInputValidation.TryNormalizePlayerName(
                request.PlayerName,
                out var playerName,
                out var playerNameLookup,
                out var playerNameError
            )
        )
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
            cancellationToken
        );

        if (joinResult.LobbyGameId is Guid lobbyGameId)
        {
            if (joinResult.MessagesGameId.HasValue)
            {
                await ensureGameSessionHandler.HandleAsync(lobbyGameId, cancellationToken);
            }

            await gameLobbyInvalidationPublisher.PublishLobbyInvalidatedAsync(
                lobbyGameId,
                cancellationToken
            );
            if (joinResult.MessagesGameId.HasValue)
            {
                await gameMessageInvalidationPublisher.PublishMessagesInvalidatedAsync(
                    lobbyGameId,
                    cancellationToken
                );
            }
        }

        return joinResult.Result;
    }
}

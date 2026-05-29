using Microsoft.Extensions.Logging;
using Spx.Game.Application.Nexus.Features.EnsureNexusSession;

namespace Spx.Game.Application.Features.JoinGame;

internal sealed partial class JoinGameHandler(
    IGamePersistence gamePersistence,
    IEnsureNexusSessionHandler ensureGameSessionHandler,
    ILobbyInvalidationPublisher gameLobbyInvalidationPublisher,
    IGameMessageInvalidationPublisher gameMessageInvalidationPublisher,
    ILogger<JoinGameHandler> logger
) : IJoinGameHandler
{
    public async Task<GameCommandOutcome> HandleAsync(
        string userId,
        JoinGameRequest request,
        CancellationToken cancellationToken = default
    )
    {
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
                var ensured = await ensureGameSessionHandler.HandleAsync(
                    lobbyGameId,
                    cancellationToken
                );
                if (!ensured)
                {
                    LogEnsureSessionFailed(logger, lobbyGameId, userId);
                }
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

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to ensure Nexus session during join for game {GameId} user {UserId}."
    )]
    private static partial void LogEnsureSessionFailed(ILogger logger, Guid gameId, string userId);
}

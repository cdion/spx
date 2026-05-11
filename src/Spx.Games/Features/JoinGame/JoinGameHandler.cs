namespace Spx.Games.Features.JoinGame;

internal sealed class JoinGameHandler(
    IGamePersistence gamePersistence,
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
            await gameLobbyEventsPublisher.PublishLobbyChangedAsync(joinResult.GameIdToPublish.Value, cancellationToken);
            if (joinResult.PublishMessagesChanged)
            {
                await gameMessageEventsPublisher.PublishMessagesChangedAsync(joinResult.GameIdToPublish.Value, cancellationToken);
            }
        }

        return joinResult.Result;
    }
}
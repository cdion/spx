using Spx.Nexus.Application;

namespace Spx.Data;

internal static class GameMessageFactory
{
    public static GameMessage CreatePublicPlayerMessage(
        Guid gameId,
        GamePlayer sender,
        string body,
        DateTime createdAtUtc
    ) =>
        CreatePlayerMessage(gameId, sender, null, GameMessageKind.PlayerPublic, body, createdAtUtc);

    public static GameMessage CreatePrivatePlayerMessage(
        Guid gameId,
        GamePlayer sender,
        GamePlayer recipient,
        string body,
        DateTime createdAtUtc
    ) =>
        CreatePlayerMessage(
            gameId,
            sender,
            recipient,
            GameMessageKind.PlayerPrivate,
            body,
            createdAtUtc
        );

    public static GameMessage CreateGameplayEvent(
        Guid gameId,
        string body,
        DateTime createdAtUtc
    ) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            GameId = gameId,
            SenderKind = GameMessageSenderKind.Game,
            SenderPlayerId = null,
            RecipientPlayerId = null,
            Kind = GameMessageKind.GameplayEvent,
            Body = body,
            SenderDisplayName = "Game",
            RecipientDisplayName = string.Empty,
            CreatedAtUtc = createdAtUtc,
        };

    public static GameMessage CreateSystemEvent(
        Guid gameId,
        GameMessageKind kind,
        DateTime createdAtUtc,
        GamePlayer? actor = null
    ) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            GameId = gameId,
            SenderKind = actor is null ? GameMessageSenderKind.Game : GameMessageSenderKind.Player,
            SenderPlayerId = actor?.Id,
            RecipientPlayerId = null,
            Kind = kind,
            Body = string.Empty,
            SenderDisplayName = actor?.Name ?? "Game",
            RecipientDisplayName = string.Empty,
            CreatedAtUtc = createdAtUtc,
        };

    private static GameMessage CreatePlayerMessage(
        Guid gameId,
        GamePlayer sender,
        GamePlayer? recipient,
        GameMessageKind kind,
        string body,
        DateTime createdAtUtc
    ) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            GameId = gameId,
            SenderKind = GameMessageSenderKind.Player,
            SenderPlayerId = sender.Id,
            RecipientPlayerId = recipient?.Id,
            Kind = kind,
            Body = body,
            SenderDisplayName = sender.Name,
            RecipientDisplayName = recipient?.Name ?? string.Empty,
            CreatedAtUtc = createdAtUtc,
        };
}

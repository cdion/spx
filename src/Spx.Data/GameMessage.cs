namespace Spx.Data;

public enum GameMessageSenderKind
{
    Player = 0,
    Game = 1
}

public enum GameMessageKind
{
    PlayerPublic = 0,
    PlayerPrivate = 1,
    GameCreated = 2,
    PlayerJoined = 3,
    PlayerLeft = 4,
    GameEnded = 5
}

public sealed class GameMessage
{
    public Guid Id { get; set; }

    public Guid GameId { get; set; }

    public GameMessageSenderKind SenderKind { get; set; }

    public Guid? SenderPlayerId { get; set; }

    public Guid? RecipientPlayerId { get; set; }

    public GameMessageKind Kind { get; set; }

    public string Body { get; set; } = string.Empty;

    public string SenderDisplayName { get; set; } = string.Empty;

    public string RecipientDisplayName { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? EditedAtUtc { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public Game? Game { get; set; }

    public GamePlayer? SenderPlayer { get; set; }

    public GamePlayer? RecipientPlayer { get; set; }
}
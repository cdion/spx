using Spx.Nexus.Application;

namespace Spx.Data;

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

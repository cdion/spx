using Spx.Game.Application;

namespace Spx.Data;

public sealed class Game
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string InviteCode { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public int MaxPlayers { get; set; }

    public GameStatus Status { get; set; } = GameStatus.Open;

    public DateTime? EndedAtUtc { get; set; }

    public ApplicationUser? CreatedBy { get; set; }

    public List<GamePlayer> Players { get; set; } = [];

    public List<GameMessage> Messages { get; set; } = [];
}

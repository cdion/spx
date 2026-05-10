namespace Spx.Web.Data;

public sealed class GamePlayer
{
    public Guid Id { get; set; }

    public Guid GameId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public DateTime JoinedAtUtc { get; set; }

    public DateTime? LeftAtUtc { get; set; }

    public Game? Game { get; set; }

    public ApplicationUser? User { get; set; }
}
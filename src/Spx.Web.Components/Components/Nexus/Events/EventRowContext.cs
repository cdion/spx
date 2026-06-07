using Spx.Nexus.Domain;

namespace Spx.Web.Components.Nexus.Events;

/// <summary>
/// Shared rendering context passed to every event row component.
/// Groups per-panel parameters that every event type needs.
/// Event rows render badges directly with tag syntax; this just supplies lookup helpers.
/// </summary>
public sealed record EventRowContext(
    NexusPlayerContext CurrentPlayer,
    NexusPlayerContext? Opponent,
    IReadOnlyDictionary<Guid, NexusUnitDesign> DesignLookup
)
{
    public string PlayerName(Guid playerId)
    {
        if (playerId == CurrentPlayer.PlayerId)
            return CurrentPlayer.DisplayName;
        if (Opponent is not null && playerId == Opponent.PlayerId)
            return Opponent.DisplayName;
        return playerId.ToString("N")[..8];
    }

    /// <summary>
    /// Faction-aware CSS class for a player name in event text.
    /// The viewing player gets the current faction color;
    /// the opponent gets the opponent color; others default to current.
    /// </summary>
    public NexusPlayerContext? PlayerContext(Guid playerId)
    {
        if (playerId == CurrentPlayer.PlayerId)
            return CurrentPlayer;
        if (Opponent is not null && playerId == Opponent.PlayerId)
            return Opponent;
        return null;
    }

    public string PlayerNameClass(Guid playerId)
    {
        if (playerId == CurrentPlayer.PlayerId)
            return NexusFactionCss.PlayerNameClass(CurrentPlayer.Faction);

        if (Opponent is not null && playerId == Opponent.PlayerId)
            return NexusFactionCss.PlayerNameClass(Opponent.Faction);

        return NexusFactionCss.PlayerNameClass(CurrentPlayer.Faction);
    }
}

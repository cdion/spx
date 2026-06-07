using Spx.Nexus.Domain;

namespace Spx.Web.Components.Nexus;

/// <summary>
/// Shared source of truth for home system display names.
/// Produces "{Player Name}'s Home System" when the viewing player's
/// home coord or a HomePlayerId is known; falls back to the sector name.
/// </summary>
public static class NexusHomeSystemNames
{
    /// <summary>Replaces "Your Home System" / "Opponent Home System" placeholders
    /// with actual player names. Used for event feed text post-processing.</summary>
    public static string ReplacePerspectiveLabels(
        string text,
        string currentPlayerName,
        string opponentPlayerName
    ) =>
        text.Replace(
                "Your Home System",
                CurrentPlayerHomeSystem(currentPlayerName),
                StringComparison.Ordinal
            )
            .Replace(
                "Opponent Home System",
                OpponentPlayerHomeSystem(opponentPlayerName),
                StringComparison.Ordinal
            );

    /// <summary>Resolves a coord's display name by comparing against known home coords.</summary>
    public static string GetSystemDisplayName(
        HexCoord coord,
        HexCoord? currentPlayerHomeCoord,
        string currentPlayerName,
        HexCoord? opponentPlayerHomeCoord,
        string opponentPlayerName
    )
    {
        if (currentPlayerHomeCoord.HasValue && coord == currentPlayerHomeCoord.Value)
            return CurrentPlayerHomeSystem(currentPlayerName);

        if (opponentPlayerHomeCoord.HasValue && coord == opponentPlayerHomeCoord.Value)
            return OpponentPlayerHomeSystem(opponentPlayerName);

        return NexusMapTopology.GetSectorDisplayName(coord);
    }

    /// <summary>Resolves a coord's display name from HomePlayerId + player contexts.
    /// Used by NexusSystemBadge and any component rendering a system badge.</summary>
    public static string GetDisplayName(
        HexCoord coord,
        Guid? homePlayerId,
        NexusPlayerContext currentPlayer,
        NexusPlayerContext? opponent
    )
    {
        if (homePlayerId == currentPlayer.PlayerId)
            return CurrentPlayerHomeSystem(currentPlayer.DisplayName);

        if (opponent is not null && homePlayerId == opponent.PlayerId)
            return OpponentPlayerHomeSystem(opponent.DisplayName);

        return NexusMapTopology.GetSectorDisplayName(coord);
    }

    public static string CurrentPlayerHomeSystem(string playerName) =>
        $"{playerName}'s Home System";

    public static string OpponentPlayerHomeSystem(string playerName) =>
        $"{playerName}'s Home System";
}

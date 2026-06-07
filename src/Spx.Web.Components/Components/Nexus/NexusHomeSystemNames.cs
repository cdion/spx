using Spx.Nexus.Domain;

namespace Spx.Web.Components.Nexus;

/// <summary>Shared source of truth for "{Player Name}'s Home System" format.</summary>
public static class NexusHomeSystemNames
{
    /// <summary>The only format method — produces "{playerName}'s Home System".</summary>
    public static string GetDisplayName(string playerName) => $"{playerName}'s Home System";

    /// <summary>Replaces "Your Home System" / "Opponent Home System" placeholders
    /// in event feed text with actual player names.</summary>
    public static string ReplacePerspectiveLabels(
        string text,
        string currentPlayerName,
        string opponentPlayerName
    ) =>
        text.Replace(
                "Your Home System",
                GetDisplayName(currentPlayerName),
                StringComparison.Ordinal
            )
            .Replace(
                "Opponent Home System",
                GetDisplayName(opponentPlayerName),
                StringComparison.Ordinal
            );
}

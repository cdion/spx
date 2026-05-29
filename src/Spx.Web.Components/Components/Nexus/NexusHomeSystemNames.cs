using Spx.Nexus.Domain;

namespace Spx.Web.Components.Nexus;

public static class NexusHomeSystemNames
{
    public static string ReplacePerspectiveLabels(
        string text,
        string currentPlayerName,
        string opponentPlayerName
    ) =>
        text.Replace(
                "Your Home System",
                CurrentPlayerSystem(currentPlayerName),
                StringComparison.Ordinal
            )
            .Replace(
                "Opponent Home System",
                OpponentPlayerSystem(opponentPlayerName),
                StringComparison.Ordinal
            );

    public static string GetSystemDisplayName(
        HexCoord coord,
        HexCoord? currentPlayerHomeCoord,
        string currentPlayerName,
        HexCoord? opponentPlayerHomeCoord,
        string opponentPlayerName
    )
    {
        if (currentPlayerHomeCoord.HasValue && coord == currentPlayerHomeCoord.Value)
            return CurrentPlayerSystem(currentPlayerName);

        if (opponentPlayerHomeCoord.HasValue && coord == opponentPlayerHomeCoord.Value)
            return OpponentPlayerSystem(opponentPlayerName);

        return NexusMapTopology.GetSectorDisplayName(coord);
    }

    public static string CurrentPlayerSystem(string currentPlayerName) =>
        $"{currentPlayerName}'s System";

    public static string OpponentPlayerSystem(string opponentPlayerName) =>
        $"{opponentPlayerName}'s System";
}

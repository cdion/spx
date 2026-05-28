using System.Collections.Immutable;
using Spx.Nexus.Primitives;

namespace Spx.Game.Application.Nexus;

public static class NexusSessionEventFormatter
{
    public static string Format(
        NexusSessionEvent evt,
        IReadOnlyDictionary<Guid, string> playerNames
    ) =>
        evt switch
        {
            NexusUnitsMovedSessionEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)}'s units {(e.IsRetreat ? "retreated from" : "advanced from")} {SectorName(e.From)} to {SectorName(e.To)}: {FormatUnits(e.Units)}",
            NexusPlanetaryControlSessionEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} took control of {SectorName(e.System)}",
            NexusSystemContestedSessionEvent e =>
                $"{SectorName(e.System)} is contested — planetary units on both sides",
            NexusSystemUncontrolledSessionEvent e =>
                $"{SectorName(e.System)} is now uncontrolled — no planetary units present",
            NexusCombatBeganSessionEvent e =>
                $"Combat erupted at {SectorName(e.System)} between {PlayerName(e.Player1Id, playerNames)} and {PlayerName(e.Player2Id, playerNames)}",
            NexusPhaseResultSessionEvent e => $"Combat phase resolved at {SectorName(e.System)}",
            NexusSystemClearedSessionEvent e =>
                $"{PlayerName(e.VictorId, playerNames)} cleared {SectorName(e.System)}",
            NexusIncomeSessionEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} collected +{e.Sources.Length * 2}⚡ from {e.Sources.Length} system(s)",
            NexusUnitDeployedSessionEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} deployed {e.Count}× {e.UnitType} at {SectorName(e.HomeSystem)}",
            NexusGateStartedSessionEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} began Nexus Gate construction at {SectorName(e.System)}",
            NexusGateCompletedSessionEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} completed the Nexus Gate at {SectorName(e.System)}!",
            NexusGateCancelledSessionEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)}'s Nexus Gate construction at {SectorName(e.System)} was cancelled",
            NexusCapitalDisbandedSessionEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)}'s {e.UnitType} at {SectorName(e.System)} was disbanded (over supply limit)",
            NexusVictorySessionEvent e =>
                $"{PlayerName(e.WinnerId, playerNames)} activated the Nexus Gate — victory!",
            NexusDrawSessionEvent e => $"The game ended in a draw: {e.Reason}",
            _ => evt.GetType().Name,
        };

    private static string PlayerName(Guid playerId, IReadOnlyDictionary<Guid, string> names) =>
        names.TryGetValue(playerId, out var name) ? name : playerId.ToString("N")[..8];

    private static string SectorName(HexCoord coord) =>
        NexusMapTopology.IsValidCoord(coord) ? coord.ToString() : coord.ToString();

    private static string FormatUnits(ImmutableDictionary<NexusUnitType, int> units) =>
        string.Join(", ", units.Select(kv => $"{kv.Value}× {kv.Key}"));
}

using System.Collections.Immutable;
using Spx.Nexus.Domain;

namespace Spx.Game.Application.Nexus;

public static class NexusSessionEventFormatter
{
    public static string Format(
        NexusResolveEvent evt,
        IReadOnlyDictionary<Guid, string> playerNames
    ) => Format(evt, playerNames, viewingPlayerId: null);

    public static string Format(
        NexusResolveEvent evt,
        IReadOnlyDictionary<Guid, string> playerNames,
        Guid? viewingPlayerId
    ) =>
        evt switch
        {
            NexusUnitsMovedEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)}'s units {(e.IsRetreat ? "retreated from" : "advanced from")} {SectorName(e.From, e.PlayerId, viewingPlayerId)} to {SectorName(e.To, e.PlayerId, viewingPlayerId)}: {FormatUnits(e.Units)}",
            NexusPlanetaryControlEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} took control of {SectorName(e.System, e.PlayerId, viewingPlayerId)}",
            NexusSystemContestedEvent e =>
                $"{SectorName(e.System, ownerPlayerId: null, viewingPlayerId)} is contested — planetary units on both sides",
            NexusSystemUncontrolledEvent e =>
                $"{SectorName(e.System, ownerPlayerId: null, viewingPlayerId)} is now uncontrolled — no planetary units present",
            NexusCombatBeganEvent e =>
                $"Combat erupted at {SectorName(e.System, ownerPlayerId: null, viewingPlayerId)} between {PlayerName(e.Player1Id, playerNames)} and {PlayerName(e.Player2Id, playerNames)}",
            NexusPhaseResultEvent e =>
                $"{e.Phase} phase resolved at {SectorName(e.System, ownerPlayerId: null, viewingPlayerId)}",
            NexusSystemClearedEvent e =>
                $"{PlayerName(e.VictorId, playerNames)} cleared {SectorName(e.System, ownerPlayerId: null, viewingPlayerId)}",
            NexusIncomeEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} collected +{e.Amount}⚡ from {e.Sources.Length} system(s)",
            NexusUnitDeployedEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} deployed {e.Count}× {e.UnitType} at {SectorName(e.HomeSystem, e.PlayerId, viewingPlayerId)}",
            NexusGateStartedEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} began Nexus Gate construction at {SectorName(e.System, ownerPlayerId: null, viewingPlayerId)}",
            NexusGateCompletedEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} completed the Nexus Gate at {SectorName(e.System, ownerPlayerId: null, viewingPlayerId)}!",
            NexusGateCancelledEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)}'s Nexus Gate construction at {SectorName(e.System, ownerPlayerId: null, viewingPlayerId)} was cancelled",
            NexusCapitalDisbandedEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)}'s {e.UnitType} at {SectorName(e.System, ownerPlayerId: null, viewingPlayerId)} was disbanded (over supply limit)",
            NexusVictoryEvent e =>
                $"{PlayerName(e.WinnerId, playerNames)} activated the Nexus Gate — victory!",
            NexusDrawEvent e => $"The game ended in a draw: {e.Reason}",
            _ => evt.GetType().Name,
        };

    private static string PlayerName(Guid playerId, IReadOnlyDictionary<Guid, string> names) =>
        names.TryGetValue(playerId, out var name) ? name : playerId.ToString("N")[..8];

    private static string SectorName(HexCoord coord, Guid? ownerPlayerId, Guid? viewingPlayerId)
    {
        if (
            coord == NexusMapTopology.Player1HomeCoord
            || coord == NexusMapTopology.Player2HomeCoord
        )
        {
            if (viewingPlayerId.HasValue && ownerPlayerId == viewingPlayerId)
                return "Your Home System";

            if (viewingPlayerId.HasValue && ownerPlayerId.HasValue)
                return "Opponent Home System";
        }

        return NexusMapTopology.GetSectorDisplayName(coord);
    }

    private static string FormatUnits(ImmutableDictionary<NexusUnitType, int> units) =>
        string.Join(", ", units.Select(kv => $"{kv.Value}× {kv.Key}"));
}

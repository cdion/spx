using System.Collections.Immutable;
using Spx.Game.Domain;

namespace Spx.Game.Application;

public static class NexusResolveEventMessageFormatter
{
    public static string Format(
        NexusResolveEvent evt,
        IReadOnlyDictionary<Guid, string> playerNames
    ) =>
        evt switch
        {
            NexusUnitsMovedEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)}'s units {(e.IsRetreat ? "retreated from" : "advanced from")} {SectorName(e.From)} to {SectorName(e.To)}: {FormatUnits(e.Units)}",
            NexusPlanetaryControlEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} took control of {SectorName(e.System)}",
            NexusSystemContestedEvent e =>
                $"{SectorName(e.System)} is contested — planetary units on both sides",
            NexusSystemUncontrolledEvent e =>
                $"{SectorName(e.System)} is now uncontrolled — no planetary units present",
            NexusCombatBeganEvent e =>
                $"Combat erupted at {SectorName(e.System)} between {PlayerName(e.Player1Id, playerNames)} and {PlayerName(e.Player2Id, playerNames)}",
            NexusPhaseResultEvent e => FormatPhaseResult(e, playerNames),
            NexusSystemClearedEvent e =>
                $"{PlayerName(e.VictorId, playerNames)} cleared {SectorName(e.System)}",
            NexusIncomeEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} collected +{e.Amount}⚡ from {e.Sources.Length} system(s)",
            NexusUnitDeployedEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} deployed {e.Count}× {e.UnitType} at {SectorName(e.HomeSystem)}",
            NexusGateStartedEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} began Nexus Gate construction at {SectorName(e.System)}",
            NexusGateCompletedEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} completed the Nexus Gate at {SectorName(e.System)}!",
            NexusGateCancelledEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)}'s Nexus Gate construction at {SectorName(e.System)} was cancelled",
            NexusVictoryEvent e =>
                $"{PlayerName(e.WinnerId, playerNames)} activated the Nexus Gate — victory!",
            NexusDrawEvent e => $"The game ended in a draw: {e.Reason}",
            _ => evt.GetType().Name,
        };

    private static string FormatPhaseResult(
        NexusPhaseResultEvent e,
        IReadOnlyDictionary<Guid, string> playerNames
    )
    {
        var phaseName = e.Phase switch
        {
            NexusCombatSpec.PhaseScreen => "Screen",
            NexusCombatSpec.PhaseEngage => "Engage",
            NexusCombatSpec.PhaseBombard => "Bombard",
            NexusCombatSpec.PhaseAssault => "Assault",
            _ => $"Phase {e.Phase}",
        };

        var attackLines =
            e.AttackRolls.Length > 0
                ? e.AttackRolls.Select(r =>
                    $"  {PlayerName(r.AttackingPlayerId, playerNames)}: {r.AttackerType}→{r.TargetType}: {r.Roll} (need {r.Threshold}+, {(r.IsHit ? "hit" : "miss")})"
                )
                : ["  no attacks"];

        var header = $"{phaseName} phase at {SectorName(e.System)}";

        if (e.Losses.Length == 0)
            return string.Join('\n', attackLines.Prepend(header).Append("  No losses."));

        var lossSummary = string.Join(
            ", ",
            e.Losses.GroupBy(l => l.PlayerId)
                .Select(g =>
                    $"{PlayerName(g.Key, playerNames)} lost "
                    + string.Join(", ", g.Select(l => $"{l.Count}× {l.UnitType}"))
                )
        );

        return string.Join('\n', attackLines.Prepend(header).Append($"  {lossSummary}."));
    }

    private static string SectorName(HexCoord coord) => NexusMap.GetSectorDisplayName(coord);

    private static string FormatUnits(ImmutableDictionary<NexusUnitType, int> units) =>
        units.Count == 0
            ? "no units"
            : string.Join(", ", units.Select(kv => $"{kv.Value}× {kv.Key}"));

    private static string PlayerName(Guid playerId, IReadOnlyDictionary<Guid, string> names) =>
        names.TryGetValue(playerId, out var name) ? name : playerId.ToString("N")[..8];
}

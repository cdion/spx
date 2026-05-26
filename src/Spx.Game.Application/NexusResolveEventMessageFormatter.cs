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
                $"{PlayerName(e.PlayerId, playerNames)}'s units moved from {e.From} to {e.To}: {FormatUnits(e.Units)}",
            NexusGroundForcesControlEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} took control of system {e.System}",
            NexusSystemContestedEvent e =>
                $"System {e.System} is contested — ground forces on both sides",
            NexusSystemUncontrolledEvent e =>
                $"System {e.System} is now uncontrolled — no ground forces present",
            NexusCombatBeganEvent e =>
                $"Combat erupted at {e.System} between {PlayerName(e.Player1Id, playerNames)} and {PlayerName(e.Player2Id, playerNames)}",
            NexusPhaseResultEvent e => FormatPhaseResult(e, playerNames),
            NexusSystemClearedEvent e =>
                $"{PlayerName(e.VictorId, playerNames)} cleared system {e.System}",
            NexusIncomeEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} collected +{e.Amount}⚡ from {e.Sources.Length} system(s)",
            NexusUnitDeployedEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} deployed {e.Count}× {e.UnitType} at {e.HomeSystem}",
            NexusGateStartedEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} began Nexus Gate construction at {e.System}",
            NexusGateCompletedEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} completed the Nexus Gate at {e.System}!",
            NexusGateCancelledEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)}'s Nexus Gate construction at {e.System} was cancelled",
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
            NexusCombatSpec.PhaseSquadron => "Squadron",
            NexusCombatSpec.PhaseNaval => "Naval",
            NexusCombatSpec.PhaseBombardment => "Bombardment",
            NexusCombatSpec.PhaseGround => "Ground",
            _ => $"Phase {e.Phase}",
        };

        var rollLines = e
            .AttackRolls.GroupBy(r => r.AttackingPlayerId)
            .Select(g =>
            {
                var rolls = string.Join(
                    ", ",
                    g.Select(r =>
                        $"{r.AttackerType}→{r.TargetType}: {r.Roll} (need {r.Threshold}+, {(r.IsHit ? "hit" : "miss")})"
                    )
                );
                return $"{PlayerName(g.Key, playerNames)}: {rolls}";
            });

        var attackSummary = e.AttackRolls.Length > 0 ? string.Join(" | ", rollLines) : "no attacks";

        if (e.Losses.Length == 0)
            return $"{phaseName} phase at {e.System} — {attackSummary}. No losses.";

        var lossSummary = string.Join(
            ", ",
            e.Losses.GroupBy(l => l.PlayerId)
                .Select(g =>
                    $"{PlayerName(g.Key, playerNames)} lost "
                    + string.Join(", ", g.Select(l => $"{l.Count}× {l.UnitType}"))
                )
        );

        return $"{phaseName} phase at {e.System} — {attackSummary}. {lossSummary}.";
    }

    private static string FormatUnits(ImmutableDictionary<NexusUnitType, int> units) =>
        units.Count == 0
            ? "no units"
            : string.Join(", ", units.Select(kv => $"{kv.Value}× {kv.Key}"));

    private static string PlayerName(Guid playerId, IReadOnlyDictionary<Guid, string> names) =>
        names.TryGetValue(playerId, out var name) ? name : playerId.ToString("N")[..8];
}

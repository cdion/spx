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
                $"{PlayerName(e.PlayerId, playerNames)}'s units {(e.IsRetreat ? "retreated from" : "advanced from")} {SectorName(e.From, e.PlayerId, viewingPlayerId)} to {SectorName(e.To, e.PlayerId, viewingPlayerId)}: {FormatUnits(e.Stacks)}",
            NexusPlanetaryControlEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} took control of {SectorName(e.System, e.PlayerId, viewingPlayerId)}",
            NexusSystemContestedEvent e =>
                $"{SectorName(e.System, ownerPlayerId: null, viewingPlayerId)} is contested",
            NexusSystemUncontrolledEvent e =>
                $"{SectorName(e.System, ownerPlayerId: null, viewingPlayerId)} is now uncontrolled — no planetary units present",
            NexusCombatBeganEvent e =>
                $"Combat erupted at {SectorName(e.System, ownerPlayerId: null, viewingPlayerId)} between {PlayerName(e.Player1Id, playerNames)} and {PlayerName(e.Player2Id, playerNames)}",
            NexusFirstStrikeEvent e => FormatCombatResult(
                e.System,
                e.Losses,
                e.AttackRolls,
                playerNames,
                viewingPlayerId,
                isFirstStrike: true
            ),
            NexusCombatResultEvent e => FormatCombatResult(
                e.System,
                e.Losses,
                e.AttackRolls,
                playerNames,
                viewingPlayerId,
                isFirstStrike: false
            ),
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

    private static string FormatUnits(ImmutableArray<NexusUnitStackGroup> stacks) =>
        string.Join(
            ", ",
            stacks.Select(stack =>
                $"{stack.Count}× {stack.UnitType} ({stack.RemainingHits}/{stack.UnitType.Profile().Hits} hits)"
            )
        );

    private static string FormatCombatResult(
        HexCoord system,
        ImmutableArray<NexusCombatLoss> losses,
        ImmutableArray<NexusCombatAttackRoll> attackRolls,
        IReadOnlyDictionary<Guid, string> playerNames,
        Guid? viewingPlayerId,
        bool isFirstStrike
    )
    {
        var systemName = SectorName(system, ownerPlayerId: null, viewingPlayerId);
        var lossSummaries = losses
            .GroupBy(loss => loss.PlayerId)
            .Select(group =>
            {
                var playerName = PlayerName(group.Key, playerNames);
                var lossList = string.Join(
                    ", ",
                    group.Select(loss => $"{loss.Count}× {loss.UnitType}").OrderBy(text => text)
                );
                return $"{playerName} loses {lossList}";
            })
            .OrderBy(summary => summary)
            .ToList();

        var phase = isFirstStrike ? NexusCombatPhase.FirstStrike : NexusCombatPhase.Battle;
        var stepName = phase.DisplayName();
        var lines = new List<string> { $"{stepName} at {systemName}" };
        if (attackRolls.Length == 0)
        {
            lines.Add("No attacks resolved");
        }
        else
        {
            lines.AddRange(attackRolls.Select(roll => FormatAttackRoll(roll, playerNames)));
        }

        lines.Add(
            lossSummaries.Count > 0 ? $"Losses: {string.Join("; ", lossSummaries)}" : "Losses: none"
        );

        return string.Join("\n", lines);
    }

    private static string FormatAttackRoll(
        NexusCombatAttackRoll roll,
        IReadOnlyDictionary<Guid, string> playerNames
    )
    {
        var attackerName = PlayerName(roll.AttackingPlayerId, playerNames);
        var targetName = roll.TargetPlayerId is Guid targetPlayerId
            ? PlayerName(targetPlayerId, playerNames)
            : "Unknown";

        var hitResult = roll.WasShielded ? "absorbed" : (roll.IsHit ? "hit" : "miss");
        return $"{attackerName} {FormatUnitWithHits(roll.AttackerType, roll.AttackerRemainingHits)} -> {targetName} {FormatUnitWithHits(roll.TargetType, roll.TargetRemainingHits)}: rolled {roll.Roll} vs {roll.Threshold} {hitResult}";
    }

    private static string FormatUnitWithHits(NexusUnitType unitType, int remainingHits) =>
        $"{unitType} ({remainingHits}/{unitType.Profile().Hits} hits)";
}

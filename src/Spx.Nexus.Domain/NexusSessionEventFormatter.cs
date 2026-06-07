using System.Collections.Immutable;

namespace Spx.Nexus.Domain;

/// <summary>
/// Formats <see cref="NexusResolveEvent"/> subtypes into human-readable strings.
///
/// <para>
/// When <paramref name="viewingPlayerId"/> is provided, sector names for home systems
/// are emitted as the magic strings <c>"Your Home System"</c> and <c>"Opponent Home System"</c>.
/// The caller (typically <c>NexusResolveEventsPanel</c>) is expected to post-process these with
/// <c>NexusHomeSystemNames.ReplacePerspectiveLabels()</c> to inject actual player names.
/// When <paramref name="viewingPlayerId"/> is <c>null</c>, raw sector names are used.
/// </para>
/// </summary>
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
            NexusCombatResultEvent e => FormatCombatResult(e, playerNames, viewingPlayerId),
            NexusSystemClearedEvent e =>
                $"{PlayerName(e.VictorId, playerNames)} cleared {SectorName(e.System, ownerPlayerId: null, viewingPlayerId)}",
            NexusIncomeEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} collected +{e.Amount}⚡ from {e.Sources.Length} system(s)",
            NexusUnitDeployedEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} deployed {e.Count}× {e.DesignName} at {SectorName(e.HomeSystem, e.PlayerId, viewingPlayerId)}",
            NexusGateStartedEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} began Nexus Gate construction at {SectorName(e.System, ownerPlayerId: null, viewingPlayerId)}",
            NexusGateCompletedEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)} completed the Nexus Gate at {SectorName(e.System, ownerPlayerId: null, viewingPlayerId)}!",
            NexusGateCancelledEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)}'s Nexus Gate construction at {SectorName(e.System, ownerPlayerId: null, viewingPlayerId)} was cancelled",
            NexusCapitalDisbandedEvent e =>
                $"{PlayerName(e.PlayerId, playerNames)}'s {e.DesignName} at {SectorName(e.System, ownerPlayerId: null, viewingPlayerId)} was disbanded (over supply limit)",
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
        string.Join(", ", stacks.Select(stack => $"{stack.Count}× {stack.DesignName}"));

    private static string FormatCombatResult(
        NexusCombatResultEvent e,
        IReadOnlyDictionary<Guid, string> playerNames,
        Guid? viewingPlayerId
    )
    {
        var systemName = SectorName(e.System, ownerPlayerId: null, viewingPlayerId);
        var lines = new List<string>
        {
            $"Combat at {systemName} — {PlayerName(e.Player1Id, playerNames)} vs {PlayerName(e.Player2Id, playerNames)}",
        };

        foreach (var phase in e.Phases)
        {
            var phaseName = phase.Phase.DisplayName();
            lines.Add($"  {phaseName}:");

            if (phase.AttackRolls.Length > 0)
                lines.AddRange(
                    phase.AttackRolls.Select(r => $"    {FormatAttackRoll(r, playerNames)}")
                );

            var lossSummaries = phase
                .Losses.GroupBy(loss => loss.PlayerId)
                .Select(group =>
                {
                    var playerName = PlayerName(group.Key, playerNames);
                    var lossList = string.Join(
                        ", ",
                        group
                            .Select(loss => $"{loss.Count}× {loss.DesignName}")
                            .OrderBy(text => text)
                    );
                    return $"{playerName} loses {lossList}";
                })
                .OrderBy(s => s)
                .ToList();

            lines.Add(
                lossSummaries.Count > 0
                    ? $"    Losses: {string.Join("; ", lossSummaries)}"
                    : $"    Losses: none"
            );
        }

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
        return $"{attackerName} {roll.AttackerDesignName} ({roll.AttackerRemainingHits} hits) -> {targetName} {roll.TargetDesignName} ({roll.TargetRemainingHits} hits): rolled {roll.Roll} vs {roll.Threshold} {hitResult}";
    }
}

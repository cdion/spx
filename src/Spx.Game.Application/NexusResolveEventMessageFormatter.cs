using Spx.Game.Domain;

namespace Spx.Game.Application;

public static class NexusResolveEventMessageFormatter
{
    public static string Format(
        NexusResolveEvent evt,
        IReadOnlyDictionary<NexusFactionColor, string> playerNames
    ) =>
        evt switch
        {
            NexusMoveEvent e =>
                $"{FactionName(e.Faction, playerNames)}'s fleet moved from {e.From} to {e.To}",
            NexusSpeedBonusMoveEvent e =>
                $"{FactionName(e.Faction, playerNames)}'s fleet advanced 2 hexes along the trade corridor from {e.From} to {e.To}",
            NexusUndefendedEntryEvent e =>
                $"{FactionName(e.Faction, playerNames)}'s fleet entered {e.Hex} — colony reverts to unclaimed",
            NexusCombatEvent e => FormatCombat(e, playerNames),
            NexusColonizeEvent e =>
                $"{FactionName(e.Faction, playerNames)} colonized {e.Hex} ({e.HexColor}) — income next turn",
            NexusColonizeFailedEvent e =>
                $"{FactionName(e.Faction, playerNames)}'s Colonize at {e.Hex} failed — hex was contested this turn",
            NexusTradeRouteOpenedEvent e =>
                $"A trade route opened between {FactionName(e.Faction1, playerNames)}'s {e.Hex1} and {FactionName(e.Faction2, playerNames)}'s {e.Hex2}",
            NexusTradeRouteClosedEvent e =>
                $"The trade route between {FactionName(e.Faction1, playerNames)}'s {e.Hex1} and {FactionName(e.Faction2, playerNames)}'s {e.Hex2} has closed",
            NexusIncomeEvent e =>
                $"{FactionName(e.Faction, playerNames)} receives +{e.Amounts.GetValueOrDefault(NexusColonyColor.Red, 0)} Red, +{e.Amounts.GetValueOrDefault(NexusColonyColor.Blue, 0)} Blue, +{e.Amounts.GetValueOrDefault(NexusColonyColor.Gold, 0)} Gold this turn",
            NexusFleetDeployedEvent e =>
                $"{FactionName(e.Faction, playerNames)}'s new fleet deployed at {e.HomeHex}",
            NexusGateBegunEvent e =>
                $"{FactionName(e.Faction, playerNames)} began Nexus Gate construction — committed {e.Cost.GetValueOrDefault(NexusColonyColor.Red, 0)}R {e.Cost.GetValueOrDefault(NexusColonyColor.Blue, 0)}B {e.Cost.GetValueOrDefault(NexusColonyColor.Gold, 0)}G",
            NexusGateProgressedEvent e =>
                $"{FactionName(e.Faction, playerNames)} completed Nexus Gate construction at {e.Hex}",
            NexusGateCancelledEvent e =>
                $"{FactionName(e.Faction, playerNames)}'s Nexus Gate construction cancelled — committed resources lost",
            NexusVictoryEvent e =>
                $"{FactionName(e.WinnerFaction, playerNames)} activated the Nexus Gate — victory!",
            NexusDrawEvent e => $"Match ended in a draw: {e.Reason}",
            NexusTiebreakerVictoryEvent e =>
                $"{FactionName(e.WinnerFaction, playerNames)} wins the tiebreaker ({e.WinnerSystems} vs {e.LoserSystems} systems)",
            NexusTiebreakerDrawEvent e =>
                $"Tiebreaker draw — both players control {e.SystemCount} systems",
            _ => evt.GetType().Name,
        };

    private static string FormatCombat(
        NexusCombatEvent e,
        IReadOnlyDictionary<NexusFactionColor, string> names
    )
    {
        if (e.Participants.Count != 2)
            return $"Combat at {e.Hex} (multi-faction — {e.Participants.Count} factions)";

        var p0 = e.Participants[0];
        var p1 = e.Participants[1];
        var name0 = FactionName(p0.Faction, names);
        var name1 = FactionName(p1.Faction, names);
        var header =
            $"{name0}'s {p0.Count} fleet(s) clashed with {name1}'s {p1.Count} fleet(s) at {e.Hex}";

        if (e.WinnerId is null)
            return $"{header} — mutual destruction, all fleets lost";

        if (e.WinnerId == p0.PlayerId)
            return $"{header} — {name0} takes {e.Hex} (losses: {name0} {p0.Losses}, {name1} {p1.Losses})";

        return $"{header} — {name1} holds {e.Hex} (losses: {name0} {p0.Losses}, {name1} {p1.Losses})";
    }

    private static string FactionName(
        NexusFactionColor faction,
        IReadOnlyDictionary<NexusFactionColor, string> names
    ) => names.TryGetValue(faction, out var name) ? name : faction.ToString();
}

using Spx.Game.Domain;

namespace Spx.Game.Application;

public static class NexusResolveEventMessageFormatter
{
    public static string Format(NexusResolveEvent evt, string redName, string blueName) =>
        evt switch
        {
            NexusMoveEvent e =>
                $"{FactionName(e.Faction, redName, blueName)}'s fleet moved from {e.From} to {e.To}",
            NexusSpeedBonusMoveEvent e =>
                $"{FactionName(e.Faction, redName, blueName)}'s fleet advanced 2 hexes along the trade corridor from {e.From} to {e.To}",
            NexusUndefendedEntryEvent e =>
                $"{FactionName(e.Faction, redName, blueName)}'s fleet entered {e.Hex} — colony reverts to unclaimed",
            NexusCombatEvent e => FormatCombat(e, redName, blueName),
            NexusColonizeEvent e =>
                $"{FactionName(e.Faction, redName, blueName)} colonized {e.Hex} ({e.HexColor}) — income next turn",
            NexusColonizeFailedEvent e =>
                $"{FactionName(e.Faction, redName, blueName)}'s Colonize at {e.Hex} failed — hex was contested this turn",
            NexusTradeRouteOpenedEvent e =>
                $"A trade route opened between {FactionName(e.Faction1, redName, blueName)}'s {e.Hex1} and {FactionName(e.Faction2, redName, blueName)}'s {e.Hex2}",
            NexusTradeRouteClosedEvent e =>
                $"The trade route between {FactionName(e.Faction1, redName, blueName)}'s {e.Hex1} and {FactionName(e.Faction2, redName, blueName)}'s {e.Hex2} has closed",
            NexusIncomeEvent e =>
                $"{FactionName(e.Faction, redName, blueName)} receives +{e.RedIncome} Red, +{e.BlueIncome} Blue, +{e.GoldIncome} Gold this turn",
            NexusFleetDeployedEvent e =>
                $"{FactionName(e.Faction, redName, blueName)}'s new fleet deployed at {e.HomeHex}",
            NexusGateBegunEvent e =>
                $"{FactionName(e.Faction, redName, blueName)} began Nexus Gate construction — committed {e.RedCost}R {e.BlueCost}B {e.GoldCost}G",
            NexusGateProgressedEvent e =>
                $"{FactionName(e.Faction, redName, blueName)} completed Nexus Gate construction at {e.Hex}",
            NexusGateCancelledEvent e =>
                $"{FactionName(e.Faction, redName, blueName)}'s Nexus Gate construction cancelled — committed resources lost",
            NexusVictoryEvent e =>
                $"{FactionName(e.WinnerFaction, redName, blueName)} activated the Nexus Gate — victory!",
            NexusDrawEvent e => $"Match ended in a draw: {e.Reason}",
            NexusTiebreakerVictoryEvent e =>
                $"{FactionName(e.WinnerFaction, redName, blueName)} wins the tiebreaker ({e.WinnerSystems} vs {e.LoserSystems} systems)",
            NexusTiebreakerDrawEvent e =>
                $"Tiebreaker draw — both players control {e.SystemCount} systems",
            _ => evt.GetType().Name,
        };

    private static string FormatCombat(NexusCombatEvent e, string redName, string blueName)
    {
        var attacker = FactionName(e.AttackerFaction, redName, blueName);
        var defender = FactionName(e.DefenderFaction, redName, blueName);
        var header =
            $"{attacker}'s {e.AttackerCount} fleet(s) clashed with {defender}'s {e.DefenderCount} fleet(s) at {e.Hex}";

        if (e.WinnerId is null)
            return $"{header} — mutual destruction, all fleets lost";

        if (e.WinnerId == e.AttackerId)
            return $"{header} — {attacker} takes {e.Hex} (losses: {attacker} {e.AttackerLosses}, {defender} {e.DefenderLosses})";

        return $"{header} — {defender} holds {e.Hex} (losses: {attacker} {e.AttackerLosses}, {defender} {e.DefenderLosses})";
    }

    private static string FactionName(NexusFactionColor faction, string redName, string blueName) =>
        faction == NexusFactionColor.Red ? redName : blueName;
}

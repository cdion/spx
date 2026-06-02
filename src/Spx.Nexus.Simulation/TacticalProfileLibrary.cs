using Spx.Nexus.Domain;

namespace Spx.Nexus.Simulation;

public static class TacticalProfileLibrary
{
    public static IReadOnlyList<TacticalProfile> CreateProfiles() =>
        [
            new(
                "interceptors-2",
                "2 Interceptors",
                TacticalProfileFamily.SpaceDuel,
                ["space", "anti-strike"],
                [new TacticalProfileUnit(NexusUnitType.Interceptor, 2)]
            ),
            new(
                "fighters-2",
                "2 Fighters",
                TacticalProfileFamily.SpaceDuel,
                ["space", "anti-strike"],
                [new TacticalProfileUnit(NexusUnitType.Fighter, 2)]
            ),
            new(
                "fighter-bomber",
                "Fighter + Bomber",
                TacticalProfileFamily.SpaceDuel,
                ["space", "bombard", "anti-capital"],
                [
                    new TacticalProfileUnit(NexusUnitType.Fighter, 1),
                    new TacticalProfileUnit(NexusUnitType.Bomber, 1),
                ]
            ),
            new(
                "bombers-2",
                "2 Bombers",
                TacticalProfileFamily.SpaceDuel,
                ["space", "bombard", "anti-capital"],
                [new TacticalProfileUnit(NexusUnitType.Bomber, 2)]
            ),
            new(
                "destroyer",
                "Destroyer",
                TacticalProfileFamily.SpaceDuel,
                ["space", "anti-strike", "anti-capital"],
                [new TacticalProfileUnit(NexusUnitType.Destroyer, 1)]
            ),
            new(
                "frigate-fighter",
                "Frigate + Fighter",
                TacticalProfileFamily.SpaceDuel,
                ["space", "anti-capital"],
                [
                    new TacticalProfileUnit(NexusUnitType.Frigate, 1),
                    new TacticalProfileUnit(NexusUnitType.Fighter, 1),
                ]
            ),
            new(
                "cruiser",
                "Cruiser",
                TacticalProfileFamily.SpaceDuel,
                ["space", "anti-capital"],
                [new TacticalProfileUnit(NexusUnitType.Cruiser, 1)]
            ),
            new(
                "destroyer-cruiser",
                "Destroyer + Cruiser",
                TacticalProfileFamily.SpaceDuel,
                ["space", "anti-strike", "anti-capital"],
                [
                    new TacticalProfileUnit(NexusUnitType.Destroyer, 1),
                    new TacticalProfileUnit(NexusUnitType.Cruiser, 1),
                ]
            ),
            new(
                "destroyer-frigate",
                "Destroyer + Frigate",
                TacticalProfileFamily.SpaceDuel,
                ["space", "anti-strike", "anti-capital"],
                [
                    new TacticalProfileUnit(NexusUnitType.Destroyer, 1),
                    new TacticalProfileUnit(NexusUnitType.Frigate, 1),
                ]
            ),
            new(
                "infantry-2",
                "2 Infantry",
                TacticalProfileFamily.InvasionControl,
                ["ground", "invasion", "control"],
                [new TacticalProfileUnit(NexusUnitType.Infantry, 2)]
            ),
            new(
                "armor",
                "Armor",
                TacticalProfileFamily.InvasionControl,
                ["ground", "invasion", "control"],
                [new TacticalProfileUnit(NexusUnitType.Armor, 1)]
            ),
            new(
                "carrier-landing",
                "Carrier + Fighter + 2 Infantry",
                TacticalProfileFamily.InvasionControl,
                ["invasion", "control", "space"],
                [
                    new TacticalProfileUnit(NexusUnitType.Carrier, 1),
                    new TacticalProfileUnit(NexusUnitType.Fighter, 1),
                    new TacticalProfileUnit(NexusUnitType.Infantry, 2),
                ]
            ),
            new(
                "carrier-bomber-drop",
                "Carrier + Bomber + Armor",
                TacticalProfileFamily.InvasionControl,
                ["invasion", "bombard", "control"],
                [
                    new TacticalProfileUnit(NexusUnitType.Carrier, 1),
                    new TacticalProfileUnit(NexusUnitType.Bomber, 1),
                    new TacticalProfileUnit(NexusUnitType.Armor, 1),
                ]
            ),
            new(
                "cruiser-landing",
                "Cruiser + 2 Infantry",
                TacticalProfileFamily.InvasionControl,
                ["invasion", "bombard", "control", "anti-capital"],
                [
                    new TacticalProfileUnit(NexusUnitType.Cruiser, 1),
                    new TacticalProfileUnit(NexusUnitType.Infantry, 2),
                ]
            ),
            // ── Budget tier 4 (equal-cost space comparisons) ─────────────────
            new(
                "frigate",
                "Frigate",
                TacticalProfileFamily.SpaceBudget,
                ["space", "anti-capital"],
                [new TacticalProfileUnit(NexusUnitType.Frigate, 1)]
            ),
            new(
                "bomber",
                "Bomber",
                TacticalProfileFamily.SpaceBudget,
                ["space", "bombard", "anti-capital"],
                [new TacticalProfileUnit(NexusUnitType.Bomber, 1)]
            ),
            // ── Budget tier 6 ────────────────────────────────────────────────
            new(
                "interceptors-3",
                "3 Interceptors",
                TacticalProfileFamily.SpaceBudget,
                ["space", "anti-strike"],
                [new TacticalProfileUnit(NexusUnitType.Interceptor, 3)]
            ),
            new(
                "fighters-3",
                "3 Fighters",
                TacticalProfileFamily.SpaceBudget,
                ["space", "anti-strike", "anti-capital"],
                [new TacticalProfileUnit(NexusUnitType.Fighter, 3)]
            ),
            new(
                "frigate-interceptor",
                "Frigate + Interceptor",
                TacticalProfileFamily.SpaceBudget,
                ["space", "anti-capital"],
                [
                    new TacticalProfileUnit(NexusUnitType.Frigate, 1),
                    new TacticalProfileUnit(NexusUnitType.Interceptor, 1),
                ]
            ),
            new(
                "bomber-interceptor",
                "Bomber + Interceptor",
                TacticalProfileFamily.SpaceBudget,
                ["space", "bombard", "anti-capital"],
                [
                    new TacticalProfileUnit(NexusUnitType.Bomber, 1),
                    new TacticalProfileUnit(NexusUnitType.Interceptor, 1),
                ]
            ),
            // ── Budget tier 8 ────────────────────────────────────────────────
            new(
                "interceptors-4",
                "4 Interceptors",
                TacticalProfileFamily.SpaceBudget,
                ["space", "anti-strike"],
                [new TacticalProfileUnit(NexusUnitType.Interceptor, 4)]
            ),
            new(
                "fighters-4",
                "4 Fighters",
                TacticalProfileFamily.SpaceBudget,
                ["space", "anti-strike", "anti-capital"],
                [new TacticalProfileUnit(NexusUnitType.Fighter, 4)]
            ),
            new(
                "frigates-2",
                "2 Frigates",
                TacticalProfileFamily.SpaceBudget,
                ["space", "anti-capital"],
                [new TacticalProfileUnit(NexusUnitType.Frigate, 2)]
            ),
            new(
                "cruiser-interceptor",
                "Cruiser + Interceptor",
                TacticalProfileFamily.SpaceBudget,
                ["space", "anti-capital"],
                [
                    new TacticalProfileUnit(NexusUnitType.Cruiser, 1),
                    new TacticalProfileUnit(NexusUnitType.Interceptor, 1),
                ]
            ),
            new(
                "cruiser-fighter",
                "Cruiser + Fighter",
                TacticalProfileFamily.SpaceBudget,
                ["space", "anti-capital"],
                [
                    new TacticalProfileUnit(NexusUnitType.Cruiser, 1),
                    new TacticalProfileUnit(NexusUnitType.Fighter, 1),
                ]
            ),
            // ── Mixed space+ground profiles (exercise Orbit-phase bombing) ───
            // Budget 8
            new(
                "cruiser-infantry",
                "Cruiser + Infantry",
                TacticalProfileFamily.SpaceBudgetMixed,
                ["space", "ground", "bombard", "control"],
                [
                    new TacticalProfileUnit(NexusUnitType.Cruiser, 1),
                    new TacticalProfileUnit(NexusUnitType.Infantry, 1),
                ]
            ),
            new(
                "bomber-2infantry",
                "Bomber + 2 Infantry",
                TacticalProfileFamily.SpaceBudgetMixed,
                ["space", "ground", "bombard", "control"],
                [
                    new TacticalProfileUnit(NexusUnitType.Bomber, 1),
                    new TacticalProfileUnit(NexusUnitType.Infantry, 2),
                ]
            ),
            new(
                "frigate-armor",
                "Frigate + Armor",
                TacticalProfileFamily.SpaceBudgetMixed,
                ["space", "ground", "control"],
                [
                    new TacticalProfileUnit(NexusUnitType.Frigate, 1),
                    new TacticalProfileUnit(NexusUnitType.Armor, 1),
                ]
            ),
            // Budget 10
            new(
                "cruiser-2infantry",
                "Cruiser + 2 Infantry",
                TacticalProfileFamily.SpaceBudgetMixed,
                ["space", "ground", "bombard", "control"],
                [
                    new TacticalProfileUnit(NexusUnitType.Cruiser, 1),
                    new TacticalProfileUnit(NexusUnitType.Infantry, 2),
                ]
            ),
            new(
                "bomber-frigate-infantry",
                "Bomber + Frigate + Infantry",
                TacticalProfileFamily.SpaceBudgetMixed,
                ["space", "ground", "bombard", "control"],
                [
                    new TacticalProfileUnit(NexusUnitType.Bomber, 1),
                    new TacticalProfileUnit(NexusUnitType.Frigate, 1),
                    new TacticalProfileUnit(NexusUnitType.Infantry, 1),
                ]
            ),
            new(
                "carrier-infantry",
                "Carrier + Infantry",
                TacticalProfileFamily.SpaceBudgetMixed,
                ["space", "ground", "control"],
                [
                    new TacticalProfileUnit(NexusUnitType.Carrier, 1),
                    new TacticalProfileUnit(NexusUnitType.Infantry, 1),
                ]
            ),
        ];

    public static IReadOnlyList<TacticalScenario> CreateScenarios(
        IReadOnlyList<TacticalProfile> profiles
    )
    {
        var spaceProfileIds = profiles
            .Where(profile => profile.Family == TacticalProfileFamily.SpaceDuel)
            .Select(profile => profile.Id)
            .ToArray();
        var invasionProfileIds = profiles
            .Where(profile => profile.Family == TacticalProfileFamily.InvasionControl)
            .Select(profile => profile.Id)
            .ToArray();

        // Equal-budget tiers — profiles at exactly the same cost competing directly.
        // Includes SpaceDuel profiles of the matching cost alongside the dedicated SpaceBudget profiles.
        static string[] BudgetTier(
            IReadOnlyList<TacticalProfile> all,
            int cost,
            params string[] additionalIds
        )
        {
            var budgetIds = all.Where(p =>
                    p.Family == TacticalProfileFamily.SpaceBudget && p.TotalCost == cost
                )
                .Select(p => p.Id);
            return additionalIds.Concat(budgetIds).ToArray();
        }

        var budget4Ids = BudgetTier(profiles, 4, "interceptors-2", "fighters-2");
        var budget6Ids = BudgetTier(profiles, 6, "fighter-bomber", "frigate-fighter", "cruiser");
        var budget8Ids = BudgetTier(profiles, 8, "bombers-2");

        // Mixed scenarios: equal-budget compositions that include both space and ground units,
        // so Orbit-phase bombing (Cruiser, Bomber vs Planetary) is exercised.
        static string[] MixedBudgetTier(IReadOnlyList<TacticalProfile> all, int cost) =>
            all.Where(p =>
                    p.Family == TacticalProfileFamily.SpaceBudgetMixed && p.TotalCost == cost
                )
                .Select(p => p.Id)
                .ToArray();

        var mixed8Ids = MixedBudgetTier(profiles, 8);
        var mixed10Ids = MixedBudgetTier(profiles, 10);

        return
        [
            .. CreateRoundScenarios(
                "space-duel-neutral",
                "Space Duel",
                TacticalControlOwner.None,
                spaceProfileIds
            ),
            .. CreateRoundScenarios(
                "invasion-neutral",
                "Invasion / Control",
                TacticalControlOwner.None,
                invasionProfileIds
            ),
            .. CreateRoundScenarios(
                "invasion-defender-held",
                "Invasion / Control",
                TacticalControlOwner.Defender,
                invasionProfileIds
            ),
            .. CreateRoundScenarios(
                "budget-4",
                "Budget 4 · Space",
                TacticalControlOwner.None,
                budget4Ids
            ),
            .. CreateRoundScenarios(
                "budget-6",
                "Budget 6 · Space",
                TacticalControlOwner.None,
                budget6Ids
            ),
            .. CreateRoundScenarios(
                "budget-8",
                "Budget 8 · Space",
                TacticalControlOwner.None,
                budget8Ids
            ),
            .. CreateRoundScenarios(
                "mixed-8-neutral",
                "Budget 8 · Mixed",
                TacticalControlOwner.None,
                mixed8Ids
            ),
            .. CreateRoundScenarios(
                "mixed-8-defender",
                "Budget 8 · Mixed",
                TacticalControlOwner.Defender,
                mixed8Ids
            ),
            .. CreateRoundScenarios(
                "mixed-10-neutral",
                "Budget 10 · Mixed",
                TacticalControlOwner.None,
                mixed10Ids
            ),
            .. CreateRoundScenarios(
                "mixed-10-defender",
                "Budget 10 · Mixed",
                TacticalControlOwner.Defender,
                mixed10Ids
            ),
        ];
    }

    private static IEnumerable<TacticalScenario> CreateRoundScenarios(
        string idPrefix,
        string labelPrefix,
        TacticalControlOwner initialControlOwner,
        IReadOnlyList<string> profileIds
    )
    {
        var controlLabel = initialControlOwner switch
        {
            TacticalControlOwner.Defender => "Defender-Held",
            TacticalControlOwner.Attacker => "Attacker-Held",
            _ => "Neutral",
        };

        for (var maxRounds = 1; maxRounds <= 3; maxRounds++)
        {
            yield return new TacticalScenario(
                $"{idPrefix}-{maxRounds}r",
                $"{labelPrefix} · {controlLabel} · {maxRounds} Round{(maxRounds == 1 ? string.Empty : "s")}",
                initialControlOwner,
                new HexCoord(1, -1),
                maxRounds,
                profileIds
            );
        }
    }
}

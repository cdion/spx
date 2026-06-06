using Spx.Nexus.Domain;

namespace Spx.Nexus.Simulation;

/// <summary>
/// Predefined designs that replicate the combat behavior of the original nine unit types.
/// Used by the tactical simulation library.
/// </summary>
public static class SimulationDesigns
{
    public static readonly NexusUnitDesign Interceptor = D(
        "Interceptor",
        NexusUnitCategory.Strike,
        new Vanguard(NexusUnitCategory.Strike),
        new Dock()
    );

    public static readonly NexusUnitDesign Fighter = D(
        "Fighter",
        NexusUnitCategory.Strike,
        new Battery(NexusUnitCategory.Strike),
        new Battery(NexusUnitCategory.Capital),
        new Scatter(NexusUnitCategory.Capital, 1),
        new Dock()
    );

    public static readonly NexusUnitDesign Bomber = D(
        "Bomber",
        NexusUnitCategory.Strike,
        new Battery(NexusUnitCategory.Strike),
        new Battery(NexusUnitCategory.Capital),
        new Battery(NexusUnitCategory.Planetary),
        new Scatter(NexusUnitCategory.Strike, 1),
        new Disruptor(),
        new Dock()
    );

    public static readonly NexusUnitDesign Frigate = D(
        "Frigate",
        NexusUnitCategory.Capital,
        new Battery(NexusUnitCategory.Strike),
        new Battery(NexusUnitCategory.Capital),
        new Shield(),
        new Screen(NexusUnitCategory.Capital, 1),
        new Hangar(2)
    );

    public static readonly NexusUnitDesign Destroyer = D(
        "Destroyer",
        NexusUnitCategory.Capital,
        new Battery(NexusUnitCategory.Strike),
        new Battery(NexusUnitCategory.Capital),
        new Barrage(NexusUnitCategory.Strike),
        new Hangar(2)
    );

    public static readonly NexusUnitDesign Cruiser = D(
        "Cruiser",
        NexusUnitCategory.Capital,
        new Battery(NexusUnitCategory.Strike),
        new Battery(NexusUnitCategory.Capital),
        new Battery(NexusUnitCategory.Planetary),
        new Seeker(NexusUnitCategory.Capital, 1),
        new Hangar(2)
    );

    public static readonly NexusUnitDesign Carrier = D(
        "Carrier",
        NexusUnitCategory.Capital,
        new Battery(NexusUnitCategory.Strike),
        new Battery(NexusUnitCategory.Capital),
        new Shield(),
        new Hangar(8)
    );

    public static readonly NexusUnitDesign Infantry = D(
        "Infantry",
        NexusUnitCategory.Planetary,
        new Battery(NexusUnitCategory.Planetary),
        new Dock()
    );

    public static readonly NexusUnitDesign Armor = D(
        "Armor",
        NexusUnitCategory.Planetary,
        new Battery(NexusUnitCategory.Planetary),
        new Shield(),
        new Dock()
    );

    private static NexusUnitDesign D(
        string name,
        NexusUnitCategory hull,
        params NexusUnitModule[] tags
    ) =>
        new()
        {
            DesignId = Guid.NewGuid(),
            Name = name,
            Hull = hull,
            Modules = [.. tags],
        };
}

public static class TacticalProfileLibrary
{
    private static TacticalProfileUnit U(
        NexusUnitDesign design,
        int count,
        int? remainingHits = null
    ) => new(design, count, remainingHits);

    public static IReadOnlyList<TacticalProfile> CreateProfiles() =>
        [
            new(
                "interceptors-2",
                "2 Interceptors",
                TacticalProfileFamily.SpaceDuel,
                ["space", "anti-strike"],
                [U(SimulationDesigns.Interceptor, 2)]
            ),
            new(
                "fighters-2",
                "2 Fighters",
                TacticalProfileFamily.SpaceDuel,
                ["space", "anti-strike"],
                [U(SimulationDesigns.Fighter, 2)]
            ),
            new(
                "fighter-bomber",
                "Fighter + Bomber",
                TacticalProfileFamily.SpaceDuel,
                ["space", "bombard", "anti-capital"],
                [U(SimulationDesigns.Fighter, 1), U(SimulationDesigns.Bomber, 1)]
            ),
            new(
                "bombers-2",
                "2 Bombers",
                TacticalProfileFamily.SpaceDuel,
                ["space", "bombard", "anti-capital"],
                [U(SimulationDesigns.Bomber, 2)]
            ),
            new(
                "destroyer",
                "Destroyer",
                TacticalProfileFamily.SpaceDuel,
                ["space", "anti-strike", "anti-capital"],
                [U(SimulationDesigns.Destroyer, 1)]
            ),
            new(
                "frigate-fighter",
                "Frigate + Fighter",
                TacticalProfileFamily.SpaceDuel,
                ["space", "anti-capital"],
                [U(SimulationDesigns.Frigate, 1), U(SimulationDesigns.Fighter, 1)]
            ),
            new(
                "cruiser",
                "Cruiser",
                TacticalProfileFamily.SpaceDuel,
                ["space", "anti-capital"],
                [U(SimulationDesigns.Cruiser, 1)]
            ),
            new(
                "destroyer-cruiser",
                "Destroyer + Cruiser",
                TacticalProfileFamily.SpaceDuel,
                ["space", "anti-strike", "anti-capital"],
                [U(SimulationDesigns.Destroyer, 1), U(SimulationDesigns.Cruiser, 1)]
            ),
            new(
                "destroyer-frigate",
                "Destroyer + Frigate",
                TacticalProfileFamily.SpaceDuel,
                ["space", "anti-strike", "anti-capital"],
                [U(SimulationDesigns.Destroyer, 1), U(SimulationDesigns.Frigate, 1)]
            ),
            new(
                "infantry-2",
                "2 Infantry",
                TacticalProfileFamily.InvasionControl,
                ["ground", "invasion", "control"],
                [U(SimulationDesigns.Infantry, 2)]
            ),
            new(
                "armor",
                "Armor",
                TacticalProfileFamily.InvasionControl,
                ["ground", "invasion", "control"],
                [U(SimulationDesigns.Armor, 1)]
            ),
            new(
                "carrier-landing",
                "Carrier + Fighter + 2 Infantry",
                TacticalProfileFamily.InvasionControl,
                ["invasion", "control", "space"],
                [
                    U(SimulationDesigns.Carrier, 1),
                    U(SimulationDesigns.Fighter, 1),
                    U(SimulationDesigns.Infantry, 2),
                ]
            ),
            new(
                "carrier-bomber-drop",
                "Carrier + Bomber + Armor",
                TacticalProfileFamily.InvasionControl,
                ["invasion", "bombard", "control"],
                [
                    U(SimulationDesigns.Carrier, 1),
                    U(SimulationDesigns.Bomber, 1),
                    U(SimulationDesigns.Armor, 1),
                ]
            ),
            new(
                "cruiser-landing",
                "Cruiser + 2 Infantry",
                TacticalProfileFamily.InvasionControl,
                ["invasion", "bombard", "control", "anti-capital"],
                [U(SimulationDesigns.Cruiser, 1), U(SimulationDesigns.Infantry, 2)]
            ),
            // ── Budget profiles ───────────────────────────────────────────────
            new(
                "frigate",
                "Frigate",
                TacticalProfileFamily.SpaceBudget,
                ["space", "anti-capital"],
                [U(SimulationDesigns.Frigate, 1)]
            ),
            new(
                "bomber",
                "Bomber",
                TacticalProfileFamily.SpaceBudget,
                ["space", "bombard", "anti-capital"],
                [U(SimulationDesigns.Bomber, 1)]
            ),
            new(
                "interceptors-3",
                "3 Interceptors",
                TacticalProfileFamily.SpaceBudget,
                ["space", "anti-strike"],
                [U(SimulationDesigns.Interceptor, 3)]
            ),
            new(
                "fighters-3",
                "3 Fighters",
                TacticalProfileFamily.SpaceBudget,
                ["space", "anti-strike", "anti-capital"],
                [U(SimulationDesigns.Fighter, 3)]
            ),
            new(
                "frigate-interceptor",
                "Frigate + Interceptor",
                TacticalProfileFamily.SpaceBudget,
                ["space", "anti-capital"],
                [U(SimulationDesigns.Frigate, 1), U(SimulationDesigns.Interceptor, 1)]
            ),
            new(
                "bomber-interceptor",
                "Bomber + Interceptor",
                TacticalProfileFamily.SpaceBudget,
                ["space", "bombard", "anti-capital"],
                [U(SimulationDesigns.Bomber, 1), U(SimulationDesigns.Interceptor, 1)]
            ),
            new(
                "interceptors-4",
                "4 Interceptors",
                TacticalProfileFamily.SpaceBudget,
                ["space", "anti-strike"],
                [U(SimulationDesigns.Interceptor, 4)]
            ),
            new(
                "fighters-4",
                "4 Fighters",
                TacticalProfileFamily.SpaceBudget,
                ["space", "anti-strike", "anti-capital"],
                [U(SimulationDesigns.Fighter, 4)]
            ),
            new(
                "frigates-2",
                "2 Frigates",
                TacticalProfileFamily.SpaceBudget,
                ["space", "anti-capital"],
                [U(SimulationDesigns.Frigate, 2)]
            ),
            new(
                "cruiser-interceptor",
                "Cruiser + Interceptor",
                TacticalProfileFamily.SpaceBudget,
                ["space", "anti-capital"],
                [U(SimulationDesigns.Cruiser, 1), U(SimulationDesigns.Interceptor, 1)]
            ),
            new(
                "cruiser-fighter",
                "Cruiser + Fighter",
                TacticalProfileFamily.SpaceBudget,
                ["space", "anti-capital"],
                [U(SimulationDesigns.Cruiser, 1), U(SimulationDesigns.Fighter, 1)]
            ),
            // ── Mixed space+ground profiles ───────────────────────────────────
            new(
                "cruiser-infantry",
                "Cruiser + Infantry",
                TacticalProfileFamily.SpaceBudgetMixed,
                ["space", "ground", "bombard", "control"],
                [U(SimulationDesigns.Cruiser, 1), U(SimulationDesigns.Infantry, 1)]
            ),
            new(
                "bomber-2infantry",
                "Bomber + 2 Infantry",
                TacticalProfileFamily.SpaceBudgetMixed,
                ["space", "ground", "bombard", "control"],
                [U(SimulationDesigns.Bomber, 1), U(SimulationDesigns.Infantry, 2)]
            ),
            new(
                "frigate-armor",
                "Frigate + Armor",
                TacticalProfileFamily.SpaceBudgetMixed,
                ["space", "ground", "control"],
                [U(SimulationDesigns.Frigate, 1), U(SimulationDesigns.Armor, 1)]
            ),
            new(
                "cruiser-2infantry",
                "Cruiser + 2 Infantry",
                TacticalProfileFamily.SpaceBudgetMixed,
                ["space", "ground", "bombard", "control"],
                [U(SimulationDesigns.Cruiser, 1), U(SimulationDesigns.Infantry, 2)]
            ),
            new(
                "bomber-frigate-infantry",
                "Bomber + Frigate + Infantry",
                TacticalProfileFamily.SpaceBudgetMixed,
                ["space", "ground", "bombard", "control"],
                [
                    U(SimulationDesigns.Bomber, 1),
                    U(SimulationDesigns.Frigate, 1),
                    U(SimulationDesigns.Infantry, 1),
                ]
            ),
            new(
                "carrier-infantry",
                "Carrier + Infantry",
                TacticalProfileFamily.SpaceBudgetMixed,
                ["space", "ground", "control"],
                [U(SimulationDesigns.Carrier, 1), U(SimulationDesigns.Infantry, 1)]
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

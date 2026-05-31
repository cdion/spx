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

using Spx.Nexus.Domain;

namespace Spx.Nexus.Simulator;

public enum TacticalControlOwner
{
    None = 0,
    Attacker = 1,
    Defender = 2,
}

public enum TacticalProfileFamily
{
    SpaceDuel = 0,
    InvasionControl = 1,
}

public sealed record TacticalSimulationSettings(int IterationsPerMatchup, int BaseSeed)
{
    public static TacticalSimulationSettings Default { get; } = new(100, 20260529);
}

public sealed record TacticalProfileUnit(NexusUnitType UnitType, int Count, int HitsAbsorbed = 0);

public sealed record TacticalProfile(
    string Id,
    string Label,
    TacticalProfileFamily Family,
    IReadOnlyList<string> Tags,
    IReadOnlyList<TacticalProfileUnit> Units
)
{
    public int TotalCost => Units.Sum(unit => unit.UnitType.Cost() * unit.Count);
}

public sealed record TacticalScenario(
    string Id,
    string Label,
    TacticalControlOwner InitialControlOwner,
    HexCoord System,
    int MaxRounds,
    IReadOnlyList<string> ProfileIds
);

public sealed record TacticalProfileSummary(
    string Id,
    string Label,
    IReadOnlyList<string> Tags,
    int TotalCost,
    IReadOnlyList<TacticalProfileUnit> Units
);

public sealed record TacticalScenarioSummary(
    string Id,
    string Label,
    TacticalControlOwner InitialControlOwner,
    HexCoord System,
    int MaxRounds,
    IReadOnlyList<string> ProfileIds
);

public sealed record TacticalMatchupSummary(
    string ScenarioId,
    string AttackerProfileId,
    string DefenderProfileId,
    int Iterations,
    double FirstContactActivityRate,
    double AttackerWinRate,
    double DefenderWinRate,
    double ContestedRate,
    double MutualDestructionRate,
    double AttackerControlRate,
    double DefenderControlRate,
    double AttackerExpectedSurvivorCost,
    double DefenderExpectedSurvivorCost,
    string DominantKillPhase
);

public sealed record TacticalPhaseSummary(
    string ScenarioId,
    string AttackerProfileId,
    string DefenderProfileId,
    string Phase,
    string Side,
    double AttacksPerTrial,
    double HitsPerTrial,
    double KillsPerTrial
);

public sealed record TacticalSurvivorSummary(
    string ScenarioId,
    string AttackerProfileId,
    string DefenderProfileId,
    string Side,
    NexusUnitType UnitType,
    double ExpectedCount
);

public sealed record TacticalReportData(
    DateTimeOffset GeneratedAtUtc,
    TacticalSimulationSettings Settings,
    IReadOnlyList<TacticalScenarioSummary> Scenarios,
    IReadOnlyList<TacticalProfileSummary> Profiles,
    IReadOnlyList<TacticalMatchupSummary> Matchups,
    IReadOnlyList<TacticalPhaseSummary> PhaseSummaries,
    IReadOnlyList<TacticalSurvivorSummary> SurvivorSummaries
);

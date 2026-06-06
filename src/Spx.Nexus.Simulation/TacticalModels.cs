using Spx.Nexus.Domain;

namespace Spx.Nexus.Simulation;

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

    /// <summary>Equal-budget space profiles used in cost-calibration scenarios.</summary>
    SpaceBudget = 2,

    /// <summary>Mixed space+ground profiles that exercise Orbit-phase capabilities.</summary>
    SpaceBudgetMixed = 3,
}

public sealed record TacticalSimulationSettings(int IterationsPerMatchup, int BaseSeed)
{
    public static TacticalSimulationSettings Default { get; } = new(100, 20260529);
}

public sealed record TacticalProfileUnit(
    NexusUnitDesign Design,
    int Count,
    int? RemainingHits = null
);

public sealed record TacticalProfile(
    string Id,
    string Label,
    TacticalProfileFamily Family,
    IReadOnlyList<string> Modules,
    IReadOnlyList<TacticalProfileUnit> Units
)
{
    public int TotalCost =>
        Units.Sum(unit => NexusHullBaselines.GetProfile(unit.Design).Cost * unit.Count);
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
    IReadOnlyList<string> Modules,
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
    double ContestedRate,
    double MutualDestructionRate,
    double AttackerControlRate,
    double DefenderControlRate,
    double AttackerExpectedSurvivorCost,
    double DefenderExpectedSurvivorCost,
    /// <summary>
    /// Expected enemy cost destroyed divided by own starting cost. &gt;1 means you destroyed more
    /// value than you cost; &lt;1 means you were cost-inefficient. Mirror matchups yield ~1.0.
    /// </summary>
    double AttackerDamageEfficiency,
    double DefenderDamageEfficiency,
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
    Guid DesignId,
    string DesignName,
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

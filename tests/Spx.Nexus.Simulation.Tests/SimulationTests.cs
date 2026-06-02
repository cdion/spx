using Spx.Nexus.Domain;
using Spx.Nexus.Simulation;
using Xunit;

namespace Spx.Nexus.Simulation.Tests;

public sealed class SimulationTests
{
    [Fact]
    public void TacticalSimulator_Run_ProducesMatrixForEveryScenarioAndProfile()
    {
        var settings = new TacticalSimulationSettings(1, 12345);

        var report = TacticalSimulator.Run(settings);

        var expectedMatchups = report.Scenarios.Sum(scenario =>
            scenario.ProfileIds.Count * scenario.ProfileIds.Count
        );

        Assert.NotEmpty(report.Profiles);
        Assert.NotEmpty(report.Scenarios);
        Assert.Equal(expectedMatchups, report.Matchups.Count);
        Assert.Contains(report.Scenarios, scenario => scenario.MaxRounds == 3);
        Assert.All(
            report.Matchups,
            matchup => Assert.InRange(matchup.FirstContactActivityRate, 0.0, 1.0)
        );
    }

    [Fact]
    public void TacticalSimulator_Run_KeepsMirrorMatchupsSymmetric()
    {
        var settings = new TacticalSimulationSettings(12, 12345);
        var profile = TacticalProfileLibrary
            .CreateProfiles()
            .Single(item => item.Id == "infantry-2");
        var scenario = new TacticalScenario(
            "test-invasion-neutral-1r",
            "Test Invasion / Control · Neutral · 1 Round",
            TacticalControlOwner.None,
            new HexCoord(1, -1),
            1,
            [profile.Id]
        );

        var report = TacticalSimulator.Run(settings, new[] { scenario }, new[] { profile });
        var matchup = Assert.Single(report.Matchups);
        var assaultPhases = report
            .PhaseSummaries.Where(summary => summary.Phase == CombatPhase.Surface.ToString())
            .OrderBy(summary => summary.Side)
            .ToArray();

        // Mirror matchup: attacker win rate ≈ 1 − contested − mutual − attacker win rate
        // i.e. both sides are symmetric. Defender win rate is no longer stored; derive it.
        var derivedDefenderWinRate =
            1 - matchup.AttackerWinRate - matchup.ContestedRate - matchup.MutualDestructionRate;
        Assert.Equal(matchup.AttackerWinRate, derivedDefenderWinRate, precision: 5);
        Assert.Equal(matchup.AttackerExpectedSurvivorCost, matchup.DefenderExpectedSurvivorCost);
        Assert.Equal(2, assaultPhases.Length);
        Assert.Equal(assaultPhases[0].HitsPerTrial, assaultPhases[1].HitsPerTrial);
        Assert.Equal(assaultPhases[0].KillsPerTrial, assaultPhases[1].KillsPerTrial);
    }
}

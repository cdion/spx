using System.Text.Json;
using Spx.Nexus.Domain;
using Xunit;

namespace Spx.Nexus.Simulator.Tests;

public sealed class SimulatorAppTests
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
            .PhaseSummaries.Where(summary => summary.Phase == CombatPhase.Assault.ToString())
            .OrderBy(summary => summary.Side)
            .ToArray();

        Assert.Equal(matchup.AttackerWinRate, matchup.DefenderWinRate);
        Assert.Equal(matchup.AttackerExpectedSurvivorCost, matchup.DefenderExpectedSurvivorCost);
        Assert.Equal(2, assaultPhases.Length);
        Assert.Equal(assaultPhases[0].HitsPerTrial, assaultPhases[1].HitsPerTrial);
        Assert.Equal(assaultPhases[0].KillsPerTrial, assaultPhases[1].KillsPerTrial);
    }

    [Fact]
    public async Task SimulatorApp_RunAsync_WritesReportAndSummary()
    {
        var outputDirectory = Path.Combine(
            Path.GetTempPath(),
            "spx-nexus-simulator-tests",
            Guid.NewGuid().ToString("N")
        );

        try
        {
            var result = await SimulatorApp.RunAsync(outputDirectory, CancellationToken.None);

            Assert.True(File.Exists(result.ReportPath));
            Assert.True(File.Exists(result.SummaryPath));

            var summaryJson = await File.ReadAllTextAsync(result.SummaryPath);
            var reportHtml = await File.ReadAllTextAsync(result.ReportPath);
            using var document = JsonDocument.Parse(summaryJson);

            Assert.True(document.RootElement.TryGetProperty("profiles", out var profiles));
            Assert.True(document.RootElement.TryGetProperty("scenarios", out var scenarios));
            Assert.True(document.RootElement.TryGetProperty("matchups", out var matchups));
            Assert.True(
                document.RootElement.TryGetProperty("phaseSummaries", out var phaseSummaries)
            );
            Assert.NotEqual(0, profiles.GetArrayLength());
            Assert.NotEqual(0, scenarios.GetArrayLength());
            Assert.NotEqual(0, matchups.GetArrayLength());
            Assert.NotEqual(0, phaseSummaries.GetArrayLength());
            Assert.Equal(
                JsonValueKind.String,
                matchups[0].GetProperty("dominantKillPhase").ValueKind
            );
            Assert.Equal(JsonValueKind.String, phaseSummaries[0].GetProperty("phase").ValueKind);
            Assert.Contains("First-contact activity", reportHtml);
            Assert.Contains("Attacker damage per trial", reportHtml);
            Assert.Contains("Net damage swing", reportHtml);
            Assert.Contains("phaseLabel", reportHtml);
            Assert.Contains("'Screen'", reportHtml);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, recursive: true);
        }
    }
}

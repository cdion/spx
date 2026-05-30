namespace Spx.Nexus.Simulator;

public sealed class SimulatorApp
{
    public static async Task<SimulatorRunResult> RunAsync(
        string outputDirectory,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        cancellationToken.ThrowIfCancellationRequested();

        var reportData = TacticalSimulator.Run(TacticalSimulationSettings.Default);

        return await TacticalReportWriter.WriteAsync(
            outputDirectory,
            reportData,
            cancellationToken
        );
    }
}

using Spx.Nexus.Simulator;

var outputDirectory =
    args.Length > 0
        ? Path.GetFullPath(args[0])
        : Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "artifacts",
                "nexus-balance"
            )
        );

var result = await SimulatorApp.RunAsync(outputDirectory, CancellationToken.None);

Console.WriteLine($"Generated {result.ReportPath}");
Console.WriteLine($"Summary data: {result.SummaryPath}");

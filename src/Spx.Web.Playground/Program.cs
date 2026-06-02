using Spx.Nexus.Simulation;
using Spx.Web.Playground.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddApplicationServices();
builder.Services.AddPlaygroundNexusServices();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet(
    "/api/nexus/balance",
    (int iterations = 200, int seed = 20260529) =>
    {
        var report = TacticalSimulator.Run(new TacticalSimulationSettings(iterations, seed));
        return Results.Ok(report);
    }
);

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

using Spx.Web.Playground.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddApplicationServices();
builder.Services.AddPlaygroundNexusServices();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

public partial class Program
{
    /// <summary>
    /// Creates the Playground app on a specific port.
    /// Used by <c>dotnet run</c> (indirectly) and by Playwright E2E tests.
    /// </summary>
    public static WebApplication CreateApp(string[]? args = null, int? port = null)
    {
        // When called from a test project, the content root defaults to the
        // test's bin directory. Resolve it to the Playground project root
        // using the assembly location.
        var assemblyDir = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
        var projectDir = assemblyDir;
        while (
            projectDir is not null
            && !File.Exists(Path.Combine(projectDir, "Spx.Web.Playground.csproj"))
        )
            projectDir = Path.GetDirectoryName(projectDir);
        projectDir ??= assemblyDir;

        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { Args = args ?? [], ContentRootPath = projectDir }
        );

        if (port.HasValue)
        {
            builder.WebHost.UseKestrel();
            builder.WebHost.UseUrls($"http://127.0.0.1:{port.Value}");
        }

        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.AddApplicationServices();
        builder.Services.AddPlaygroundNexusServices();

        var app = builder.Build();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

        return app;
    }
}

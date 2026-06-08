using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Spx.Web.Playground;

namespace Spx.Web.E2ETests;

/// <summary>
/// Hosts the Playground app on a real Kestrel port for Playwright browser tests.
/// Creates the host directly via <c>Program.CreateApp</c> instead of using
/// <c>WebApplicationFactory</c>'s TestServer pipeline, which doesn't work
/// with real browser connections.
/// </summary>
public class PlaygroundFixture : IAsyncDisposable
{
    private IHost? _host;

    public string BaseUrl { get; private set; } = null!;

    public async Task StartAsync()
    {
        var port = GetFreePort();
        BaseUrl = $"http://127.0.0.1:{port}";

        var builder = Program.CreateApp(args: null, port: port);
        _host = builder;
        await builder.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

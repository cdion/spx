using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Spx.Web.Circuits;

public sealed class CircuitConnectionEvents
{
    public event Func<Task>? ConnectionDown;

    public event Func<Task>? ConnectionUp;

    internal Task NotifyConnectionDownAsync()
        => NotifyAsync(ConnectionDown);

    internal Task NotifyConnectionUpAsync()
        => NotifyAsync(ConnectionUp);

    private static Task NotifyAsync(Func<Task>? handlers)
    {
        if (handlers is null)
        {
            return Task.CompletedTask;
        }

        return Task.WhenAll(handlers.GetInvocationList().Cast<Func<Task>>().Select(handler => handler()));
    }
}

internal sealed class CircuitConnectionHandler(CircuitConnectionEvents connectionEvents) : CircuitHandler
{
    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
        => connectionEvents.NotifyConnectionDownAsync();

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
        => connectionEvents.NotifyConnectionUpAsync();

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
        => connectionEvents.NotifyConnectionDownAsync();
}
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Spx.Web.Presence;

internal sealed partial class NexusPresenceCircuitHandler(
    NexusPresenceLeaseCoordinator presenceLeaseCoordinator
) : CircuitHandler
{
    public override Task OnConnectionDownAsync(
        Circuit circuit,
        CancellationToken cancellationToken
    ) => presenceLeaseCoordinator.OnConnectionDownAsync(cancellationToken);

    public override Task OnConnectionUpAsync(
        Circuit circuit,
        CancellationToken cancellationToken
    ) => presenceLeaseCoordinator.OnConnectionUpAsync(cancellationToken);

    public override Task OnCircuitClosedAsync(
        Circuit circuit,
        CancellationToken cancellationToken
    ) => presenceLeaseCoordinator.OnCircuitClosedAsync(cancellationToken);
}

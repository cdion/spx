using System.Collections.Immutable;
using Spx.Nexus.Domain;

namespace Spx.Web.Components.Nexus;

public sealed record OrderDraftState(
    ImmutableArray<NexusMoveOrder> PendingMoveOrders,
    ImmutableArray<NexusBuildOrder> PendingBuildOrders,
    bool PendingBeginNexusGate
)
{
    public static OrderDraftState Empty { get; } = new([], [], false);

    public int ComputeProjectedSpend(IReadOnlyList<NexusUnitDesign> designs)
    {
        var lookup = designs.ToDictionary(d => d.DesignId);
        return NexusEngine.ComputeProjectedSpend(PendingBuildOrders, PendingBeginNexusGate, lookup);
    }

    public int ComputeProjectedSpend(Dictionary<Guid, NexusUnitDesign> lookup) =>
        NexusEngine.ComputeProjectedSpend(PendingBuildOrders, PendingBeginNexusGate, lookup);
}

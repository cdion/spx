using Spx.Nexus.Domain;

namespace Spx.Web.Components.Nexus;

public enum PendingOrderRequestKind
{
    RemoveMoveOrder,
    RemoveBuildOrder,
    ClearNexusGate,
}

public sealed record PendingOrderRequest(
    PendingOrderRequestKind Kind,
    NexusMoveOrder? MoveOrder = null,
    NexusBuildOrder? BuildOrder = null
)
{
    public static PendingOrderRequest MoveOrderRemovalRequested(NexusMoveOrder moveOrder) =>
        new(PendingOrderRequestKind.RemoveMoveOrder, MoveOrder: moveOrder);

    public static PendingOrderRequest BuildOrderRemovalRequested(NexusBuildOrder buildOrder) =>
        new(PendingOrderRequestKind.RemoveBuildOrder, BuildOrder: buildOrder);

    public static PendingOrderRequest NexusGateCleared() =>
        new(PendingOrderRequestKind.ClearNexusGate);
}

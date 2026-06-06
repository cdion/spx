using System.Collections.Immutable;
using Spx.Nexus.Domain;

namespace Spx.Web.Components.Nexus;

public enum OrderDraftRequestKind
{
    MoveDraftChanged,
    BuildDraftAdjusted,
    NexusGateDraftChanged,
}

public enum BuildDraftAdjustmentKind
{
    AddOne,
    AddMax,
    RemoveOne,
    RemoveAll,
}

public sealed record OrderDraftRequest(
    OrderDraftRequestKind Kind,
    ImmutableArray<NexusUnitStackGroup> MoveDraftStacks = default,
    Guid? DesignId = null,
    BuildDraftAdjustmentKind? BuildAdjustment = null,
    bool? BeginNexusGate = null
)
{
    public static OrderDraftRequest MoveDraftChanged(ImmutableArray<NexusUnitStackGroup> stacks) =>
        new(OrderDraftRequestKind.MoveDraftChanged, MoveDraftStacks: stacks);

    public static OrderDraftRequest BuildDraftAdjusted(
        Guid designId,
        BuildDraftAdjustmentKind adjustment
    ) =>
        new(
            OrderDraftRequestKind.BuildDraftAdjusted,
            DesignId: designId,
            BuildAdjustment: adjustment
        );

    public static OrderDraftRequest NexusGateDraftChanged(bool beginNexusGate) =>
        new(OrderDraftRequestKind.NexusGateDraftChanged, BeginNexusGate: beginNexusGate);
}

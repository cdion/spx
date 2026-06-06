using System.Collections.Immutable;
using Spx.Nexus.Domain;

namespace Spx.Web.Components.Nexus;

public sealed record SelectionState(
    HexCoord? SelectedSystem,
    ImmutableArray<NexusUnitStackGroup> StagedMoveStacks,
    IReadOnlyList<HexCoord> ValidMoveTargets,
    bool EventFocusOwnsSelection
)
{
    public static SelectionState Empty { get; } =
        new(null, ImmutableArray<NexusUnitStackGroup>.Empty, [], false);
}

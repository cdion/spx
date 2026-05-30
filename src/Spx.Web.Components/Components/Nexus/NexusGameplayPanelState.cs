using System.Collections.Immutable;
using Spx.Nexus.Domain;

namespace Spx.Web.Components.Nexus;

public static class NexusGameplayPanelState
{
    public static bool ShouldResetUiState(
        Guid sessionGameId,
        Guid sessionCurrentPlayerId,
        int sessionRound,
        Guid? lastKnownGameId,
        Guid? lastKnownCurrentPlayerId,
        int? lastKnownRound
    ) =>
        sessionGameId != lastKnownGameId
        || sessionCurrentPlayerId != lastKnownCurrentPlayerId
        || sessionRound != lastKnownRound;

    public static EventFocusState ApplyEventFocusRequest(
        EventFocusState state,
        EventFocusRequest request
    ) =>
        request.Kind switch
        {
            EventFocusRequestKind.Preview => state with
            {
                Preview = GetEventFocus(request.ResolveEvent!),
            },
            EventFocusRequestKind.ClearPreview => state with { Preview = NexusEventFocus.None },
            EventFocusRequestKind.Dismiss => EventFocusState.Empty,
            _ => state,
        };

    public static HexCoord? ApplySelectionRequest(
        HexCoord? selectedSystem,
        SelectionRequest request
    ) =>
        request.Kind switch
        {
            SelectionRequestKind.SelectSystem => selectedSystem == request.System
                ? null
                : request.System,
            SelectionRequestKind.ClearSelection => null,
            _ => selectedSystem,
        };

    public static SelectionState ClearSelection(SelectionState state) =>
        state with
        {
            SelectedSystem = null,
            StagedMoveStacks = ImmutableArray<NexusUnitStackGroup>.Empty,
            ValidMoveTargets = [],
            EventFocusOwnsSelection = false,
        };

    public static SelectionState ApplySelectionRequest(
        SelectionState state,
        SelectionRequest request
    ) =>
        state with
        {
            SelectedSystem = ApplySelectionRequest(state.SelectedSystem, request),
            StagedMoveStacks = ImmutableArray<NexusUnitStackGroup>.Empty,
            ValidMoveTargets = [],
            EventFocusOwnsSelection = false,
        };

    public static SelectionState ApplyMoveDraft(
        SelectionState state,
        ImmutableArray<NexusUnitStackGroup> stacks,
        NexusGameView session,
        Guid playerId
    ) =>
        state.SelectedSystem is null || stacks.IsDefaultOrEmpty
            ? state with
            {
                StagedMoveStacks = stacks,
                ValidMoveTargets = [],
            }
            : state with
            {
                StagedMoveStacks = stacks,
                ValidMoveTargets = NexusViewQueries.GetValidMoveDestinations(
                    session,
                    playerId,
                    state.SelectedSystem.Value
                ),
            };

    public static SelectionState SyncSelectedSystemToEventFocus(
        SelectionState state,
        EventFocusState eventFocusState
    )
    {
        var activeFocus = eventFocusState.Active;
        if (activeFocus.Primary.HasValue)
        {
            return state with
            {
                SelectedSystem = activeFocus.Primary.Value,
                EventFocusOwnsSelection = true,
            };
        }

        return state.EventFocusOwnsSelection
            ? state with
            {
                SelectedSystem = null,
                EventFocusOwnsSelection = false,
            }
            : state;
    }

    public static OrderDraftState QueueMoveOrder(
        OrderDraftState state,
        HexCoord from,
        HexCoord to,
        ImmutableArray<NexusUnitStackGroup> stacks
    ) =>
        state with
        {
            PendingMoveOrders = [.. state.PendingMoveOrders, new NexusMoveOrder(from, to, stacks)],
        };

    public static OrderDraftState ApplyBuildDraftAdjustment(
        OrderDraftState state,
        NexusUnitType unit,
        BuildDraftAdjustmentKind adjustment,
        int projectedEnergy
    ) =>
        adjustment switch
        {
            BuildDraftAdjustmentKind.AddOne => AddBuildOrder(state, unit, projectedEnergy),
            BuildDraftAdjustmentKind.AddMax => AddBuildOrderMax(state, unit, projectedEnergy),
            BuildDraftAdjustmentKind.RemoveOne => RemoveBuildOrder(state, unit),
            BuildDraftAdjustmentKind.RemoveAll => RemoveBuildOrdersForUnit(state, unit),
            _ => state,
        };

    public static OrderDraftState ApplyPendingOrderRequest(
        OrderDraftState state,
        PendingOrderRequest request
    ) =>
        request.Kind switch
        {
            PendingOrderRequestKind.RemoveMoveOrder => state with
            {
                PendingMoveOrders =
                [
                    .. state.PendingMoveOrders.Where(order => order != request.MoveOrder),
                ],
            },
            PendingOrderRequestKind.RemoveBuildOrder => state with
            {
                PendingBuildOrders =
                [
                    .. state.PendingBuildOrders.Where(order =>
                        order.UnitType != request.BuildOrder!.UnitType
                    ),
                ],
            },
            PendingOrderRequestKind.ClearNexusGate => state with { PendingBeginNexusGate = false },
            _ => state,
        };

    public static OrderDraftState SetNexusGateDraft(OrderDraftState state, bool beginNexusGate) =>
        state with
        {
            PendingBeginNexusGate = beginNexusGate,
        };

    public static SidebarState SetActiveSidebarTab(SidebarState state, NexusGameplayTab tab) =>
        state with
        {
            ActiveTab = tab,
        };

    private static OrderDraftState AddBuildOrder(
        OrderDraftState state,
        NexusUnitType unit,
        int projectedEnergy
    )
    {
        if (projectedEnergy < unit.Cost())
            return state;

        var index = FindBuildOrderIndex(state.PendingBuildOrders, unit);
        if (index < 0)
        {
            return state with
            {
                PendingBuildOrders = [.. state.PendingBuildOrders, new NexusBuildOrder(unit, 1)],
            };
        }

        var updatedOrders = state.PendingBuildOrders.ToBuilder();
        updatedOrders[index] = updatedOrders[index] with { Count = updatedOrders[index].Count + 1 };
        return state with { PendingBuildOrders = updatedOrders.ToImmutable() };
    }

    private static OrderDraftState AddBuildOrderMax(
        OrderDraftState state,
        NexusUnitType unit,
        int projectedEnergy
    )
    {
        var nextState = state;
        var maxAdd = projectedEnergy / unit.Cost();
        for (var i = 0; i < maxAdd; i++)
        {
            nextState = AddBuildOrder(
                nextState,
                unit,
                projectedEnergy - nextState.ProjectedSpend + state.ProjectedSpend
            );
        }

        return nextState;
    }

    private static OrderDraftState RemoveBuildOrder(OrderDraftState state, NexusUnitType unit)
    {
        var index = FindBuildOrderIndex(state.PendingBuildOrders, unit);
        if (index < 0)
            return state;

        if (state.PendingBuildOrders[index].Count <= 1)
        {
            return state with
            {
                PendingBuildOrders = [.. state.PendingBuildOrders.RemoveAt(index)],
            };
        }

        var updatedOrders = state.PendingBuildOrders.ToBuilder();
        updatedOrders[index] = updatedOrders[index] with { Count = updatedOrders[index].Count - 1 };
        return state with { PendingBuildOrders = updatedOrders.ToImmutable() };
    }

    private static OrderDraftState RemoveBuildOrdersForUnit(
        OrderDraftState state,
        NexusUnitType unit
    ) =>
        state with
        {
            PendingBuildOrders =
            [
                .. state.PendingBuildOrders.Where(order => order.UnitType != unit),
            ],
        };

    private static int FindBuildOrderIndex(
        ImmutableArray<NexusBuildOrder> pendingBuildOrders,
        NexusUnitType unit
    )
    {
        for (var i = 0; i < pendingBuildOrders.Length; i++)
        {
            if (pendingBuildOrders[i].UnitType == unit)
                return i;
        }

        return -1;
    }

    public static NexusEventFocus GetEventFocus(NexusResolveEvent evt) =>
        evt switch
        {
            NexusUnitsMovedEvent e => new NexusEventFocus([e.From, e.To], e.From, e.To, e.To),
            NexusPlanetaryControlEvent e => new NexusEventFocus([e.System], Primary: e.System),
            NexusSystemContestedEvent e => new NexusEventFocus([e.System], Primary: e.System),
            NexusSystemUncontrolledEvent e => new NexusEventFocus([e.System], Primary: e.System),
            NexusCombatBeganEvent e => new NexusEventFocus([e.System], Primary: e.System),
            NexusPhaseResultEvent e => new NexusEventFocus([e.System], Primary: e.System),
            NexusSystemClearedEvent e => new NexusEventFocus([e.System], Primary: e.System),
            NexusUnitDeployedEvent e => new NexusEventFocus([e.HomeSystem], Primary: e.HomeSystem),
            NexusGateStartedEvent e => new NexusEventFocus([e.System], Primary: e.System),
            NexusGateCompletedEvent e => new NexusEventFocus([e.System], Primary: e.System),
            NexusGateCancelledEvent e => new NexusEventFocus([e.System], Primary: e.System),
            NexusIncomeEvent e when e.Sources.Length > 0 => new NexusEventFocus(
                e.Sources.ToArray(),
                Primary: e.Sources[0]
            ),
            _ => NexusEventFocus.None,
        };
}

public enum EventFocusRequestKind
{
    Preview,
    ClearPreview,
    Dismiss,
}

public enum SelectionRequestKind
{
    SelectSystem,
    ClearSelection,
}

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

public enum PendingOrderRequestKind
{
    RemoveMoveOrder,
    RemoveBuildOrder,
    ClearNexusGate,
}

public sealed record EventFocusRequest(
    EventFocusRequestKind Kind,
    NexusResolveEvent? ResolveEvent = null
)
{
    public static EventFocusRequest PreviewRequested(NexusResolveEvent resolveEvent) =>
        new(EventFocusRequestKind.Preview, resolveEvent);

    public static EventFocusRequest PreviewCleared() => new(EventFocusRequestKind.ClearPreview);

    public static EventFocusRequest Dismissed() => new(EventFocusRequestKind.Dismiss);
}

public sealed record SelectionRequest(SelectionRequestKind Kind, HexCoord? System = null)
{
    public static SelectionRequest SystemSelected(HexCoord system) =>
        new(SelectionRequestKind.SelectSystem, system);

    public static SelectionRequest SelectionCleared() => new(SelectionRequestKind.ClearSelection);
}

public sealed record OrderDraftRequest(
    OrderDraftRequestKind Kind,
    ImmutableArray<NexusUnitStackGroup> MoveDraftStacks = default,
    NexusUnitType? UnitType = null,
    BuildDraftAdjustmentKind? BuildAdjustment = null,
    bool? BeginNexusGate = null
)
{
    public static OrderDraftRequest MoveDraftChanged(ImmutableArray<NexusUnitStackGroup> stacks) =>
        new(OrderDraftRequestKind.MoveDraftChanged, MoveDraftStacks: stacks);

    public static OrderDraftRequest BuildDraftAdjusted(
        NexusUnitType unitType,
        BuildDraftAdjustmentKind adjustment
    ) =>
        new(
            OrderDraftRequestKind.BuildDraftAdjusted,
            UnitType: unitType,
            BuildAdjustment: adjustment
        );

    public static OrderDraftRequest NexusGateDraftChanged(bool beginNexusGate) =>
        new(OrderDraftRequestKind.NexusGateDraftChanged, BeginNexusGate: beginNexusGate);
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

public sealed record EventFocusState(NexusEventFocus Preview)
{
    public static EventFocusState Empty { get; } = new(NexusEventFocus.None);

    public NexusEventFocus Active => Preview;
}

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

public sealed record OrderDraftState(
    ImmutableArray<NexusMoveOrder> PendingMoveOrders,
    ImmutableArray<NexusBuildOrder> PendingBuildOrders,
    bool PendingBeginNexusGate
)
{
    public static OrderDraftState Empty { get; } = new([], [], false);

    public int ProjectedSpend =>
        PendingBuildOrders.Sum(order => order.UnitType.Cost() * order.Count)
        + (PendingBeginNexusGate ? 12 : 0);
}

public sealed record SidebarState(NexusGameplayTab ActiveTab)
{
    public static SidebarState Default { get; } = new(NexusGameplayTab.Orders);
}

public sealed record NexusEventFocus(
    IReadOnlyList<HexCoord> Systems,
    HexCoord? From = null,
    HexCoord? To = null,
    HexCoord? Primary = null
)
{
    public static NexusEventFocus None { get; } = new([]);

    public bool HasTarget => Systems.Count > 0 || From.HasValue || To.HasValue || Primary.HasValue;

    public bool Matches(NexusEventFocus other)
    {
        if (!HasTarget || !other.HasTarget)
            return false;

        return Primary == other.Primary && From == other.From && To == other.To;
    }
}

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
                Current = GetEventFocus(request.ResolveEvent!),
            },
            EventFocusRequestKind.ClearPreview => state with { Current = EventFocus.None },
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
            DraftPath = ImmutableArray<HexCoord>.Empty,
            FleetMove = 0,
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
            DraftPath = ImmutableArray<HexCoord>.Empty,
            FleetMove = 0,
        };

    public static SelectionState ApplyMoveDraft(
        SelectionState state,
        ImmutableArray<NexusUnitStackGroup> stacks,
        NexusGameView session,
        Guid playerId
    )
    {
        if (state.SelectedSystem is null || stacks.IsDefaultOrEmpty)
            return state with
            {
                StagedMoveStacks = stacks,
                ValidMoveTargets = [],
                DraftPath = ImmutableArray<HexCoord>.Empty,
                FleetMove = 0,
            };

        var fleetMove = ComputeFleetMove(stacks, session.CurrentPlayer.Designs);
        var validTargets =
            fleetMove > 0
                ? NexusViewQueries.GetValidNextHops(
                    session,
                    playerId,
                    state.SelectedSystem.Value,
                    ImmutableArray<HexCoord>.Empty,
                    fleetMove
                )
                : (IReadOnlyList<HexCoord>)[];

        return state with
        {
            StagedMoveStacks = stacks,
            ValidMoveTargets = validTargets,
            DraftPath = ImmutableArray<HexCoord>.Empty,
            FleetMove = fleetMove,
        };
    }

    public static SelectionState ApplyPathStep(
        SelectionState state,
        HexCoord step,
        NexusGameView session,
        Guid playerId
    )
    {
        var newPath = state.DraftPath.IsDefaultOrEmpty
            ? ImmutableArray.Create(step)
            : [.. state.DraftPath, step];

        var remainingMove = state.FleetMove - newPath.Length;

        var opponentId = session.Opponent.PlayerId;
        var stepSystem = session.Systems.FirstOrDefault(s => s.Coord == step);
        var enemyBlocksExtension =
            stepSystem is not null
            && stepSystem
                .GetPlayerStacks(opponentId)
                .Any(s => s.Category is NexusUnitCategory.Strike or NexusUnitCategory.Capital);

        IReadOnlyList<HexCoord> validNextHops;
        if (remainingMove <= 0 || enemyBlocksExtension)
        {
            validNextHops = [];
        }
        else
        {
            validNextHops = NexusViewQueries.GetValidNextHops(
                session,
                playerId,
                state.SelectedSystem!.Value,
                newPath,
                remainingMove
            );
        }

        return state with
        {
            DraftPath = newPath,
            ValidMoveTargets = validNextHops,
        };
    }

    public static SelectionState UndoPathStep(
        SelectionState state,
        NexusGameView session,
        Guid playerId
    )
    {
        if (state.DraftPath.IsDefaultOrEmpty)
            return state;

        var newPath = state.DraftPath.RemoveAt(state.DraftPath.Length - 1);
        var remainingMove = state.FleetMove - newPath.Length;

        var validNextHops =
            state.SelectedSystem.HasValue && remainingMove > 0
                ? NexusViewQueries.GetValidNextHops(
                    session,
                    playerId,
                    state.SelectedSystem.Value,
                    newPath,
                    remainingMove
                )
                : (IReadOnlyList<HexCoord>)[];

        return state with
        {
            DraftPath = newPath,
            ValidMoveTargets = validNextHops,
        };
    }

    public static SelectionState ClearDraftPath(SelectionState state) =>
        state with
        {
            DraftPath = ImmutableArray<HexCoord>.Empty,
            ValidMoveTargets = [],
            StagedMoveStacks = ImmutableArray<NexusUnitStackGroup>.Empty,
            FleetMove = 0,
        };

    public static SelectionState SyncSelectedSystemToEventFocus(
        SelectionState state,
        EventFocusState eventFocusState
    )
    {
        var activeFocus = eventFocusState.Current;
        if (activeFocus.Primary.HasValue)
        {
            return state with
            {
                SelectedSystem = activeFocus.Primary.Value,
                EventFocusOwnsSelection = true,
                DraftPath = ImmutableArray<HexCoord>.Empty,
                FleetMove = 0,
            };
        }

        return state.EventFocusOwnsSelection
            ? state with
            {
                SelectedSystem = null,
                EventFocusOwnsSelection = false,
                DraftPath = ImmutableArray<HexCoord>.Empty,
                FleetMove = 0,
            }
            : state;
    }

    public static OrderDraftState QueueMoveOrder(
        OrderDraftState state,
        HexCoord from,
        ImmutableArray<HexCoord> waypoints,
        ImmutableArray<NexusUnitStackGroup> stacks
    ) =>
        state with
        {
            PendingMoveOrders =
            [
                .. state.PendingMoveOrders,
                new NexusMoveOrder(from, waypoints, stacks),
            ],
        };

    public static OrderDraftState ApplyBuildDraftAdjustment(
        OrderDraftState state,
        Guid designId,
        BuildDraftAdjustmentKind adjustment,
        int projectedEnergy,
        IReadOnlyList<NexusUnitDesign> designs
    ) =>
        adjustment switch
        {
            BuildDraftAdjustmentKind.AddOne => AddBuildOrder(
                state,
                designId,
                projectedEnergy,
                designs
            ),
            BuildDraftAdjustmentKind.AddMax => AddBuildOrderMax(
                state,
                designId,
                projectedEnergy,
                designs
            ),
            BuildDraftAdjustmentKind.RemoveOne => RemoveBuildOrder(state, designId),
            BuildDraftAdjustmentKind.RemoveAll => RemoveBuildOrdersForUnit(state, designId),
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
                        order.DesignId != request.BuildOrder!.DesignId
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
        Guid designId,
        int projectedEnergy,
        IReadOnlyList<NexusUnitDesign> designs
    )
    {
        var design = designs.FirstOrDefault(d => d.DesignId == designId);
        if (design is null)
            return state;

        var cost = NexusHullBaselines.GetProfile(design).Cost;
        if (projectedEnergy < cost)
            return state;

        var index = FindBuildOrderIndex(state.PendingBuildOrders, designId);
        if (index < 0)
        {
            return state with
            {
                PendingBuildOrders =
                [
                    .. state.PendingBuildOrders,
                    new NexusBuildOrder(designId, 1),
                ],
            };
        }

        var updatedOrders = state.PendingBuildOrders.ToBuilder();
        updatedOrders[index] = updatedOrders[index] with { Count = updatedOrders[index].Count + 1 };
        return state with { PendingBuildOrders = updatedOrders.ToImmutable() };
    }

    private static OrderDraftState AddBuildOrderMax(
        OrderDraftState state,
        Guid designId,
        int projectedEnergy,
        IReadOnlyList<NexusUnitDesign> designs
    )
    {
        var design = designs.FirstOrDefault(d => d.DesignId == designId);
        if (design is null)
            return state;

        var cost = NexusHullBaselines.GetProfile(design).Cost;
        if (cost <= 0)
            return state;

        var nextState = state;
        var designsLookup = designs.ToDictionary(d => d.DesignId);
        var baseSpend = state.ComputeProjectedSpend(designsLookup);
        var maxAdd = projectedEnergy / cost;
        for (var i = 0; i < maxAdd; i++)
        {
            nextState = AddBuildOrder(
                nextState,
                designId,
                projectedEnergy - nextState.ComputeProjectedSpend(designsLookup) + baseSpend,
                designs
            );
        }

        return nextState;
    }

    private static OrderDraftState RemoveBuildOrder(OrderDraftState state, Guid designId)
    {
        var index = FindBuildOrderIndex(state.PendingBuildOrders, designId);
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

    private static OrderDraftState RemoveBuildOrdersForUnit(OrderDraftState state, Guid designId) =>
        state with
        {
            PendingBuildOrders =
            [
                .. state.PendingBuildOrders.Where(order => order.DesignId != designId),
            ],
        };

    private static int FindBuildOrderIndex(
        ImmutableArray<NexusBuildOrder> pendingBuildOrders,
        Guid designId
    )
    {
        for (var i = 0; i < pendingBuildOrders.Length; i++)
        {
            if (pendingBuildOrders[i].DesignId == designId)
                return i;
        }

        return -1;
    }

    private static int ComputeFleetMove(
        ImmutableArray<NexusUnitStackGroup> stacks,
        ImmutableArray<NexusUnitDesign> designs
    )
    {
        var designLookup = designs.ToDictionary(d => d.DesignId);
        var minMove = int.MaxValue;
        var found = false;
        foreach (var stack in stacks)
        {
            if (!designLookup.TryGetValue(stack.DesignId, out var design))
                continue;
            var move = NexusHullBaselines.GetProfile(design).Move;
            if (move > 0)
            {
                minMove = Math.Min(minMove, move);
                found = true;
            }
        }
        return found ? minMove : 0;
    }

    public static EventFocus GetEventFocus(NexusResolveEvent evt) =>
        evt switch
        {
            NexusUnitsMovedEvent e => new EventFocus([e.From, e.To], e.From, e.To, e.To),
            NexusPlanetaryControlEvent e => new EventFocus([e.System], Primary: e.System),
            NexusSystemContestedEvent e => new EventFocus([e.System], Primary: e.System),
            NexusSystemUncontrolledEvent e => new EventFocus([e.System], Primary: e.System),
            NexusCombatResultEvent e => new EventFocus([e.System], Primary: e.System),
            NexusSystemClearedEvent e => new EventFocus([e.System], Primary: e.System),
            NexusUnitDeployedEvent e => new EventFocus([e.HomeSystem], Primary: e.HomeSystem),
            NexusGateStartedEvent e => new EventFocus([e.System], Primary: e.System),
            NexusGateCompletedEvent e => new EventFocus([e.System], Primary: e.System),
            NexusGateCancelledEvent e => new EventFocus([e.System], Primary: e.System),
            NexusIncomeEvent e when e.Sources.Length > 0 => new EventFocus(
                e.Sources.ToArray(),
                Primary: e.Sources[0]
            ),
            _ => EventFocus.None,
        };
}

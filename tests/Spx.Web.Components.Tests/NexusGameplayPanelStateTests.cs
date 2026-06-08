using System.Collections.Immutable;
using Spx.Nexus.Domain;
using Spx.Web.Components.Nexus;
using Xunit;

namespace Spx.Web.Components.Tests;

public sealed class NexusGameplayPanelStateTests
{
    private static readonly Guid CarrierDesignId = Guid.Parse(
        "aaaaaaaa-0000-0000-0000-000000000001"
    );
    private static readonly Guid FighterDesignId = Guid.Parse(
        "aaaaaaaa-0000-0000-0000-000000000002"
    );
    private static readonly Guid Player1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Player2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly HexCoord CoordA = new(1, 2);
    private static readonly HexCoord CoordB = new(3, 4);
    private static readonly HexCoord CoordC = new(5, -1);

    private static NexusUnitDesign FighterDesign =>
        new()
        {
            DesignId = FighterDesignId,
            Name = "Fighter",
            Hull = NexusUnitCategory.Strike,
            Modules =
            [
                new Battery(NexusUnitCategory.Strike),
                new Battery(NexusUnitCategory.Capital),
                new Dock(),
            ],
        };

    // ── ShouldResetUiState ────────────────────────────────────────────────────

    [Fact]
    public void ShouldResetUiState_WhenGameIdChangesWithSameRound_ReturnsTrue()
    {
        var shouldReset = NexusGameplayPanelState.ShouldResetUiState(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            sessionCurrentPlayerId: Player1Id,
            sessionRound: 3,
            lastKnownGameId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            lastKnownCurrentPlayerId: Player1Id,
            lastKnownRound: 3
        );

        Assert.True(shouldReset);
    }

    [Fact]
    public void ShouldResetUiState_WhenGameAndRoundUnchanged_ReturnsFalse()
    {
        var gameId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var shouldReset = NexusGameplayPanelState.ShouldResetUiState(
            gameId,
            sessionCurrentPlayerId: Player1Id,
            sessionRound: 4,
            lastKnownGameId: gameId,
            lastKnownCurrentPlayerId: Player1Id,
            lastKnownRound: 4
        );

        Assert.False(shouldReset);
    }

    [Fact]
    public void ShouldResetUiState_WhenCurrentPlayerChangesWithSameGameAndRound_ReturnsTrue()
    {
        var gameId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var shouldReset = NexusGameplayPanelState.ShouldResetUiState(
            gameId,
            sessionCurrentPlayerId: Player2Id,
            sessionRound: 4,
            lastKnownGameId: gameId,
            lastKnownCurrentPlayerId: Player1Id,
            lastKnownRound: 4
        );

        Assert.True(shouldReset);
    }

    [Fact]
    public void ShouldResetUiState_WhenRoundChangesWithSameGameAndPlayer_ReturnsTrue()
    {
        var gameId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var shouldReset = NexusGameplayPanelState.ShouldResetUiState(
            gameId,
            sessionCurrentPlayerId: Player1Id,
            sessionRound: 5,
            lastKnownGameId: gameId,
            lastKnownCurrentPlayerId: Player1Id,
            lastKnownRound: 4
        );

        Assert.True(shouldReset);
    }

    // ── ApplyEventFocusRequest ────────────────────────────────────────────────

    [Fact]
    public void ApplyEventFocusRequest_WhenPreviewRequested_SetsFocus()
    {
        var movementEvent = new NexusUnitsMovedEvent(
            Player1Id,
            CoordA,
            CoordB,
            ImmutableArray<NexusUnitStackGroup>.Empty,
            IsRetreat: false
        );

        var state = NexusGameplayPanelState.ApplyEventFocusRequest(
            EventFocusState.Empty,
            EventFocusRequest.PreviewRequested(movementEvent)
        );

        Assert.Equal(CoordB, state.Current.Primary);
        Assert.Equal(CoordA, state.Current.From);
        Assert.Equal(CoordB, state.Current.To);
        Assert.Equal([CoordA, CoordB], state.Current.Systems);
    }

    [Fact]
    public void ApplyEventFocusRequest_WhenPreviewCleared_ClearsFocus()
    {
        var previewEvent = new NexusIncomeEvent(Player1Id, Amount: 3, [CoordA, CoordB]);

        var previewState = NexusGameplayPanelState.ApplyEventFocusRequest(
            EventFocusState.Empty,
            EventFocusRequest.PreviewRequested(previewEvent)
        );
        var clearedState = NexusGameplayPanelState.ApplyEventFocusRequest(
            previewState,
            EventFocusRequest.PreviewCleared()
        );

        Assert.False(clearedState.Current.HasTarget);
    }

    [Fact]
    public void ApplyEventFocusRequest_WhenDismissed_ClearsFocus()
    {
        var previewEvent = new NexusIncomeEvent(Player1Id, Amount: 3, [CoordA, CoordB]);

        var previewState = NexusGameplayPanelState.ApplyEventFocusRequest(
            EventFocusState.Empty,
            EventFocusRequest.PreviewRequested(previewEvent)
        );
        var dismissedState = NexusGameplayPanelState.ApplyEventFocusRequest(
            previewState,
            EventFocusRequest.Dismissed()
        );

        Assert.False(dismissedState.Current.HasTarget);
    }

    // ── GetEventFocus ─────────────────────────────────────────────────────────

    [Fact]
    public void GetEventFocus_ForUnitsMovedEvent_SetsFromToAndDestinationAsPrimary()
    {
        var evt = new NexusUnitsMovedEvent(
            Player1Id,
            CoordA,
            CoordB,
            ImmutableArray<NexusUnitStackGroup>.Empty,
            IsRetreat: false
        );

        var focus = NexusGameplayPanelState.GetEventFocus(evt);

        Assert.Equal([CoordA, CoordB], focus.Systems);
        Assert.Equal(CoordA, focus.From);
        Assert.Equal(CoordB, focus.To);
        Assert.Equal(CoordB, focus.Primary);
    }

    [Fact]
    public void GetEventFocus_ForPlanetaryControlEvent_SetsSystemAsPrimary()
    {
        var evt = new NexusPlanetaryControlEvent(CoordA, Player1Id);

        var focus = NexusGameplayPanelState.GetEventFocus(evt);

        Assert.Equal([CoordA], focus.Systems);
        Assert.Equal(CoordA, focus.Primary);
    }

    [Fact]
    public void GetEventFocus_ForSystemContestedEvent_SetsSystemAsPrimary()
    {
        var evt = new NexusSystemContestedEvent(CoordA);

        var focus = NexusGameplayPanelState.GetEventFocus(evt);

        Assert.Equal([CoordA], focus.Systems);
        Assert.Equal(CoordA, focus.Primary);
    }

    [Fact]
    public void GetEventFocus_ForSystemUncontrolledEvent_SetsSystemAsPrimary()
    {
        var evt = new NexusSystemUncontrolledEvent(CoordA);

        var focus = NexusGameplayPanelState.GetEventFocus(evt);

        Assert.Equal([CoordA], focus.Systems);
        Assert.Equal(CoordA, focus.Primary);
    }

    [Fact]
    public void GetEventFocus_ForCombatResultEvent_SetsSystemAsPrimary()
    {
        var evt = new NexusCombatResultEvent(
            CoordA,
            Player1Id,
            Player2Id,
            ImmutableArray<NexusPhaseResult>.Empty
        );

        var focus = NexusGameplayPanelState.GetEventFocus(evt);

        Assert.Equal([CoordA], focus.Systems);
        Assert.Equal(CoordA, focus.Primary);
    }

    [Fact]
    public void GetEventFocus_ForSystemClearedEvent_SetsSystemAsPrimary()
    {
        var evt = new NexusSystemClearedEvent(CoordA, Player1Id);

        var focus = NexusGameplayPanelState.GetEventFocus(evt);

        Assert.Equal([CoordA], focus.Systems);
        Assert.Equal(CoordA, focus.Primary);
    }

    [Fact]
    public void GetEventFocus_ForUnitDeployedEvent_SetsHomeSystemAsPrimary()
    {
        var evt = new NexusUnitDeployedEvent(Player1Id, FighterDesignId, "Fighter", CoordA, 2);

        var focus = NexusGameplayPanelState.GetEventFocus(evt);

        Assert.Equal([CoordA], focus.Systems);
        Assert.Equal(CoordA, focus.Primary);
    }

    [Fact]
    public void GetEventFocus_ForGateStartedEvent_SetsSystemAsPrimary()
    {
        var evt = new NexusGateStartedEvent(Player1Id, CoordA);

        var focus = NexusGameplayPanelState.GetEventFocus(evt);

        Assert.Equal([CoordA], focus.Systems);
        Assert.Equal(CoordA, focus.Primary);
    }

    [Fact]
    public void GetEventFocus_ForGateCompletedEvent_SetsSystemAsPrimary()
    {
        var evt = new NexusGateCompletedEvent(Player1Id, CoordA);

        var focus = NexusGameplayPanelState.GetEventFocus(evt);

        Assert.Equal([CoordA], focus.Systems);
        Assert.Equal(CoordA, focus.Primary);
    }

    [Fact]
    public void GetEventFocus_ForGateCancelledEvent_SetsSystemAsPrimary()
    {
        var evt = new NexusGateCancelledEvent(Player1Id, CoordA);

        var focus = NexusGameplayPanelState.GetEventFocus(evt);

        Assert.Equal([CoordA], focus.Systems);
        Assert.Equal(CoordA, focus.Primary);
    }

    [Fact]
    public void GetEventFocus_ForIncomeEvent_WithSources_SetsPrimaryToFirstSource()
    {
        var evt = new NexusIncomeEvent(Player1Id, Amount: 5, [CoordA, CoordB, CoordC]);

        var focus = NexusGameplayPanelState.GetEventFocus(evt);

        Assert.Equal([CoordA, CoordB, CoordC], focus.Systems);
        Assert.Equal(CoordA, focus.Primary);
    }

    [Fact]
    public void GetEventFocus_ForIncomeEvent_WithNoSources_ReturnsNone()
    {
        var evt = new NexusIncomeEvent(Player1Id, Amount: 5, []);

        var focus = NexusGameplayPanelState.GetEventFocus(evt);

        Assert.False(focus.HasTarget);
    }

    [Fact]
    public void GetEventFocus_ForCapitalDisbandedEvent_ReturnsNone()
    {
        var evt = new NexusCapitalDisbandedEvent(Player1Id, FighterDesignId, "Fighter", CoordA, 1);

        var focus = NexusGameplayPanelState.GetEventFocus(evt);

        Assert.False(focus.HasTarget);
    }

    [Fact]
    public void GetEventFocus_ForVictoryEvent_ReturnsNone()
    {
        var evt = new NexusVictoryEvent(Player1Id);

        var focus = NexusGameplayPanelState.GetEventFocus(evt);

        Assert.False(focus.HasTarget);
    }

    [Fact]
    public void GetEventFocus_ForDrawEvent_ReturnsNone()
    {
        var evt = new NexusDrawEvent("Both players completed the Nexus Gate simultaneously.");

        var focus = NexusGameplayPanelState.GetEventFocus(evt);

        Assert.False(focus.HasTarget);
    }

    // ── ApplySelectionRequest ─────────────────────────────────────────────────

    [Fact]
    public void ApplySelectionRequest_WhenSameSystemSelected_TogglesSelectionOff()
    {
        var selectedSystem = NexusGameplayPanelState.ApplySelectionRequest(
            CoordA,
            SelectionRequest.SystemSelected(CoordA)
        );

        Assert.Null(selectedSystem);
    }

    [Fact]
    public void ApplySelectionRequest_WhenDifferentSystemSelected_ReturnsNewSystem()
    {
        var selectedSystem = NexusGameplayPanelState.ApplySelectionRequest(
            CoordA,
            SelectionRequest.SystemSelected(CoordB)
        );

        Assert.Equal(CoordB, selectedSystem);
    }

    [Fact]
    public void ApplySelectionRequest_WhenSelectionCleared_ReturnsNull()
    {
        var selectedSystem = NexusGameplayPanelState.ApplySelectionRequest(
            CoordB,
            SelectionRequest.SelectionCleared()
        );

        Assert.Null(selectedSystem);
    }

    // ── SelectionState operations ─────────────────────────────────────────────

    [Fact]
    public void ClearSelection_ClearsAllSelectionFields()
    {
        var state = SelectionState.Empty with
        {
            SelectedSystem = CoordA,
            StagedMoveStacks = ImmutableArray.Create(
                new NexusUnitStackGroup(FighterDesignId, NexusUnitCategory.Strike, 1, 2, "Fighter")
            ),
            ValidMoveTargets = [CoordB],
            EventFocusOwnsSelection = true,
        };

        var nextState = NexusGameplayPanelState.ClearSelection(state);

        Assert.Null(nextState.SelectedSystem);
        Assert.Empty(nextState.StagedMoveStacks);
        Assert.Empty(nextState.ValidMoveTargets);
        Assert.False(nextState.EventFocusOwnsSelection);
    }

    [Fact]
    public void ApplySelectionRequest_OnSelectionState_ClearsStagedMoveStacks()
    {
        var state = SelectionState.Empty with
        {
            SelectedSystem = CoordA,
            StagedMoveStacks = ImmutableArray.Create(
                new NexusUnitStackGroup(FighterDesignId, NexusUnitCategory.Strike, 1, 2, "Fighter")
            ),
        };

        var nextState = NexusGameplayPanelState.ApplySelectionRequest(
            state,
            SelectionRequest.SystemSelected(CoordB)
        );

        Assert.Equal(CoordB, nextState.SelectedSystem);
        Assert.Empty(nextState.StagedMoveStacks);
        Assert.Empty(nextState.ValidMoveTargets);
        Assert.False(nextState.EventFocusOwnsSelection);
    }

    // ── ApplyMoveDraft ────────────────────────────────────────────────────────

    [Fact]
    public void ApplyMoveDraft_WhenSystemSelected_PopulatesValidMoveTargets()
    {
        var gameId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var session = GamePageCoordinatorTestData.CreateGameplayPanelSession(gameId);

        var nextState = NexusGameplayPanelState.ApplyMoveDraft(
            SelectionState.Empty with
            {
                SelectedSystem = GamePageCoordinatorTestData.CurrentPlayerHomeCoord,
            },
            ImmutableArray.Create(
                new NexusUnitStackGroup(CarrierDesignId, NexusUnitCategory.Capital, 1, 1, "Carrier")
            ),
            session,
            GamePageCoordinatorTestData.CurrentPlayerId
        );

        Assert.Contains(GamePageCoordinatorTestData.MoveTargetCoord, nextState.ValidMoveTargets);
        Assert.Equal(GamePageCoordinatorTestData.CurrentPlayerHomeCoord, nextState.SelectedSystem);
    }

    [Fact]
    public void ApplyMoveDraft_WhenNoSystemSelected_ReturnsEmptyTargets()
    {
        var gameId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var session = GamePageCoordinatorTestData.CreateGameplayPanelSession(gameId);

        var nextState = NexusGameplayPanelState.ApplyMoveDraft(
            SelectionState.Empty,
            ImmutableArray.Create(
                new NexusUnitStackGroup(CarrierDesignId, NexusUnitCategory.Capital, 1, 1, "Carrier")
            ),
            session,
            GamePageCoordinatorTestData.CurrentPlayerId
        );

        Assert.Null(nextState.SelectedSystem);
        Assert.Empty(nextState.ValidMoveTargets);
        Assert.Single(nextState.StagedMoveStacks);
    }

    [Fact]
    public void ApplyMoveDraft_WhenStacksAreDefault_ReturnsEmptyTargets()
    {
        var gameId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var session = GamePageCoordinatorTestData.CreateGameplayPanelSession(gameId);

        var nextState = NexusGameplayPanelState.ApplyMoveDraft(
            SelectionState.Empty with
            {
                SelectedSystem = GamePageCoordinatorTestData.CurrentPlayerHomeCoord,
            },
            default,
            session,
            GamePageCoordinatorTestData.CurrentPlayerId
        );

        Assert.Empty(nextState.ValidMoveTargets);
    }

    [Fact]
    public void QueueMoveOrder_AppendsToPendingMoveOrders()
    {
        var stacks = ImmutableArray.Create(
            new NexusUnitStackGroup(FighterDesignId, NexusUnitCategory.Strike, 1, 3, "Fighter")
        );

        var nextState = NexusGameplayPanelState.QueueMoveOrder(
            OrderDraftState.Empty,
            CoordA,
            ImmutableArray.Create(CoordB),
            stacks
        );

        var moveOrder = Assert.Single(nextState.PendingMoveOrders);
        Assert.Equal(CoordA, moveOrder.From);
        Assert.Equal(CoordB, moveOrder.To);
        Assert.Equal(stacks, moveOrder.Stacks);
    }

    // ── SyncSelectedSystemToEventFocus ────────────────────────────────────────

    [Fact]
    public void SyncSelectedSystemToEventFocus_WhenFocusHasPrimary_SetsSelectedSystem()
    {
        var focusState = new EventFocusState(new EventFocus([CoordA], Primary: CoordA));

        var nextState = NexusGameplayPanelState.SyncSelectedSystemToEventFocus(
            SelectionState.Empty,
            focusState
        );

        Assert.Equal(CoordA, nextState.SelectedSystem);
        Assert.True(nextState.EventFocusOwnsSelection);
    }

    [Fact]
    public void SyncSelectedSystemToEventFocus_WhenFocusClearsOwnedSelection_ClearsSystem()
    {
        var selectionState = SelectionState.Empty with
        {
            SelectedSystem = CoordA,
            EventFocusOwnsSelection = true,
        };

        var nextState = NexusGameplayPanelState.SyncSelectedSystemToEventFocus(
            selectionState,
            EventFocusState.Empty
        );

        Assert.Null(nextState.SelectedSystem);
        Assert.False(nextState.EventFocusOwnsSelection);
    }

    [Fact]
    public void SyncSelectedSystemToEventFocus_WhenNotOwned_KeepsExistingSelection()
    {
        var selectionState = SelectionState.Empty with
        {
            SelectedSystem = CoordA,
            EventFocusOwnsSelection = false,
        };

        var nextState = NexusGameplayPanelState.SyncSelectedSystemToEventFocus(
            selectionState,
            EventFocusState.Empty
        );

        Assert.Equal(CoordA, nextState.SelectedSystem);
        Assert.False(nextState.EventFocusOwnsSelection);
    }

    // ── SetActiveSidebarTab ───────────────────────────────────────────────────

    [Fact]
    public void SetActiveSidebarTab_SetsActiveTab()
    {
        var nextState = NexusGameplayPanelState.SetActiveSidebarTab(
            SidebarState.Default,
            NexusGameplayTab.Events
        );

        Assert.Equal(NexusGameplayTab.Events, nextState.ActiveTab);
    }

    // ── ApplyBuildDraftAdjustment ─────────────────────────────────────────────

    [Fact]
    public void ApplyBuildDraftAdjustment_WhenAddOneRequested_AddsBuildOrder()
    {
        var nextState = NexusGameplayPanelState.ApplyBuildDraftAdjustment(
            OrderDraftState.Empty,
            FighterDesignId,
            BuildDraftAdjustmentKind.AddOne,
            projectedEnergy: 20,
            designs: [FighterDesign]
        );

        var buildOrder = Assert.Single(nextState.PendingBuildOrders);
        Assert.Equal(FighterDesignId, buildOrder.DesignId);
        Assert.Equal(1, buildOrder.Count);
    }

    [Fact]
    public void ApplyBuildDraftAdjustment_WhenAddOneAndInsufficientEnergy_ReturnsUnchanged()
    {
        var nextState = NexusGameplayPanelState.ApplyBuildDraftAdjustment(
            OrderDraftState.Empty,
            FighterDesignId,
            BuildDraftAdjustmentKind.AddOne,
            projectedEnergy: 0,
            designs: [FighterDesign]
        );

        Assert.Empty(nextState.PendingBuildOrders);
    }

    [Fact]
    public void ApplyBuildDraftAdjustment_WhenAddOne_AccumulatesMultipleOrders()
    {
        var afterOne = NexusGameplayPanelState.ApplyBuildDraftAdjustment(
            OrderDraftState.Empty,
            FighterDesignId,
            BuildDraftAdjustmentKind.AddOne,
            projectedEnergy: 20,
            designs: [FighterDesign]
        );
        var afterTwo = NexusGameplayPanelState.ApplyBuildDraftAdjustment(
            afterOne,
            FighterDesignId,
            BuildDraftAdjustmentKind.AddOne,
            projectedEnergy: 20,
            designs: [FighterDesign]
        );

        var buildOrder = Assert.Single(afterTwo.PendingBuildOrders);
        Assert.Equal(2, buildOrder.Count);
    }

    [Fact]
    public void ApplyBuildDraftAdjustment_WhenAddMaxRequested_AddsMaximumAffordable()
    {
        var nextState = NexusGameplayPanelState.ApplyBuildDraftAdjustment(
            OrderDraftState.Empty,
            FighterDesignId,
            BuildDraftAdjustmentKind.AddMax,
            projectedEnergy: 20,
            designs: [FighterDesign]
        );

        var buildOrder = Assert.Single(nextState.PendingBuildOrders);
        Assert.Equal(FighterDesignId, buildOrder.DesignId);
        Assert.True(buildOrder.Count > 0, "AddMax should produce at least 1 order");
    }

    [Fact]
    public void ApplyBuildDraftAdjustment_WhenRemoveOneRequested_RemovesOneFromExisting()
    {
        var startState = OrderDraftState.Empty with
        {
            PendingBuildOrders = [new NexusBuildOrder(FighterDesignId, 3)],
        };

        var nextState = NexusGameplayPanelState.ApplyBuildDraftAdjustment(
            startState,
            FighterDesignId,
            BuildDraftAdjustmentKind.RemoveOne,
            projectedEnergy: 20,
            designs: [FighterDesign]
        );

        var buildOrder = Assert.Single(nextState.PendingBuildOrders);
        Assert.Equal(2, buildOrder.Count);
    }

    [Fact]
    public void ApplyBuildDraftAdjustment_WhenRemoveOneFromSingleCount_RemovesOrder()
    {
        var startState = OrderDraftState.Empty with
        {
            PendingBuildOrders = [new NexusBuildOrder(FighterDesignId, 1)],
        };

        var nextState = NexusGameplayPanelState.ApplyBuildDraftAdjustment(
            startState,
            FighterDesignId,
            BuildDraftAdjustmentKind.RemoveOne,
            projectedEnergy: 20,
            designs: [FighterDesign]
        );

        Assert.Empty(nextState.PendingBuildOrders);
    }

    [Fact]
    public void ApplyBuildDraftAdjustment_WhenRemoveAllRequested_RemovesAllForDesign()
    {
        var startState = OrderDraftState.Empty with
        {
            PendingBuildOrders =
            [
                new NexusBuildOrder(FighterDesignId, 3),
                new NexusBuildOrder(CarrierDesignId, 1),
            ],
        };

        var nextState = NexusGameplayPanelState.ApplyBuildDraftAdjustment(
            startState,
            FighterDesignId,
            BuildDraftAdjustmentKind.RemoveAll,
            projectedEnergy: 20,
            designs: [FighterDesign]
        );

        Assert.Single(nextState.PendingBuildOrders);
        Assert.Equal(CarrierDesignId, nextState.PendingBuildOrders[0].DesignId);
    }

    // ── SetNexusGateDraft ─────────────────────────────────────────────────────

    [Fact]
    public void SetNexusGateDraft_WhenTrue_SetsGateDraft()
    {
        var nextState = NexusGameplayPanelState.SetNexusGateDraft(OrderDraftState.Empty, true);

        Assert.True(nextState.PendingBeginNexusGate);
    }

    [Fact]
    public void SetNexusGateDraft_WhenFalse_ClearsGateDraft()
    {
        var startState = OrderDraftState.Empty with { PendingBeginNexusGate = true };

        var nextState = NexusGameplayPanelState.SetNexusGateDraft(startState, false);

        Assert.False(nextState.PendingBeginNexusGate);
    }

    // ── ApplyPendingOrderRequest ──────────────────────────────────────────────

    [Fact]
    public void ApplyPendingOrderRequest_WhenMoveOrderRemoved_RemovesQueuedMove()
    {
        var moveOrder = new NexusMoveOrder(
            CoordA,
            ImmutableArray.Create(CoordB),
            ImmutableArray.Create(
                new NexusUnitStackGroup(CarrierDesignId, NexusUnitCategory.Capital, 4, 1)
            )
        );

        var nextState = NexusGameplayPanelState.ApplyPendingOrderRequest(
            OrderDraftState.Empty with
            {
                PendingMoveOrders = [moveOrder],
            },
            PendingOrderRequest.MoveOrderRemovalRequested(moveOrder)
        );

        Assert.Empty(nextState.PendingMoveOrders);
    }

    [Fact]
    public void ApplyPendingOrderRequest_WhenBuildOrderRemoved_RemovesQueuedBuild()
    {
        var buildOrder = new NexusBuildOrder(FighterDesignId, 2);

        var nextState = NexusGameplayPanelState.ApplyPendingOrderRequest(
            OrderDraftState.Empty with
            {
                PendingBuildOrders = [buildOrder],
            },
            PendingOrderRequest.BuildOrderRemovalRequested(buildOrder)
        );

        Assert.Empty(nextState.PendingBuildOrders);
    }

    [Fact]
    public void ApplyPendingOrderRequest_WhenGateCleared_ClearsGateDraft()
    {
        var nextState = NexusGameplayPanelState.ApplyPendingOrderRequest(
            OrderDraftState.Empty with
            {
                PendingBeginNexusGate = true,
            },
            PendingOrderRequest.NexusGateCleared()
        );

        Assert.False(nextState.PendingBeginNexusGate);
    }
}

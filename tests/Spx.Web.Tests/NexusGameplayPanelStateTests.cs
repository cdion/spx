using System.Collections.Immutable;
using Spx.Nexus.Domain;
using Spx.Web.Components.Nexus;
using Xunit;

namespace Spx.Web.Tests;

public sealed class NexusGameplayPanelStateTests
{
    private static readonly Guid CarrierDesignId = Guid.Parse(
        "aaaaaaaa-0000-0000-0000-000000000001"
    );
    private static readonly Guid FighterDesignId = Guid.Parse(
        "aaaaaaaa-0000-0000-0000-000000000002"
    );

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

    [Fact]
    public void ShouldResetUiState_WhenGameIdChangesWithSameRound_ReturnsTrue()
    {
        var nextGameId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var lastGameId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var playerId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var shouldReset = NexusGameplayPanelState.ShouldResetUiState(
            nextGameId,
            sessionCurrentPlayerId: playerId,
            sessionRound: 3,
            lastKnownGameId: lastGameId,
            lastKnownCurrentPlayerId: playerId,
            lastKnownRound: 3
        );

        Assert.True(shouldReset);
    }

    [Fact]
    public void ShouldResetUiState_WhenGameAndRoundUnchanged_ReturnsFalse()
    {
        var gameId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var playerId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var shouldReset = NexusGameplayPanelState.ShouldResetUiState(
            gameId,
            sessionCurrentPlayerId: playerId,
            sessionRound: 4,
            lastKnownGameId: gameId,
            lastKnownCurrentPlayerId: playerId,
            lastKnownRound: 4
        );

        Assert.False(shouldReset);
    }

    [Fact]
    public void ShouldResetUiState_WhenCurrentPlayerChangesWithSameGameAndRound_ReturnsTrue()
    {
        var gameId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var lastPlayerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var nextPlayerId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var shouldReset = NexusGameplayPanelState.ShouldResetUiState(
            gameId,
            sessionCurrentPlayerId: nextPlayerId,
            sessionRound: 4,
            lastKnownGameId: gameId,
            lastKnownCurrentPlayerId: lastPlayerId,
            lastKnownRound: 4
        );

        Assert.True(shouldReset);
    }

    [Fact]
    public void ApplyEventFocusRequest_WhenPreviewRequested_SetsActivePreviewFocus()
    {
        var movementEvent = new NexusUnitsMovedEvent(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            new HexCoord(1, 2),
            new HexCoord(3, 4),
            ImmutableArray<NexusUnitStackGroup>.Empty,
            IsRetreat: false
        );

        var state = NexusGameplayPanelState.ApplyEventFocusRequest(
            EventFocusState.Empty,
            EventFocusRequest.PreviewRequested(movementEvent)
        );

        Assert.Equal(new HexCoord(3, 4), state.Active.Primary);
        Assert.Equal(new HexCoord(1, 2), state.Active.From);
        Assert.Equal(new HexCoord(3, 4), state.Active.To);
        Assert.Equal([new HexCoord(1, 2), new HexCoord(3, 4)], state.Active.Systems);
    }

    [Fact]
    public void ApplyEventFocusRequest_WhenPreviewCleared_ClearsActiveFocus()
    {
        var previewEvent = new NexusIncomeEvent(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Amount: 3,
            [new HexCoord(8, 2), new HexCoord(9, 2)]
        );

        var previewState = NexusGameplayPanelState.ApplyEventFocusRequest(
            EventFocusState.Empty,
            EventFocusRequest.PreviewRequested(previewEvent)
        );
        var clearedState = NexusGameplayPanelState.ApplyEventFocusRequest(
            previewState,
            EventFocusRequest.PreviewCleared()
        );

        Assert.False(clearedState.Preview.HasTarget);
        Assert.False(clearedState.Active.HasTarget);
    }

    [Fact]
    public void ApplyEventFocusRequest_WhenDismissed_ClearsPreviewFocus()
    {
        var previewEvent = new NexusIncomeEvent(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Amount: 3,
            [new HexCoord(8, 2), new HexCoord(9, 2)]
        );

        var previewState = NexusGameplayPanelState.ApplyEventFocusRequest(
            EventFocusState.Empty,
            EventFocusRequest.PreviewRequested(previewEvent)
        );
        var dismissedState = NexusGameplayPanelState.ApplyEventFocusRequest(
            previewState,
            EventFocusRequest.Dismissed()
        );

        Assert.False(dismissedState.Preview.HasTarget);
        Assert.False(dismissedState.Active.HasTarget);
    }

    [Fact]
    public void ApplySelectionRequest_WhenSameSystemSelected_TogglesSelectionOff()
    {
        var system = new HexCoord(2, -1);

        var selectedSystem = NexusGameplayPanelState.ApplySelectionRequest(
            system,
            SelectionRequest.SystemSelected(system)
        );

        Assert.Null(selectedSystem);
    }

    [Fact]
    public void ApplySelectionRequest_WhenSelectionCleared_ReturnsNull()
    {
        var selectedSystem = NexusGameplayPanelState.ApplySelectionRequest(
            new HexCoord(4, 3),
            SelectionRequest.SelectionCleared()
        );

        Assert.Null(selectedSystem);
    }

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
        Assert.Equal(new HexCoord(2, -2), nextState.SelectedSystem);
    }

    [Fact]
    public void SyncSelectedSystemToEventFocus_WhenFocusClearsOwnedSelection_ClearsSystem()
    {
        var selectionState = SelectionState.Empty with
        {
            SelectedSystem = GamePageCoordinatorTestData.MoveTargetCoord,
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
    public void ApplyBuildDraftAdjustment_WhenAddOneRequested_AddsBuildOrder()
    {
        var nextState = NexusGameplayPanelState.ApplyBuildDraftAdjustment(
            OrderDraftState.Empty with
            {
                Designs = [FighterDesign],
            },
            FighterDesignId,
            BuildDraftAdjustmentKind.AddOne,
            projectedEnergy: 20
        );

        var buildOrder = Assert.Single(nextState.PendingBuildOrders);
        Assert.Equal(FighterDesignId, buildOrder.DesignId);
        Assert.Equal(1, buildOrder.Count);
    }

    [Fact]
    public void ApplyPendingOrderRequest_WhenMoveOrderRemoved_RemovesQueuedMove()
    {
        var moveOrder = new NexusMoveOrder(
            GamePageCoordinatorTestData.CurrentPlayerHomeCoord,
            GamePageCoordinatorTestData.MoveTargetCoord,
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
}

using System.Collections.Immutable;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Spx.Web.Components.Nexus;
using Xunit;

namespace Spx.Web.Tests;

public sealed class NexusGameplayPanelInteractionTests : TestContext
{
    [Fact]
    public void FirstEvent_IsNotPinnedOnInitialRender()
    {
        var gameId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var session = GamePageCoordinatorTestData.CreateGameplayPanelSession(
            gameId,
            lastResolveEvents: GamePageCoordinatorTestData.CreateGameplayPanelResolveEvents()
        );

        var cut = RenderComponent<NexusGameplayPanel>(parameters =>
            parameters
                .Add(x => x.Session, session)
                .Add(x => x.Lobby, GamePageCoordinatorTestData.CreateLobby(gameId))
                .Add(x => x.SelectedTab, NexusGameplayTab.Events)
        );

        Assert.Equal(
            "inactive",
            cut.Find(TestIdSelector(NexusGameplayPanelTestIds.ResolveEventRow(0)))
                .GetAttribute("data-focus-state")
        );

        Assert.Contains(
            "Captain Red's System",
            cut.Find(TestIdSelector(NexusGameplayPanelTestIds.ResolveEventRow(0))).TextContent
        );
    }

    [Fact]
    public void HoveringEventRow_ActivatesFocusUntilMouseLeaves()
    {
        var gameId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var session = GamePageCoordinatorTestData.CreateGameplayPanelSession(
            gameId,
            lastResolveEvents: GamePageCoordinatorTestData.CreateGameplayPanelResolveEvents()
        );

        var cut = RenderComponent<NexusGameplayPanel>(parameters =>
            parameters
                .Add(x => x.Session, session)
                .Add(x => x.Lobby, GamePageCoordinatorTestData.CreateLobby(gameId))
                .Add(x => x.SelectedTab, NexusGameplayTab.Events)
        );

        var firstEvent = cut.Find(TestIdSelector(NexusGameplayPanelTestIds.ResolveEventRow(0)));

        firstEvent.TriggerEvent("onmouseenter", new MouseEventArgs());

        Assert.Equal("active", firstEvent.GetAttribute("data-focus-state"));

        firstEvent.TriggerEvent("onmouseleave", new MouseEventArgs());

        Assert.Equal("inactive", firstEvent.GetAttribute("data-focus-state"));
    }

    [Fact]
    public void HoveringSecondEventAndClickingMap_ClearsPreviewHighlight()
    {
        var gameId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var session = GamePageCoordinatorTestData.CreateGameplayPanelSession(
            gameId,
            lastResolveEvents: GamePageCoordinatorTestData.CreateGameplayPanelResolveEvents()
        );

        var cut = RenderComponent<NexusGameplayPanel>(parameters =>
            parameters
                .Add(x => x.Session, session)
                .Add(x => x.Lobby, GamePageCoordinatorTestData.CreateLobby(gameId))
                .Add(x => x.SelectedTab, NexusGameplayTab.Events)
        );

        var secondEvent = cut.Find(TestIdSelector(NexusGameplayPanelTestIds.ResolveEventRow(1)));

        secondEvent.TriggerEvent("onmouseenter", new MouseEventArgs());

        Assert.Equal("active", secondEvent.GetAttribute("data-focus-state"));

        cut.Find(TestIdSelector(NexusGameplayPanelTestIds.MapBackground)).Click();

        Assert.Equal(
            "inactive",
            cut.Find(TestIdSelector(NexusGameplayPanelTestIds.ResolveEventRow(1)))
                .GetAttribute("data-focus-state")
        );
    }

    [Fact]
    public void SelectingSector_FromEventsTab_SwitchesActiveTabToOrders()
    {
        var gameId = Guid.Parse("45454545-4545-4545-4545-454545454545");
        var session = GamePageCoordinatorTestData.CreateGameplayPanelSession(
            gameId,
            lastResolveEvents: GamePageCoordinatorTestData.CreateGameplayPanelResolveEvents()
        );

        var cut = RenderComponent<NexusGameplayPanel>(parameters =>
            parameters
                .Add(x => x.Session, session)
                .Add(x => x.Lobby, GamePageCoordinatorTestData.CreateLobby(gameId))
                .Add(x => x.SelectedTab, NexusGameplayTab.Events)
        );

        Assert.Equal("true", cut.Find("#nexus-sidebar-tab-events").GetAttribute("aria-selected"));
        Assert.Equal("false", cut.Find("#nexus-sidebar-tab-orders").GetAttribute("aria-selected"));

        cut.Find(
                TestIdSelector(
                    NexusGameplayPanelTestIds.System(
                        GamePageCoordinatorTestData.CurrentPlayerHomeCoord
                    )
                )
            )
            .Click();

        Assert.Equal("false", cut.Find("#nexus-sidebar-tab-events").GetAttribute("aria-selected"));
        Assert.Equal("true", cut.Find("#nexus-sidebar-tab-orders").GetAttribute("aria-selected"));
    }

    [Fact]
    public void SelectingFleetAndTarget_QueuesPendingMoveOrder()
    {
        var gameId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var session = GamePageCoordinatorTestData.CreateGameplayPanelSession(gameId);

        var cut = RenderComponent<NexusGameplayPanel>(parameters =>
            parameters
                .Add(x => x.Session, session)
                .Add(x => x.Lobby, GamePageCoordinatorTestData.CreateLobby(gameId))
                .Add(x => x.SelectedTab, NexusGameplayTab.Orders)
        );

        cut.Find(
                TestIdSelector(
                    NexusGameplayPanelTestIds.System(
                        GamePageCoordinatorTestData.CurrentPlayerHomeCoord
                    )
                )
            )
            .Click();

        var carrierButton = cut.Find(
            TestIdSelector(NexusGameplayPanelTestIds.FleetStack("Carrier", 1))
        );

        carrierButton.Click();
        cut.Find(
                TestIdSelector(
                    NexusGameplayPanelTestIds.System(GamePageCoordinatorTestData.MoveTargetCoord)
                )
            )
            .Click();

        var pendingOrder = cut.Find(
            TestIdSelector(
                NexusGameplayPanelTestIds.PendingMoveOrder(
                    0,
                    GamePageCoordinatorTestData.CurrentPlayerHomeCoord,
                    GamePageCoordinatorTestData.MoveTargetCoord
                )
            )
        );

        Assert.Contains("Captain Red's System", pendingOrder.TextContent);
        Assert.Contains("Carrier", pendingOrder.TextContent);
    }

    [Fact]
    public void SelectingWholeFleetAndTarget_QueuesAllAvailableStacks()
    {
        var gameId = Guid.Parse("57575757-5757-5757-5757-575757575757");
        var session = GamePageCoordinatorTestData.CreateGameplayPanelSession(gameId);

        var cut = RenderComponent<NexusGameplayPanel>(parameters =>
            parameters
                .Add(x => x.Session, session)
                .Add(x => x.Lobby, GamePageCoordinatorTestData.CreateLobby(gameId))
                .Add(x => x.SelectedTab, NexusGameplayTab.Orders)
        );

        cut.Find(
                TestIdSelector(
                    NexusGameplayPanelTestIds.System(
                        GamePageCoordinatorTestData.CurrentPlayerHomeCoord
                    )
                )
            )
            .Click();

        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Select All", StringComparison.Ordinal))
            .Click();
        cut.Find(
                TestIdSelector(
                    NexusGameplayPanelTestIds.System(GamePageCoordinatorTestData.MoveTargetCoord)
                )
            )
            .Click();

        var pendingOrder = cut.Find(
            TestIdSelector(
                NexusGameplayPanelTestIds.PendingMoveOrder(
                    0,
                    GamePageCoordinatorTestData.CurrentPlayerHomeCoord,
                    GamePageCoordinatorTestData.MoveTargetCoord
                )
            )
        );

        Assert.Contains("Carrier (1 hits)", pendingOrder.TextContent);
        Assert.Contains("Fighter (1 hits)", pendingOrder.TextContent);
        Assert.Contains("Infantry (1 hits)", pendingOrder.TextContent);
    }

    [Fact]
    public void SelectingDamagedStack_QueuesPendingMoveOrderWithExactHitsBucket()
    {
        var gameId = Guid.Parse("56565656-5656-5656-5656-565656565656");
        var session = GamePageCoordinatorTestData.CreateGameplayPanelSession(gameId);
        var damagedHome = session.Systems.Single(system =>
            system.Coord == GamePageCoordinatorTestData.CurrentPlayerHomeCoord
        );

        session = session with
        {
            Systems =
            [
                .. session.Systems.Select(system =>
                    system.Coord == GamePageCoordinatorTestData.CurrentPlayerHomeCoord
                        ? damagedHome with
                        {
                            UnitStacks = ImmutableDictionary<
                                Guid,
                                ImmutableArray<NexusUnitStackGroup>
                            >.Empty.Add(
                                GamePageCoordinatorTestData.CurrentPlayerId,
                                ImmutableArray.Create(
                                    new NexusUnitStackGroup(
                                        Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
                                        NexusUnitCategory.Capital,
                                        1,
                                        1,
                                        "Carrier"
                                    ),
                                    new NexusUnitStackGroup(
                                        Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
                                        NexusUnitCategory.Capital,
                                        1,
                                        1,
                                        "Carrier"
                                    ),
                                    new NexusUnitStackGroup(
                                        Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"),
                                        NexusUnitCategory.Strike,
                                        1,
                                        1,
                                        "Fighter"
                                    )
                                )
                            ),
                        }
                        : system
                ),
            ],
        };

        var cut = RenderComponent<NexusGameplayPanel>(parameters =>
            parameters
                .Add(x => x.Session, session)
                .Add(x => x.Lobby, GamePageCoordinatorTestData.CreateLobby(gameId))
                .Add(x => x.SelectedTab, NexusGameplayTab.Orders)
        );

        cut.Find(
                TestIdSelector(
                    NexusGameplayPanelTestIds.System(
                        GamePageCoordinatorTestData.CurrentPlayerHomeCoord
                    )
                )
            )
            .Click();

        cut.Find(TestIdSelector(NexusGameplayPanelTestIds.FleetStack("Carrier", 1))).Click();
        cut.Find(
                TestIdSelector(
                    NexusGameplayPanelTestIds.System(GamePageCoordinatorTestData.MoveTargetCoord)
                )
            )
            .Click();

        var pendingOrder = cut.Find(
            TestIdSelector(
                NexusGameplayPanelTestIds.PendingMoveOrder(
                    0,
                    GamePageCoordinatorTestData.CurrentPlayerHomeCoord,
                    GamePageCoordinatorTestData.MoveTargetCoord
                )
            )
        );

        Assert.Contains("1 hits", pendingOrder.TextContent);
        Assert.DoesNotContain("this-string-no-longer-applies", pendingOrder.TextContent);
    }

    [Fact]
    public void GameplayError_HomeSystemPlaceholders_UsePlayerNames()
    {
        var gameId = Guid.Parse("91919191-9191-9191-9191-919191919191");
        var session = GamePageCoordinatorTestData.CreateGameplayPanelSession(gameId);

        var cut = RenderComponent<NexusGameplayPanel>(parameters =>
            parameters
                .Add(x => x.Session, session)
                .Add(x => x.Lobby, GamePageCoordinatorTestData.CreateLobby(gameId))
                .Add(
                    x => x.GameplayError,
                    "Insufficient Fleet Capacity for move from Your Home System to Opponent Home System: need 1, have 0."
                )
        );

        Assert.Contains("Captain Red's System", cut.Markup);
        Assert.Contains("Captain Blue's System", cut.Markup);
        Assert.DoesNotContain("Your Home System", cut.Markup);
        Assert.DoesNotContain("Opponent Home System", cut.Markup);
    }

    [Fact]
    public void RemovingPendingMoveOrder_ClearsOrdersPanel()
    {
        var gameId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var session = GamePageCoordinatorTestData.CreateGameplayPanelSession(gameId);

        var cut = RenderComponent<NexusGameplayPanel>(parameters =>
            parameters
                .Add(x => x.Session, session)
                .Add(x => x.Lobby, GamePageCoordinatorTestData.CreateLobby(gameId))
                .Add(x => x.SelectedTab, NexusGameplayTab.Orders)
        );

        cut.Find(
                TestIdSelector(
                    NexusGameplayPanelTestIds.System(
                        GamePageCoordinatorTestData.CurrentPlayerHomeCoord
                    )
                )
            )
            .Click();

        var carrierButton = cut.Find(
            TestIdSelector(NexusGameplayPanelTestIds.FleetStack("Carrier", 1))
        );

        carrierButton.Click();
        cut.Find(
                TestIdSelector(
                    NexusGameplayPanelTestIds.System(GamePageCoordinatorTestData.MoveTargetCoord)
                )
            )
            .Click();

        cut.Find(
                TestIdSelector(
                    NexusGameplayPanelTestIds.PendingMoveOrderRemove(
                        0,
                        GamePageCoordinatorTestData.CurrentPlayerHomeCoord,
                        GamePageCoordinatorTestData.MoveTargetCoord
                    )
                )
            )
            .Click();

        Assert.Empty(
            cut.FindAll(
                TestIdSelector(
                    NexusGameplayPanelTestIds.PendingMoveOrder(
                        0,
                        GamePageCoordinatorTestData.CurrentPlayerHomeCoord,
                        GamePageCoordinatorTestData.MoveTargetCoord
                    )
                )
            )
        );
    }

    [Fact]
    public void SubmittingOrders_KeepsPendingMoveOrderVisible()
    {
        var gameId = Guid.Parse("76767676-7676-7676-7676-767676767676");
        var session = GamePageCoordinatorTestData.CreateGameplayPanelSession(gameId);

        var cut = RenderComponent<NexusGameplayPanel>(parameters =>
            parameters
                .Add(x => x.Session, session)
                .Add(x => x.Lobby, GamePageCoordinatorTestData.CreateLobby(gameId))
                .Add(x => x.SelectedTab, NexusGameplayTab.Orders)
                .Add(
                    x => x.OnSubmitOrders,
                    EventCallback.Factory.Create<NexusTurnOrdersCommand>(
                        this,
                        _ => Task.CompletedTask
                    )
                )
        );

        cut.Find(
                TestIdSelector(
                    NexusGameplayPanelTestIds.System(
                        GamePageCoordinatorTestData.CurrentPlayerHomeCoord
                    )
                )
            )
            .Click();

        cut.Find(TestIdSelector(NexusGameplayPanelTestIds.FleetStack("Carrier", 1))).Click();
        cut.Find(
                TestIdSelector(
                    NexusGameplayPanelTestIds.System(GamePageCoordinatorTestData.MoveTargetCoord)
                )
            )
            .Click();

        var pendingOrderSelector = TestIdSelector(
            NexusGameplayPanelTestIds.PendingMoveOrder(
                0,
                GamePageCoordinatorTestData.CurrentPlayerHomeCoord,
                GamePageCoordinatorTestData.MoveTargetCoord
            )
        );

        Assert.Single(cut.FindAll(pendingOrderSelector));

        cut.Find(TestIdSelector(NexusGameplayPanelTestIds.SubmitOrdersButton)).Click();

        Assert.Single(cut.FindAll(pendingOrderSelector));
    }

    [Fact]
    public void SwitchingCurrentPlayer_SameGameAndRound_ClearsPendingMoveOrdersFromPreviousView()
    {
        var gameId = Guid.Parse("87878787-8787-8787-8787-878787878787");
        var playerOneSession = GamePageCoordinatorTestData.CreateGameplayPanelSession(gameId);

        var cut = RenderComponent<NexusGameplayPanel>(parameters =>
            parameters
                .Add(x => x.Session, playerOneSession)
                .Add(x => x.Lobby, GamePageCoordinatorTestData.CreateLobby(gameId))
                .Add(x => x.SelectedTab, NexusGameplayTab.Orders)
        );

        cut.Find(
                TestIdSelector(
                    NexusGameplayPanelTestIds.System(
                        GamePageCoordinatorTestData.CurrentPlayerHomeCoord
                    )
                )
            )
            .Click();
        cut.Find(TestIdSelector(NexusGameplayPanelTestIds.FleetStack("Carrier", 1))).Click();
        cut.Find(
                TestIdSelector(
                    NexusGameplayPanelTestIds.System(GamePageCoordinatorTestData.MoveTargetCoord)
                )
            )
            .Click();

        var playerOneOrderSelector = TestIdSelector(
            NexusGameplayPanelTestIds.PendingMoveOrder(
                0,
                GamePageCoordinatorTestData.CurrentPlayerHomeCoord,
                GamePageCoordinatorTestData.MoveTargetCoord
            )
        );

        Assert.Single(cut.FindAll(playerOneOrderSelector));

        var playerTwoSession = playerOneSession with
        {
            CurrentPlayer = playerOneSession.Opponent with
            {
                PendingMoveOrders = [],
                PendingBuildOrders = [],
                PendingBeginNexusGate = false,
            },
            Opponent = playerOneSession.CurrentPlayer with
            {
                PendingMoveOrders = null,
                PendingBuildOrders = null,
                PendingBeginNexusGate = false,
            },
        };

        cut.SetParametersAndRender(parameters =>
            parameters
                .Add(x => x.Session, playerTwoSession)
                .Add(x => x.Lobby, GamePageCoordinatorTestData.CreateLobby(gameId))
                .Add(x => x.SelectedTab, NexusGameplayTab.Orders)
        );

        Assert.Empty(cut.FindAll(playerOneOrderSelector));
    }

    private static string TestIdSelector(string testId) => $"[data-testid='{testId}']";
}

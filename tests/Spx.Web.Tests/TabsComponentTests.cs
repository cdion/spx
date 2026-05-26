using Bunit;
using Microsoft.AspNetCore.Components;
using Spx.Web.Components;
using Xunit;

namespace Spx.Web.Tests;

public sealed class TabsComponentTests : TestContext
{
    [Fact]
    public void ArrowRightSelectsNextEnabledTab()
    {
        const string selected = "Orders";
        string? captured = null;

        var cut = RenderComponent<Tabs<string>>(parameters =>
            parameters
                .Add(x => x.Items, ["Orders", "Events"])
                .Add(x => x.SelectedItem, selected)
                .Add(x => x.ItemLabelSelector, x => x)
                .Add(
                    x => x.OnSelected,
                    EventCallback.Factory.Create<string>(this, value => captured = value)
                )
                .Add(x => x.ItemIdSelector, x => $"tab-{x.ToLowerInvariant()}")
                .Add(x => x.PanelIdSelector, x => $"panel-{x.ToLowerInvariant()}")
        );

        cut.Find("button[role='tab'][aria-selected='true']").KeyDown("ArrowRight");

        Assert.Equal("Events", captured);
    }

    [Fact]
    public void RendersExpectedAriaAttributesForTabs()
    {
        var cut = RenderComponent<Tabs<string>>(parameters =>
            parameters
                .Add(x => x.Items, ["Orders", "Events"])
                .Add(x => x.SelectedItem, "Orders")
                .Add(x => x.ItemLabelSelector, x => x)
                .Add(x => x.ItemIdSelector, x => $"tab-{x.ToLowerInvariant()}")
                .Add(x => x.PanelIdSelector, x => $"panel-{x.ToLowerInvariant()}")
        );

        var selectedTab = cut.Find("#tab-orders");
        var unselectedTab = cut.Find("#tab-events");

        Assert.Equal("tab", selectedTab.GetAttribute("role"));
        Assert.Equal("true", selectedTab.GetAttribute("aria-selected"));
        Assert.Equal("panel-orders", selectedTab.GetAttribute("aria-controls"));
        Assert.Equal("0", selectedTab.GetAttribute("tabindex"));

        Assert.Equal("false", unselectedTab.GetAttribute("aria-selected"));
        Assert.Equal("panel-events", unselectedTab.GetAttribute("aria-controls"));
        Assert.Equal("-1", unselectedTab.GetAttribute("tabindex"));
    }
}

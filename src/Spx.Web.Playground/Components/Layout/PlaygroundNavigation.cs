namespace Spx.Web.Playground.Components.Layout;

internal static class PlaygroundNavigation
{
    public static IReadOnlyList<NavSection> Sections { get; } =
    [
        new NavSection(
            "Shared",
            [
                new NavGroup(
                    "Components",
                    [new NavItem("Timeline", "/stories/shared/components/timeline")]
                ),
            ]
        ),
        new NavSection(
            "Nexus",
            [
                new NavGroup(
                    "Components",
                    [
                        new NavItem("Hex Grid", "/stories/nexus/components/hex-grid"),
                        new NavItem("Gameplay Panel", "/stories/nexus/components/gameplay-panel"),
                        new NavItem("Live Gameplay", "/stories/nexus/components/live-gameplay"),
                        new NavItem("Top Info Bar", "/stories/nexus/components/top-info-bar"),
                        new NavItem(
                            "Selected Hex Panel",
                            "/stories/nexus/components/selected-hex-panel"
                        ),
                        new NavItem("Pending Orders", "/stories/nexus/components/pending-orders"),
                        new NavItem("Hex Cell States", "/stories/nexus/components/hex-cell-states"),
                    ]
                ),
                new NavGroup(
                    "Pages",
                    [
                        new NavItem("Lobby", "/stories/nexus/pages/lobby"),
                        new NavItem("Forms", "/stories/nexus/pages/forms"),
                        new NavItem("Page", "/stories/nexus/pages/page"),
                    ]
                ),
            ]
        ),
        new NavSection(
            "Account",
            [
                new NavGroup(
                    "Pages",
                    [
                        new NavItem("Forms", "/stories/account/forms"),
                        new NavItem("Access", "/stories/account/access"),
                    ]
                ),
            ]
        ),
    ];

    public sealed record NavSection(string Title, IReadOnlyList<NavGroup> Groups);

    public sealed record NavGroup(string Title, IReadOnlyList<NavItem> Items);

    public sealed record NavItem(string Label, string Href);
}

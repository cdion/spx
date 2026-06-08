using System.Reflection;
using Microsoft.AspNetCore.Components;
using Spx.Web.Playground.Components.Shared;

namespace Spx.Web.Playground.Components.Layout;

internal static class PlaygroundNavigation
{
    public static IReadOnlyList<NavSection> Sections { get; } = BuildSections();

    private static List<NavSection> BuildSections() =>
        typeof(PlaygroundNavigation)
            .Assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<StoryAttribute>() is not null)
            .GroupBy(t => t.GetCustomAttribute<StoryAttribute>()!.Section)
            .Select(sectionGroup => new NavSection(
                sectionGroup.Key,
                sectionGroup
                    .GroupBy(t => t.GetCustomAttribute<StoryAttribute>()!.Group)
                    .Select(groupGroup => new NavGroup(
                        groupGroup.Key,
                        groupGroup
                            .Select(t =>
                            {
                                var attr = t.GetCustomAttribute<StoryAttribute>()!;
                                var route = t.GetCustomAttribute<RouteAttribute>()?.Template ?? "/";
                                return new NavItem(attr.Label, "/" + route.TrimStart('/'));
                            })
                            .OrderBy(n => n.Label)
                            .ToList()
                    ))
                    .ToList()
            ))
            .ToList();

    public sealed record NavSection(string Title, IReadOnlyList<NavGroup> Groups);

    public sealed record NavGroup(string Title, IReadOnlyList<NavItem> Items);

    public sealed record NavItem(string Label, string Href);
}

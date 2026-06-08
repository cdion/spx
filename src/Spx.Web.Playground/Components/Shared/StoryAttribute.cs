namespace Spx.Web.Playground.Components.Shared;

/// <summary>
/// Applied to story page components to declare their place in the playground
/// navigation hierarchy. <see cref="PlaygroundNavigation"/> uses reflection
/// to build the sidebar from these attributes, keeping the nav always in sync
/// with the <c>@page</c> routes.
/// </summary>
/// <param name="section">Top-level section (e.g. "Nexus", "Account", "Shared").</param>
/// <param name="group">Group within the section (e.g. "Components", "Pages").</param>
/// <param name="label">Display label in the sidebar.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class StoryAttribute(string section, string group, string label) : Attribute
{
    public string Section { get; } = section;
    public string Group { get; } = group;
    public string Label { get; } = label;
}

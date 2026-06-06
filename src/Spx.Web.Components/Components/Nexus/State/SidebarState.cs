namespace Spx.Web.Components.Nexus;

public sealed record SidebarState(NexusGameplayTab ActiveTab)
{
    public static SidebarState Default { get; } = new(NexusGameplayTab.Orders);
}

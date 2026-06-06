using Spx.Nexus.Domain;

namespace Spx.Web.Components.Nexus;

public sealed record EventFocusState(NexusEventFocus Current)
{
    public static EventFocusState Empty { get; } = new(NexusEventFocus.None);
}

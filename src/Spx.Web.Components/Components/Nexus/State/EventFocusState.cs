using Spx.Nexus.Domain;

namespace Spx.Web.Components.Nexus;

public sealed record EventFocusState(EventFocus Current)
{
    public static EventFocusState Empty { get; } = new(EventFocus.None);
}

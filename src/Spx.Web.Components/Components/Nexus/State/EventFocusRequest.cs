using Spx.Nexus.Domain;

namespace Spx.Web.Components.Nexus;

public enum EventFocusRequestKind
{
    Preview,
    ClearPreview,
    Dismiss,
}

public sealed record EventFocusRequest(
    EventFocusRequestKind Kind,
    NexusResolveEvent? ResolveEvent = null
)
{
    public static EventFocusRequest PreviewRequested(NexusResolveEvent resolveEvent) =>
        new(EventFocusRequestKind.Preview, resolveEvent);

    public static EventFocusRequest PreviewCleared() => new(EventFocusRequestKind.ClearPreview);

    public static EventFocusRequest Dismissed() => new(EventFocusRequestKind.Dismiss);
}

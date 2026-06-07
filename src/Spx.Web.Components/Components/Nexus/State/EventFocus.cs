using Spx.Nexus.Domain;

namespace Spx.Web.Components.Nexus;

public sealed record EventFocus(
    IReadOnlyList<HexCoord> Systems,
    HexCoord? From = null,
    HexCoord? To = null,
    HexCoord? Primary = null
)
{
    public static EventFocus None { get; } = new([]);

    public bool HasTarget => Systems.Count > 0 || From.HasValue || To.HasValue || Primary.HasValue;

    public bool Matches(EventFocus other)
    {
        if (!HasTarget || !other.HasTarget)
            return false;

        return Primary == other.Primary && From == other.From && To == other.To;
    }
}

using Spx.Nexus.Domain;

namespace Spx.Web.Components.Nexus;

public sealed record NexusPlayerContext(
    Guid PlayerId,
    string DisplayName,
    NexusFactionColor Faction,
    HexCoord HomeCoord
);

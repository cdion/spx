using Spx.Nexus.Domain;

namespace Spx.Web.Components.Nexus;

/// <summary>
/// Parameter bag for <see cref="NexusGameplayTopInfoBar"/>.
/// Collates the 6+ related player-display parameters into one init-only model.
/// </summary>
public sealed class NexusPlayerInfoBarModel
{
    public required string PlayerName { get; init; }
    public required NexusFactionColor Faction { get; init; }
    public required int Energy { get; init; }
    public required int SupplyPool { get; init; }
    public required int CapitalCount { get; init; }
    public required bool HasSubmittedOrders { get; init; }
}

namespace Spx.Web.Components.Nexus;

public static class NexusGameplayPanelTestIds
{
    public const string MapBackground = "nexus-map-background";
    public const string NexusGateDraftToggle = "nexus-gate-draft-toggle";
    public const string PendingGateOrder = "nexus-pending-gate-order";
    public const string PendingGateOrderRemove = "nexus-pending-gate-order-remove";
    public const string SubmitOrdersButton = "nexus-submit-orders";
    public const string SelectWholeFleet = "nexus-select-whole-fleet";

    public static string ResolveEventRow(int index) => $"nexus-resolve-event-{index}";

    public static string System(HexCoord coord) => $"nexus-map-system-{CoordToken(coord)}";

    public static string FleetStack(string designName, int remainingHits) =>
        $"nexus-fleet-unit-{NameToken(designName)}-h{remainingHits}";

    public static string BuildUnit(string designName) =>
        $"nexus-build-unit-{NameToken(designName)}";

    public static string PendingMoveOrder(int index, HexCoord from, HexCoord to) =>
        $"nexus-pending-move-order-{index}-{CoordToken(from)}-to-{CoordToken(to)}";

    public static string PendingMoveOrderRemove(int index, HexCoord from, HexCoord to) =>
        $"{PendingMoveOrder(index, from, to)}-remove";

    public static string PendingBuildOrder(string designName) =>
        $"nexus-pending-build-order-{NameToken(designName)}";

    public static string PendingBuildOrderRemove(string designName) =>
        $"{PendingBuildOrder(designName)}-remove";

    private static string CoordToken(HexCoord coord) => $"q{coord.Q}-r{coord.R}";

    private static string NameToken(string name) => name.ToLowerInvariant().Replace(" ", "-");
}

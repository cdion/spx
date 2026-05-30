namespace Spx.Web.Components.Nexus;

public static class NexusGameplayPanelTestIds
{
    public const string MapBackground = "nexus-map-background";
    public const string NexusGateDraftToggle = "nexus-gate-draft-toggle";
    public const string PendingGateOrder = "nexus-pending-gate-order";
    public const string PendingGateOrderRemove = "nexus-pending-gate-order-remove";
    public const string SubmitOrdersButton = "nexus-submit-orders";

    public static string ResolveEventRow(int index) => $"nexus-resolve-event-{index}";

    public static string System(HexCoord coord) => $"nexus-map-system-{CoordToken(coord)}";

    public static string FleetStack(NexusUnitType unitType, int remainingHull) =>
        $"nexus-fleet-unit-{UnitToken(unitType)}-h{remainingHull}";

    public static string BuildUnit(NexusUnitType unitType) =>
        $"nexus-build-unit-{UnitToken(unitType)}";

    public static string PendingMoveOrder(int index, HexCoord from, HexCoord to) =>
        $"nexus-pending-move-order-{index}-{CoordToken(from)}-to-{CoordToken(to)}";

    public static string PendingMoveOrderRemove(int index, HexCoord from, HexCoord to) =>
        $"{PendingMoveOrder(index, from, to)}-remove";

    public static string PendingBuildOrder(NexusUnitType unitType) =>
        $"nexus-pending-build-order-{UnitToken(unitType)}";

    public static string PendingBuildOrderRemove(NexusUnitType unitType) =>
        $"{PendingBuildOrder(unitType)}-remove";

    private static string CoordToken(HexCoord coord) => $"q{coord.Q}-r{coord.R}";

    private static string UnitToken(NexusUnitType unitType) =>
        unitType.ToString().ToLowerInvariant();
}

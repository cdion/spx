using Spx.Nexus.Domain;

namespace Spx.Web.Components.Nexus;

public enum SelectionRequestKind
{
    SelectSystem,
    ClearSelection,
}

public sealed record SelectionRequest(SelectionRequestKind Kind, HexCoord? System = null)
{
    public static SelectionRequest SystemSelected(HexCoord system) =>
        new(SelectionRequestKind.SelectSystem, system);

    public static SelectionRequest SelectionCleared() => new(SelectionRequestKind.ClearSelection);
}

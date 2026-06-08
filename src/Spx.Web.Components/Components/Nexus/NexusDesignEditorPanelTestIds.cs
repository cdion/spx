namespace Spx.Web.Components.Nexus;

public static class NexusDesignEditorPanelTestIds
{
    public const string ModuleTypeSelect = "nexus-design-editor-module-type";
    public const string ModuleCategorySelect = "nexus-design-editor-module-category";
    public const string DesignNameInput = "nexus-design-editor-design-name";
    public const string AddModuleButton = "nexus-design-editor-add-module";
    public const string CreateDesignButton = "nexus-design-editor-create-design";
    public const string SlotCount = "nexus-design-editor-slots";

    public static string HullRadio(NexusUnitCategory hull) =>
        $"nexus-design-editor-hull-{hull.ToString().ToLowerInvariant()}";

    public static string ModuleRow(int index) => $"nexus-design-editor-module-row-{index}";

    public static string ModuleRemoveButton(int index) =>
        $"nexus-design-editor-module-remove-{index}";

    public static string ExistingDesignRow(string designName) =>
        $"nexus-design-editor-design-{NameToken(designName)}";

    public static string ExistingDesignDelete(string designName) =>
        $"{ExistingDesignRow(designName)}-delete";

    private static string NameToken(string name) => name.ToLowerInvariant().Replace(" ", "-");
}

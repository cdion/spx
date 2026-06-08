using System.Collections.Immutable;
using Bunit;
using Xunit;

namespace Spx.Web.Components.Tests;

public sealed class NexusDesignEditorPanelTests : TestContext
{
    private static readonly Guid PlayerId = Guid.Parse("eeeeeeee-0000-0000-0000-000000000001");

    private static readonly ImmutableArray<NexusUnitDesign> EmptyDesigns = [];

    private static readonly ImmutableArray<NexusUnitDesign> SampleDesigns =
    [
        new()
        {
            DesignId = Guid.Parse("eeeeeeee-0000-0000-0000-000000000101"),
            Name = "Interceptor",
            Hull = NexusUnitCategory.Strike,
            Modules = [new Battery(NexusUnitCategory.Strike)],
        },
        new()
        {
            DesignId = Guid.Parse("eeeeeeee-0000-0000-0000-000000000102"),
            Name = "Bomber",
            Hull = NexusUnitCategory.Strike,
            Modules =
            [
                new Vanguard(NexusUnitCategory.Capital),
                new Vanguard(NexusUnitCategory.Planetary),
            ],
        },
    ];

    private static string TestId(string id) => $"[data-testid='{id}']";

    [Fact]
    public void ModuleTypeSelect_Renders_WithAllAvailableOptions()
    {
        var cut = RenderComponent<NexusDesignEditorPanel>(parameters =>
            parameters.Add(x => x.Designs, EmptyDesigns).Add(x => x.PlayerId, PlayerId)
        );

        var select = cut.Find(TestId(NexusDesignEditorPanelTestIds.ModuleTypeSelect));
        Assert.NotNull(select);

        var options = select.QuerySelectorAll("option");
        Assert.NotEmpty(options);
    }

    [Fact]
    public void ModuleTypeSelect_StrikeHull_ShowsCorrectModuleOptions()
    {
        var cut = RenderComponent<NexusDesignEditorPanel>(parameters =>
            parameters.Add(x => x.Designs, EmptyDesigns).Add(x => x.PlayerId, PlayerId)
        );

        var options = cut.Find(TestId(NexusDesignEditorPanelTestIds.ModuleTypeSelect))
            .QuerySelectorAll("option");
        var optionTexts = options.Select(o => o.TextContent).ToList();

        Assert.Contains("Battery", optionTexts);
        Assert.Contains("Vanguard", optionTexts);
        Assert.Contains("Shield", optionTexts);
        Assert.Contains("Disruptor", optionTexts);
        Assert.DoesNotContain("Hangar", optionTexts);
    }

    [Fact]
    public void ChangingModuleType_Updates_SelectedValue()
    {
        var cut = RenderComponent<NexusDesignEditorPanel>(parameters =>
            parameters.Add(x => x.Designs, EmptyDesigns).Add(x => x.PlayerId, PlayerId)
        );

        cut.Find(TestId(NexusDesignEditorPanelTestIds.ModuleTypeSelect)).Change("Shield");

        var changedValue = cut.Find(TestId(NexusDesignEditorPanelTestIds.ModuleTypeSelect))
            .GetAttribute("value");
        Assert.Equal("Shield", changedValue);
    }

    [Fact]
    public void CategorySelect_Appears_WhenModuleNeedsCategory()
    {
        var cut = RenderComponent<NexusDesignEditorPanel>(parameters =>
            parameters.Add(x => x.Designs, EmptyDesigns).Add(x => x.PlayerId, PlayerId)
        );

        var addCategorySelect = cut.FindAll(
            TestId(NexusDesignEditorPanelTestIds.ModuleCategorySelect)
        );
        Assert.Single(addCategorySelect);
    }

    [Fact]
    public void CategorySelect_Disappears_WhenModuleDoesNotNeedCategory()
    {
        var cut = RenderComponent<NexusDesignEditorPanel>(parameters =>
            parameters.Add(x => x.Designs, EmptyDesigns).Add(x => x.PlayerId, PlayerId)
        );

        cut.Find(TestId(NexusDesignEditorPanelTestIds.ModuleTypeSelect)).Change("Shield");

        var addCategorySelect = cut.FindAll(
            TestId(NexusDesignEditorPanelTestIds.ModuleCategorySelect)
        );
        Assert.Empty(addCategorySelect);
    }

    [Fact]
    public void HullRadioButton_ChangingToCapital_IncludesHangarOption()
    {
        var cut = RenderComponent<NexusDesignEditorPanel>(parameters =>
            parameters.Add(x => x.Designs, EmptyDesigns).Add(x => x.PlayerId, PlayerId)
        );

        // Strike hull (default) — Hangar should NOT be available
        var options = cut.Find(TestId(NexusDesignEditorPanelTestIds.ModuleTypeSelect))
            .QuerySelectorAll("option");
        var optionTexts = options.Select(o => o.TextContent).ToList();
        Assert.DoesNotContain("Hangar", optionTexts);

        // Switch to Capital hull via testid
        var capitalRadio = cut.Find(
            TestId(NexusDesignEditorPanelTestIds.HullRadio(NexusUnitCategory.Capital))
        );
        capitalRadio.Change("Capital");

        // After switching to Capital hull, Hangar should appear
        var capitalOptions = cut.Find(TestId(NexusDesignEditorPanelTestIds.ModuleTypeSelect))
            .QuerySelectorAll("option");
        var capitalOptionTexts = capitalOptions.Select(o => o.TextContent).ToList();
        Assert.Contains("Hangar", capitalOptionTexts);
    }

    [Fact]
    public void CreateDesignButton_Disabled_WhenNoName()
    {
        var cut = RenderComponent<NexusDesignEditorPanel>(parameters =>
            parameters.Add(x => x.Designs, EmptyDesigns).Add(x => x.PlayerId, PlayerId)
        );

        var createButton = cut.Find(TestId(NexusDesignEditorPanelTestIds.CreateDesignButton));
        Assert.True(createButton.HasAttribute("disabled"));
    }

    [Fact]
    public void CreateDesignButton_Disabled_WhenNoModules()
    {
        var cut = RenderComponent<NexusDesignEditorPanel>(parameters =>
            parameters.Add(x => x.Designs, EmptyDesigns).Add(x => x.PlayerId, PlayerId)
        );

        var nameInput = cut.Find(TestId(NexusDesignEditorPanelTestIds.DesignNameInput));
        nameInput.Input("Test Design");

        var createButton = cut.Find(TestId(NexusDesignEditorPanelTestIds.CreateDesignButton));
        Assert.True(createButton.HasAttribute("disabled"));
    }

    [Fact]
    public void Description_WhenModuleTypeChanges_UpdatesText()
    {
        var cut = RenderComponent<NexusDesignEditorPanel>(parameters =>
            parameters.Add(x => x.Designs, EmptyDesigns).Add(x => x.PlayerId, PlayerId)
        );

        cut.Find(TestId(NexusDesignEditorPanelTestIds.ModuleTypeSelect)).Change("Shield");

        Assert.Contains("Absorbs", cut.Markup);

        cut.Find(TestId(NexusDesignEditorPanelTestIds.ModuleTypeSelect)).Change("Battery");

        Assert.Contains("Battle phase", cut.Markup);
    }

    [Fact]
    public void ExistingDesigns_Provided_AreDisplayed()
    {
        var cut = RenderComponent<NexusDesignEditorPanel>(parameters =>
            parameters.Add(x => x.Designs, SampleDesigns).Add(x => x.PlayerId, PlayerId)
        );

        Assert.Contains("Interceptor", cut.Markup);
        Assert.Contains("Bomber", cut.Markup);
    }

    [Fact]
    public void AddingModule_ThenRemoving_UpdatesModuleList()
    {
        var cut = RenderComponent<NexusDesignEditorPanel>(parameters =>
            parameters.Add(x => x.Designs, EmptyDesigns).Add(x => x.PlayerId, PlayerId)
        );

        cut.Find(TestId(NexusDesignEditorPanelTestIds.AddModuleButton)).Click();

        var removeButtons = cut.FindAll(
            TestId(NexusDesignEditorPanelTestIds.ModuleRemoveButton(0))
        );
        Assert.Single(removeButtons);

        removeButtons[0].Click();
    }
}

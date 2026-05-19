using Spx.Contracts;
using Spx.Game.Domain;
using Xunit;

namespace Spx.Game.Domain.Tests;

public sealed class GameCardCatalogTests
{
    [Fact]
    public void IsPlayable_returns_true_for_actions_and_effects_only()
    {
        Assert.True(GameCardCatalog.IsPlayable(GameCardDefinition.Extract));
        Assert.True(GameCardCatalog.IsPlayable(GameCardDefinition.Corrupt));
        Assert.False(GameCardCatalog.IsPlayable(GameCardDefinition.Red));
        Assert.False(GameCardCatalog.IsPlayable(GameCardDefinition.Victory));
    }

    [Fact]
    public void GetInitiativeWeight_matches_spec_weights()
    {
        Assert.Equal(1, GameCardCatalog.GetInitiativeWeight(GameCardDefinition.Extract));
        Assert.Equal(1, GameCardCatalog.GetInitiativeWeight(GameCardDefinition.Blue));
        Assert.Equal(2, GameCardCatalog.GetInitiativeWeight(GameCardDefinition.Green));
        Assert.Equal(3, GameCardCatalog.GetInitiativeWeight(GameCardDefinition.Scout));
    }

    [Fact]
    public void GetCategory_distinguishes_victory_from_regular_resources()
    {
        Assert.Equal(
            GameCardCategory.Resource,
            GameCardCatalog.GetCategory(GameCardDefinition.Purple)
        );
        Assert.Equal(
            GameCardCategory.Victory,
            GameCardCatalog.GetCategory(GameCardDefinition.Victory)
        );
    }

    [Fact]
    public void TryGetRefineResult_rejects_non_base_inputs()
    {
        var succeeded = GameCraftingRules.TryGetRefineResult(
            [GameCardDefinition.Orange, GameCardDefinition.Blue],
            out _
        );

        Assert.False(succeeded);
    }

    [Fact]
    public void CanAddProduceInput_enforces_recipe_membership_and_counts()
    {
        Assert.True(
            GameCraftingRules.CanAddProduceInput(
                GameCardDefinition.Sabotage,
                [],
                GameCardDefinition.Red
            )
        );
        Assert.False(
            GameCraftingRules.CanAddProduceInput(
                GameCardDefinition.Sabotage,
                [GameCardDefinition.Red],
                GameCardDefinition.Red
            )
        );
        Assert.False(
            GameCraftingRules.CanAddProduceInput(
                GameCardDefinition.Sabotage,
                [],
                GameCardDefinition.Blue
            )
        );
    }
}

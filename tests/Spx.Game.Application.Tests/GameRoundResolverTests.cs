using Spx.Contracts;
using Xunit;

namespace Spx.Game.Application.Tests;

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
        Assert.Equal(GameCardCategory.Resource, GameCardCatalog.GetCategory(GameCardDefinition.Purple));
        Assert.Equal(GameCardCategory.Victory, GameCardCatalog.GetCategory(GameCardDefinition.Victory));
    }
}
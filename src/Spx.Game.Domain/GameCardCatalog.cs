namespace Spx.Game.Domain;

public static class GameCardCatalog
{
    public const int MarketSize = 5;

    public const int MaxBatchSize = 3;

    public static string GetDisplayName(GameCardDefinition definition)
        => definition.ToString();

    public static GameCardCategory GetCategory(GameCardDefinition definition)
        => definition switch
        {
            GameCardDefinition.Extract or GameCardDefinition.Refine or GameCardDefinition.Produce => GameCardCategory.Action,
            GameCardDefinition.Red or GameCardDefinition.Yellow or GameCardDefinition.Blue
                or GameCardDefinition.Purple or GameCardDefinition.Green or GameCardDefinition.Orange => GameCardCategory.Resource,
            GameCardDefinition.Victory => GameCardCategory.Victory,
            _ => GameCardCategory.Effect
        };

    public static GameResourceColor? GetResourceColor(GameCardDefinition definition)
        => definition switch
        {
            GameCardDefinition.Red => GameResourceColor.Red,
            GameCardDefinition.Yellow => GameResourceColor.Yellow,
            GameCardDefinition.Blue => GameResourceColor.Blue,
            GameCardDefinition.Purple => GameResourceColor.Purple,
            GameCardDefinition.Green => GameResourceColor.Green,
            GameCardDefinition.Orange => GameResourceColor.Orange,
            _ => null
        };

    public static bool IsMarketCard(GameCardDefinition definition)
        => definition is GameCardDefinition.Extract or GameCardDefinition.Refine or GameCardDefinition.Produce;

    public static bool IsPlayable(GameCardDefinition definition)
        => GetCategory(definition) is GameCardCategory.Action or GameCardCategory.Effect;

    public static bool IsBaseResource(GameCardDefinition definition)
        => definition is GameCardDefinition.Red or GameCardDefinition.Yellow or GameCardDefinition.Blue;

    public static bool IsRefinedResource(GameCardDefinition definition)
        => definition is GameCardDefinition.Purple or GameCardDefinition.Green or GameCardDefinition.Orange;

    public static int GetInitiativeWeight(GameCardDefinition definition)
        => GetCategory(definition) switch
        {
            GameCardCategory.Action => 1,
            GameCardCategory.Resource when IsBaseResource(definition) => 1,
            GameCardCategory.Resource => 2,
            GameCardCategory.Effect => 3,
            _ => 4
        };

    public static int GetResolutionStep(GameCardDefinition definition)
        => definition switch
        {
            GameCardDefinition.Extract => 1,
            GameCardDefinition.Refine => 2,
            GameCardDefinition.Produce => 3,
            GameCardDefinition.Sabotage or GameCardDefinition.Replicate or GameCardDefinition.Catalyst or GameCardDefinition.Corrupt or GameCardDefinition.Reclaim or GameCardDefinition.Scout => 0,
            _ => int.MaxValue
        };

    public static bool TryGetBaseDefinition(GameResourceColor color, out GameCardDefinition definition)
    {
        definition = color switch
        {
            GameResourceColor.Red => GameCardDefinition.Red,
            GameResourceColor.Yellow => GameCardDefinition.Yellow,
            GameResourceColor.Blue => GameCardDefinition.Blue,
            _ => default
        };

        return definition is GameCardDefinition.Red or GameCardDefinition.Yellow or GameCardDefinition.Blue;
    }

    public static GameCardDefinition? TryGetRefineOutput(GameCardDefinition first, GameCardDefinition second)
    {
        var pair = new[] { first, second }.OrderBy(value => value).ToArray();
        return (pair[0], pair[1]) switch
        {
            (GameCardDefinition.Red, GameCardDefinition.Blue) => GameCardDefinition.Purple,
            (GameCardDefinition.Blue, GameCardDefinition.Yellow) => GameCardDefinition.Green,
            (GameCardDefinition.Red, GameCardDefinition.Yellow) => GameCardDefinition.Orange,
            _ => null
        };
    }

    public static bool TryGetProduceRecipe(GameCardDefinition definition, out GameCardDefinition[] recipe)
    {
        recipe = definition switch
        {
            GameCardDefinition.Sabotage => [GameCardDefinition.Red, GameCardDefinition.Yellow],
            GameCardDefinition.Replicate => [GameCardDefinition.Blue, GameCardDefinition.Yellow],
            GameCardDefinition.Catalyst => [GameCardDefinition.Red, GameCardDefinition.Blue],
            GameCardDefinition.Corrupt => [GameCardDefinition.Orange, GameCardDefinition.Blue],
            GameCardDefinition.Reclaim => [GameCardDefinition.Green, GameCardDefinition.Red],
            GameCardDefinition.Scout => [GameCardDefinition.Purple, GameCardDefinition.Yellow],
            GameCardDefinition.Victory => [GameCardDefinition.Red, GameCardDefinition.Yellow, GameCardDefinition.Blue, GameCardDefinition.Purple, GameCardDefinition.Green, GameCardDefinition.Orange],
            _ => []
        };

        return recipe.Length > 0;
    }

    public static bool MatchesRecipe(IReadOnlyCollection<GameCardDefinition> consumedCards, IReadOnlyCollection<GameCardDefinition> recipe)
        => consumedCards.OrderBy(definition => definition).SequenceEqual(recipe.OrderBy(definition => definition));
}
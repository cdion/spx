namespace Spx.Game.Domain;

public static class GameCraftingRules
{
    public static bool IsValidRefineInput(GameCardDefinition definition)
        => GameCardCatalog.IsBaseResource(definition);

    public static bool TryGetRefineResult(IReadOnlyCollection<GameCardDefinition> consumedDefinitions, out GameCardDefinition output)
    {
        output = default;

        if (consumedDefinitions.Count != 2)
        {
            return false;
        }

        var definitions = consumedDefinitions.ToArray();
        if (definitions.Any(definition => !IsValidRefineInput(definition)))
        {
            return false;
        }

        if (GameCardCatalog.TryGetRefineOutput(definitions[0], definitions[1]) is not { } refineOutput)
        {
            return false;
        }

        output = refineOutput;
        return true;
    }

    public static bool IsValidProduceInput(GameCardDefinition craftedDefinition, GameCardDefinition candidateInput)
        => TryGetProduceRecipe(craftedDefinition, out var recipe)
            && recipe.Contains(candidateInput);

    public static bool CanAddProduceInput(
        GameCardDefinition craftedDefinition,
        IReadOnlyCollection<GameCardDefinition> selectedInputs,
        GameCardDefinition candidateInput)
    {
        if (!TryGetProduceRecipe(craftedDefinition, out var recipe)
            || selectedInputs.Count >= recipe.Length
            || !recipe.Contains(candidateInput))
        {
            return false;
        }

        return selectedInputs.Count(definition => definition == candidateInput)
            < recipe.Count(definition => definition == candidateInput);
    }

    public static bool TryGetProduceRecipe(GameCardDefinition craftedDefinition, out GameCardDefinition[] recipe)
        => GameCardCatalog.TryGetProduceRecipe(craftedDefinition, out recipe);

    public static bool TryGetProduceResult(GameCardDefinition? craftedDefinition, out GameCardDefinition output)
    {
        output = default;
        return craftedDefinition is { } selectedDefinition
            && TryGetProduceRecipe(selectedDefinition, out _)
            && (output = selectedDefinition) == selectedDefinition;
    }

    public static bool MatchesProduceRecipe(GameCardDefinition craftedDefinition, IReadOnlyCollection<GameCardDefinition> consumedDefinitions)
        => TryGetProduceRecipe(craftedDefinition, out var recipe)
            && GameCardCatalog.MatchesRecipe(consumedDefinitions, recipe);
}
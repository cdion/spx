using Spx.Game.Domain;

namespace Spx.Web.Components;

internal static class GameCardStyles
{
    public static string GetCardColorClass(GameCardDefinition definition) =>
        definition switch
        {
            GameCardDefinition.Red => "ui-game-card-red",
            GameCardDefinition.Yellow => "ui-game-card-amber",
            GameCardDefinition.Blue => "ui-game-card-sky",
            GameCardDefinition.Purple => "ui-game-card-violet",
            GameCardDefinition.Green => "ui-game-card-emerald",
            GameCardDefinition.Orange => "ui-game-card-orange",
            GameCardDefinition.Sabotage
            or GameCardDefinition.Replicate
            or GameCardDefinition.Catalyst
            or GameCardDefinition.Corrupt
            or GameCardDefinition.Reclaim
            or GameCardDefinition.Scout => "ui-game-card-effect",
            GameCardDefinition.Victory => "ui-game-card-victory",
            _ => "ui-game-card-action",
        };

    public static string GetResourceDotColorClass(GameResourceColor color) =>
        color switch
        {
            GameResourceColor.Red => "bg-red-500",
            GameResourceColor.Yellow => "bg-amber-400",
            GameResourceColor.Blue => "bg-sky-500",
            GameResourceColor.Purple => "bg-violet-500",
            GameResourceColor.Green => "bg-emerald-500",
            GameResourceColor.Orange => "bg-orange-500",
            _ => "bg-slate-500",
        };
}

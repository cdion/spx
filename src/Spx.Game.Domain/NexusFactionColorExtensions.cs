namespace Spx.Game.Domain;

public static class NexusFactionColorExtensions
{
    public static NexusColonyColor ToColonyColor(this NexusFactionColor faction) =>
        faction switch
        {
            NexusFactionColor.Red => NexusColonyColor.Red,
            NexusFactionColor.Blue => NexusColonyColor.Blue,
            NexusFactionColor.Green => NexusColonyColor.Green,
            NexusFactionColor.Yellow => NexusColonyColor.Yellow,
            _ => throw new ArgumentOutOfRangeException(nameof(faction), faction, null),
        };
}

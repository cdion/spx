using Orleans;
using Spx.Game.Domain;

namespace Spx.Grains;

[GenerateSerializer]
public sealed class NexusGameSessionGrainState
{
    [Id(0)]
    public NexusGameState Game { get; set; } = new();
}

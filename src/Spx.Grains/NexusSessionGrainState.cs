using Orleans;
using Spx.Nexus.Domain;

namespace Spx.Grains;

[GenerateSerializer]
public sealed class NexusSessionGrainState
{
    [Id(0)]
    public NexusState Game { get; set; } = new();
}

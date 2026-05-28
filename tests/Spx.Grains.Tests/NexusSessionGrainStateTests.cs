using System.Collections.Immutable;
using Spx.Grains;
using Xunit;

namespace Spx.Grains.Tests;

public sealed class NexusSessionGrainStateTests
{
    [Fact]
    public void Default_state_has_uninitialized_game()
    {
        var grainState = new NexusSessionGrainState();

        Assert.NotNull(grainState.Game);
        Assert.Empty(grainState.Game.Players);
    }

    [Fact]
    public void Initialize_sets_players_on_embedded_game_state()
    {
        var grainState = new NexusSessionGrainState();
        var first = new NexusSessionPlayer(Guid.NewGuid());
        var second = new NexusSessionPlayer(Guid.NewGuid());

        NexusEngine.Initialize(
            grainState.Game,
            new InitializeNexusGameCommand(ImmutableArray.Create(first, second)),
            Random.Shared
        );

        Assert.Equal(2, grainState.Game.Players.Count);
        Assert.Equal(1, grainState.Game.RoundNumber);
        Assert.Equal(
            14,
            grainState.Game.Systems.Sum(s =>
                s.Units.Values.Sum(stacks => stacks.Sum(st => st.Count))
            )
        );
    }

    [Fact]
    public void Initialize_assigns_each_participant_a_distinct_faction()
    {
        var grainState = new NexusSessionGrainState();
        var first = new NexusSessionPlayer(Guid.NewGuid());
        var second = new NexusSessionPlayer(Guid.NewGuid());

        NexusEngine.Initialize(
            grainState.Game,
            new InitializeNexusGameCommand(ImmutableArray.Create(first, second)),
            Random.Shared
        );

        Assert.Equal(2, grainState.Game.Players.Count);
        Assert.NotEqual(grainState.Game.Players[0].Faction, grainState.Game.Players[1].Faction);
        var playerIds = grainState.Game.Players.Select(p => p.PlayerId).ToHashSet();
        Assert.Contains(first.PlayerId, playerIds);
        Assert.Contains(second.PlayerId, playerIds);
    }
}

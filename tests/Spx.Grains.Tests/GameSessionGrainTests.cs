using Spx.Grains;
using Xunit;

namespace Spx.Grains.Tests;

public sealed class NexusGameSessionGrainStateTests
{
    [Fact]
    public void Default_state_has_uninitialized_game()
    {
        var grainState = new NexusGameSessionGrainState();

        Assert.NotNull(grainState.Game);
        Assert.Null(grainState.Game.RedPlayer);
        Assert.Null(grainState.Game.BluePlayer);
    }

    [Fact]
    public void Initialize_sets_players_on_embedded_game_state()
    {
        var grainState = new NexusGameSessionGrainState();
        var first = new GameSessionParticipant(Guid.NewGuid());
        var second = new GameSessionParticipant(Guid.NewGuid());

        NexusGameEngine.Initialize(grainState.Game, new InitializeNexusGameCommand(first, second));

        Assert.NotNull(grainState.Game.RedPlayer);
        Assert.NotNull(grainState.Game.BluePlayer);
        Assert.Equal(1, grainState.Game.RoundNumber);
        Assert.Equal(NexusGamePhase.Planning, grainState.Game.Phase);
        Assert.Equal(4, grainState.Game.Hexes.Sum(h => h.RedFleets + h.BlueFleets));
    }

    [Fact]
    public void Initialize_assigns_each_participant_a_distinct_faction()
    {
        var grainState = new NexusGameSessionGrainState();
        var first = new GameSessionParticipant(Guid.NewGuid());
        var second = new GameSessionParticipant(Guid.NewGuid());

        NexusGameEngine.Initialize(grainState.Game, new InitializeNexusGameCommand(first, second));

        Assert.NotEqual(grainState.Game.RedPlayer!.Faction, grainState.Game.BluePlayer!.Faction);
        var playerIds = new[]
        {
            grainState.Game.RedPlayer.PlayerId,
            grainState.Game.BluePlayer.PlayerId,
        };
        Assert.Contains(first.PlayerId, playerIds);
        Assert.Contains(second.PlayerId, playerIds);
    }
}

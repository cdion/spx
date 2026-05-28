using Spx.Web.Components.Nexus;
using Xunit;

namespace Spx.Web.Tests;

public sealed class NexusGameplayPanelStateTests
{
    [Fact]
    public void ShouldResetUiState_WhenGameIdChangesWithSameRound_ReturnsTrue()
    {
        var nextGameId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var lastGameId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var shouldReset = NexusGameplayPanelState.ShouldResetUiState(
            nextGameId,
            sessionRound: 3,
            lastKnownGameId: lastGameId,
            lastKnownRound: 3
        );

        Assert.True(shouldReset);
    }

    [Fact]
    public void ShouldResetUiState_WhenGameAndRoundUnchanged_ReturnsFalse()
    {
        var gameId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var shouldReset = NexusGameplayPanelState.ShouldResetUiState(
            gameId,
            sessionRound: 4,
            lastKnownGameId: gameId,
            lastKnownRound: 4
        );

        Assert.False(shouldReset);
    }
}

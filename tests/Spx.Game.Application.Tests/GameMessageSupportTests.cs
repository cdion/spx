using Spx.Game.Application;
using Xunit;

namespace Spx.Game.Application.Tests;

public sealed class GameMessageSupportTests
{
    [Theory]
    [InlineData(0, 20)]
    [InlineData(-3, 20)]
    [InlineData(5, 5)]
    [InlineData(99, 20)]
    public void NormalizeTake_applies_default_and_bounds(int requestedTake, int expectedTake)
    {
        Assert.Equal(expectedTake, GameMessageSupport.NormalizeTake(requestedTake));
    }

    [Fact]
    public void TryNormalizeMessageBody_trims_and_normalizes_line_endings()
    {
        var succeeded = GameMessageSupport.TryNormalizeMessageBody(
            "  Hello\r\ncrew  ",
            out var body,
            out var errorMessage
        );

        Assert.True(succeeded);
        Assert.Equal("Hello\ncrew", body);
        Assert.Equal(string.Empty, errorMessage);
    }

    [Fact]
    public void TryNormalizeMessageBody_rejects_messages_that_are_too_long()
    {
        var succeeded = GameMessageSupport.TryNormalizeMessageBody(
            new string('a', 1025),
            out _,
            out var errorMessage
        );

        Assert.False(succeeded);
        Assert.Equal("Messages must be 1024 characters or fewer.", errorMessage);
    }

    [Fact]
    public void MapMessage_clears_deleted_body_and_disables_mutation_flags()
    {
        var playerId = Guid.NewGuid();
        var snapshot = new GameMessageSupport.GameMessageSnapshot(
            Guid.NewGuid(),
            GameMessageKind.PlayerPublic,
            GameMessageSenderKind.Player,
            playerId,
            "Captain Red",
            null,
            string.Empty,
            "Original",
            DateTime.UtcNow.AddMinutes(-1),
            null,
            DateTime.UtcNow
        );

        var message = GameMessageSupport.MapMessage(
            snapshot,
            playerId,
            canMutate: true,
            DateTime.UtcNow
        );

        Assert.Equal(string.Empty, message.Body);
        Assert.False(message.CanEdit);
        Assert.False(message.CanDelete);
    }

    [Fact]
    public void MapMessage_disables_mutation_outside_mutation_window()
    {
        var playerId = Guid.NewGuid();
        var createdAtUtc = DateTime
            .UtcNow.Subtract(GameMessageSupport.MessageMutationWindow)
            .AddSeconds(-1);
        var snapshot = new GameMessageSupport.GameMessageSnapshot(
            Guid.NewGuid(),
            GameMessageKind.PlayerPrivate,
            GameMessageSenderKind.Player,
            playerId,
            "Captain Red",
            Guid.NewGuid(),
            "Captain Blue",
            "Secret",
            createdAtUtc,
            null,
            null
        );

        var message = GameMessageSupport.MapMessage(
            snapshot,
            playerId,
            canMutate: true,
            DateTime.UtcNow
        );

        Assert.True(message.IsPrivate);
        Assert.False(message.CanEdit);
        Assert.False(message.CanDelete);
    }
}

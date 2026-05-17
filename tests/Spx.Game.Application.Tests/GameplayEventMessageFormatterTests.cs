using Spx.Contracts;
using Spx.Game.Application;
using Spx.Game.Domain;
using Xunit;

namespace Spx.Game.Application.Tests;

public sealed class GameplayEventMessageFormatterTests
{
    [Fact]
    public void CreateMessageBodies_formats_round_summary_events_and_completion()
    {
        var formatter = new GameplayEventMessageFormatter();
        var firstPlayer = new GameSessionParticipant(Guid.Parse("85b56bc8-bd95-48f5-8374-f53714734717"), "user-1");
        var secondPlayer = new GameSessionParticipant(Guid.Parse("6421fe5a-5585-4db9-b48b-e6caf8323b8f"), "user-2");
        var resolvedAtUtc = DateTime.UtcNow;
        var session = new GameSessionSnapshot(
            Guid.NewGuid(),
            4,
            GamePhase.Completed,
            new GamePlayerSnapshot(firstPlayer, [], false, 0, 0, false, false, []),
            new GamePlayerSnapshot(secondPlayer, [], false, 0, 0, false, false, []),
            [],
            0,
            false,
            false,
            false,
            GameCardCatalog.MaxBatchSize,
            new GameResolvedBatchSnapshot(
                4,
                [
                    new GameResolvedPlayerBatchSnapshot(firstPlayer, [CreatePlayedCardView(GameCardDefinition.Produce)], true),
                    new GameResolvedPlayerBatchSnapshot(secondPlayer, [], false)
                ],
                resolvedAtUtc),
            new GameCompletionSnapshot(GameCompletionReason.Victory, firstPlayer, resolvedAtUtc));
        var gameplayEvents = new GameplayEvent[]
        {
            new(GameplayEventKind.CreatedCard, "user-1", GameCardDefinition.Produce, null, null, GameCardDefinition.Victory),
            new(GameplayEventKind.Fizzled, "user-2", GameCardDefinition.Sabotage, null, null, null)
        };
        var playerNames = new Dictionary<Guid, string>
        {
            [firstPlayer.PlayerId] = "Captain Red",
            [secondPlayer.PlayerId] = "Captain Blue"
        };

        var messages = formatter.CreateMessageBodies(session, gameplayEvents, playerNames);

        Assert.Equal(2, messages.Count);
        Assert.Contains("Round 4 resolved.", messages[0], StringComparison.Ordinal);
        Assert.Contains("Captain Red played Produce.", messages[0], StringComparison.Ordinal);
        Assert.Contains("Captain Blue passed.", messages[0], StringComparison.Ordinal);
        Assert.Contains("Captain Red produced Victory.", messages[0], StringComparison.Ordinal);
        Assert.Contains("Captain Blue's Sabotage fizzled.", messages[0], StringComparison.Ordinal);
        Assert.Equal("Captain Red won by producing Victory.", messages[1]);
    }

    private static GameBatchCardSnapshot CreatePlayedCardView(GameCardDefinition definition)
        => new(
            new GameCardSnapshot(Guid.NewGuid(), definition, definition.ToString(), GameCardCatalog.GetCategory(definition), GameCardCatalog.GetResourceColor(definition)),
            null,
            null,
            null,
            null,
            []);
}
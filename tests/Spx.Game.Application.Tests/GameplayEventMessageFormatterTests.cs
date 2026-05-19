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
        var firstPlayer = new GameSessionParticipant(
            Guid.Parse("85b56bc8-bd95-48f5-8374-f53714734717")
        );
        var secondPlayer = new GameSessionParticipant(
            Guid.Parse("6421fe5a-5585-4db9-b48b-e6caf8323b8f")
        );
        var resolvedAtUtc = DateTime.UtcNow;
        var session = new GameSessionView(
            Guid.NewGuid(),
            4,
            GamePhase.Completed,
            new GamePlayerStateView(firstPlayer, [], false, 0, 0, false, false, []),
            new GamePlayerStateView(secondPlayer, [], false, 0, 0, false, false, []),
            [],
            0,
            false,
            false,
            false,
            GameCardCatalog.MaxBatchSize,
            new GameResolvedBatchView(
                4,
                [
                    new GameResolvedPlayerBatchView(
                        firstPlayer,
                        [CreatePlayedCardView(GameCardDefinition.Produce)],
                        true
                    ),
                    new GameResolvedPlayerBatchView(secondPlayer, [], false),
                ],
                resolvedAtUtc
            ),
            new GameCompletionView(GameCompletionReason.Victory, firstPlayer, resolvedAtUtc)
        );
        var gameplayEvents = new GameplayEvent[]
        {
            new(
                GameplayEventKind.CreatedCard,
                firstPlayer.PlayerId,
                GameCardDefinition.Produce,
                null,
                null,
                GameCardDefinition.Victory
            ),
            new(
                GameplayEventKind.Fizzled,
                secondPlayer.PlayerId,
                GameCardDefinition.Sabotage,
                null,
                null,
                null
            ),
        };
        var playerNames = new Dictionary<Guid, string>
        {
            [firstPlayer.PlayerId] = "Captain Red",
            [secondPlayer.PlayerId] = "Captain Blue",
        };

        var messages = formatter.CreateMessageBodies(
            session.LastResolvedBatch,
            session.Completion,
            gameplayEvents,
            playerNames
        );

        Assert.Equal(2, messages.Count);
        Assert.Contains("Round 4 resolved.", messages[0], StringComparison.Ordinal);
        Assert.Contains("Captain Red played Produce.", messages[0], StringComparison.Ordinal);
        Assert.Contains("Captain Blue passed.", messages[0], StringComparison.Ordinal);
        Assert.Contains("Captain Red produced Victory.", messages[0], StringComparison.Ordinal);
        Assert.Contains("Captain Blue's Sabotage fizzled.", messages[0], StringComparison.Ordinal);
        Assert.Equal("Captain Red won by producing Victory.", messages[1]);
    }

    private static GameBatchCardView CreatePlayedCardView(GameCardDefinition definition) =>
        new(
            new GameCardView(
                Guid.NewGuid(),
                definition,
                definition.ToString(),
                GameCardCatalog.GetCategory(definition),
                GameCardCatalog.GetResourceColor(definition)
            ),
            null,
            null,
            null,
            null,
            []
        );
}

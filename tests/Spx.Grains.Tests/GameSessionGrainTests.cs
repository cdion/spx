using Spx.Game.Domain;
using Spx.Grains;
using Xunit;

namespace Spx.Grains.Tests;

public sealed class GameSessionGrainTests
{
    [Fact]
    public void FromDomainState_preserves_round_state_and_completion()
    {
        var firstPlayer = new GameSessionParticipant(Guid.NewGuid());
        var secondPlayer = new GameSessionParticipant(Guid.NewGuid());
        var domainState = new GameSessionState
        {
            FirstPlayer = firstPlayer,
            SecondPlayer = secondPlayer,
            RoundNumber = 4,
            Phase = GamePhase.Completed,
            FirstPlayerActive = true,
            SecondPlayerActive = false,
            CurrentAcquireFirstPlayerId = firstPlayer.PlayerId,
            CurrentAcquireSecondPlayerId = secondPlayer.PlayerId,
            FirstPlayerHand =
            [
                new GameCardState
                {
                    CardInstanceId = Guid.NewGuid(),
                    Definition = GameCardDefinition.Extract,
                },
            ],
            LastResolvedBatch = new ResolvedGameBatchState
            {
                RoundNumber = 3,
                ResolvedAtUtc = DateTime.UtcNow,
                Players =
                [
                    new ResolvedGamePlayerBatchState
                    {
                        PlayerId = firstPlayer.PlayerId,
                        Cards =
                        [
                            new PendingGameBatchCardState
                            {
                                Card = new GameCardState
                                {
                                    CardInstanceId = Guid.NewGuid(),
                                    Definition = GameCardDefinition.Refine,
                                },
                                ConsumedCards =
                                [
                                    new GameCardReferenceState
                                    {
                                        ProducedCardDefinition = GameCardDefinition.Red,
                                    },
                                ],
                            },
                        ],
                        ProducedVictory = true,
                    },
                ],
            },
            Completion = new GameCompletionState
            {
                Reason = GameCompletionReason.Victory,
                WinnerPlayerId = firstPlayer.PlayerId,
                CompletedAtUtc = DateTime.UtcNow,
            },
        };

        var grainState = GameSessionGrainStateMapper.FromDomainState(domainState);

        Assert.Equal(domainState.RoundNumber, grainState.RoundNumber);
        Assert.Equal(domainState.Phase, grainState.Phase);
        Assert.NotNull(grainState.FirstPlayer);
        Assert.Equal(domainState.FirstPlayer!.PlayerId, grainState.FirstPlayer!.PlayerId);
        Assert.NotNull(grainState.SecondPlayer);
        Assert.Equal(domainState.SecondPlayer!.PlayerId, grainState.SecondPlayer!.PlayerId);
        Assert.Single(grainState.FirstPlayerHand);
        Assert.Equal(GameCardDefinition.Extract, grainState.FirstPlayerHand[0].Definition);
        Assert.NotNull(grainState.LastResolvedBatch);
        Assert.Single(grainState.LastResolvedBatch!.Players);
        Assert.Equal(firstPlayer.PlayerId, grainState.LastResolvedBatch.Players[0].PlayerId);
        Assert.True(grainState.LastResolvedBatch.Players[0].ProducedVictory);
        Assert.NotNull(grainState.Completion);
        Assert.Equal(GameCompletionReason.Victory, grainState.Completion!.Reason);
        Assert.Equal(firstPlayer.PlayerId, grainState.Completion.WinnerPlayerId);
    }

    [Fact]
    public void ToDomainState_round_trips_pending_batch_details()
    {
        var firstPlayerId = Guid.NewGuid();
        var grainState = new GameSessionGrainState
        {
            FirstPlayer = new GameSessionParticipant(firstPlayerId),
            SecondPlayer = new GameSessionParticipant(Guid.NewGuid()),
            RoundNumber = 2,
            Phase = GamePhase.Play,
            FirstPlayerPendingBatch = new GameSessionPendingBatchGrainState
            {
                PlayerId = firstPlayerId,
                Cards =
                [
                    new GameSessionPendingBatchCardGrainState
                    {
                        Card = new GameSessionGrainCardState
                        {
                            CardInstanceId = Guid.NewGuid(),
                            Definition = GameCardDefinition.Produce,
                        },
                        CraftedCardDefinition = GameCardDefinition.Victory,
                        ConsumedCards =
                        [
                            new GameSessionCardReferenceGrainState
                            {
                                CardInstanceId = Guid.NewGuid(),
                            },
                            new GameSessionCardReferenceGrainState
                            {
                                ProducedByCardInstanceId = Guid.NewGuid(),
                                ProducedCardDefinition = GameCardDefinition.Red,
                            },
                        ],
                        ReturnToHand = true,
                    },
                ],
            },
        };

        var domainState = GameSessionGrainStateMapper.ToDomainState(grainState);

        Assert.Equal(2, domainState.RoundNumber);
        Assert.Equal(GamePhase.Play, domainState.Phase);
        Assert.NotNull(domainState.FirstPlayerPendingBatch);
        Assert.Equal(firstPlayerId, domainState.FirstPlayerPendingBatch!.PlayerId);
        Assert.Single(domainState.FirstPlayerPendingBatch.Cards);
        var card = domainState.FirstPlayerPendingBatch.Cards[0];
        Assert.Equal(GameCardDefinition.Produce, card.Card.Definition);
        Assert.Equal(GameCardDefinition.Victory, card.CraftedCardDefinition);
        Assert.Equal(2, card.ConsumedCards.Count);
        Assert.True(card.ReturnToHand);
    }
}

using Spx.Contracts;
using Spx.Game.Domain;
using Spx.Grains;
using Xunit;

namespace Spx.Grains.Tests;

public sealed class GameSessionGrainTests
{
    [Fact]
    public void FromDomainState_preserves_round_state_and_completion()
    {
        var firstPlayer = new GameSessionParticipantGrainView(Guid.NewGuid(), "user-1");
        var secondPlayer = new GameSessionParticipantGrainView(Guid.NewGuid(), "user-2");
        var domainState = new GameSessionState
        {
            FirstPlayer = firstPlayer,
            SecondPlayer = secondPlayer,
            RoundNumber = 4,
            Phase = GamePhase.Completed,
            FirstPlayerActive = true,
            SecondPlayerActive = false,
            CurrentAcquireFirstUserId = firstPlayer.UserId,
            CurrentAcquireSecondUserId = secondPlayer.UserId,
            FirstPlayerHand = [new GameCardState { CardInstanceId = Guid.NewGuid(), Definition = GameCardDefinition.Extract }],
            LastResolvedBatch = new ResolvedGameBatchState
            {
                RoundNumber = 3,
                ResolvedAtUtc = DateTime.UtcNow,
                Players =
                [
                    new ResolvedGamePlayerBatchState
                    {
                        UserId = firstPlayer.UserId,
                        Cards =
                        [
                            new PendingGameBatchCardState
                            {
                                Card = new GameCardState { CardInstanceId = Guid.NewGuid(), Definition = GameCardDefinition.Refine },
                                ConsumedCards = [new GameCardReferenceState { ProducedCardDefinition = GameCardDefinition.Red }]
                            }
                        ],
                        ProducedVictory = true
                    }
                ]
            },
            Completion = new GameCompletionState
            {
                Reason = GameCompletionReason.Victory,
                WinnerUserId = firstPlayer.UserId,
                CompletedAtUtc = DateTime.UtcNow
            }
        };

        var grainState = GameSessionGrainStateMapper.FromDomainState(domainState);

        Assert.Equal(domainState.RoundNumber, grainState.RoundNumber);
        Assert.Equal(domainState.Phase, grainState.Phase);
        Assert.Equal(domainState.FirstPlayer, grainState.FirstPlayer);
        Assert.Equal(domainState.SecondPlayer, grainState.SecondPlayer);
        Assert.Single(grainState.FirstPlayerHand);
        Assert.Equal(GameCardDefinition.Extract, grainState.FirstPlayerHand[0].Definition);
        Assert.NotNull(grainState.LastResolvedBatch);
        Assert.Single(grainState.LastResolvedBatch!.Players);
        Assert.Equal(firstPlayer.UserId, grainState.LastResolvedBatch.Players[0].UserId);
        Assert.True(grainState.LastResolvedBatch.Players[0].ProducedVictory);
        Assert.NotNull(grainState.Completion);
        Assert.Equal(GameCompletionReason.Victory, grainState.Completion!.Reason);
        Assert.Equal(firstPlayer.UserId, grainState.Completion.WinnerUserId);
    }

    [Fact]
    public void ToDomainState_round_trips_pending_batch_details()
    {
        var grainState = new GameSessionGrainState
        {
            FirstPlayer = new GameSessionParticipantGrainView(Guid.NewGuid(), "user-1"),
            SecondPlayer = new GameSessionParticipantGrainView(Guid.NewGuid(), "user-2"),
            RoundNumber = 2,
            Phase = GamePhase.Play,
            FirstPlayerPendingBatch = new GameSessionPendingBatchGrainState
            {
                UserId = "user-1",
                Cards =
                [
                    new GameSessionPendingBatchCardGrainState
                    {
                        Card = new GameSessionGrainCardState { CardInstanceId = Guid.NewGuid(), Definition = GameCardDefinition.Produce },
                        CraftedCardDefinition = GameCardDefinition.Victory,
                        ConsumedCards =
                        [
                            new GameSessionCardReferenceGrainState { CardInstanceId = Guid.NewGuid() },
                            new GameSessionCardReferenceGrainState { ProducedByCardInstanceId = Guid.NewGuid(), ProducedCardDefinition = GameCardDefinition.Red }
                        ],
                        ReturnToHand = true
                    }
                ]
            }
        };

        var domainState = GameSessionGrainStateMapper.ToDomainState(grainState);

        Assert.Equal(2, domainState.RoundNumber);
        Assert.Equal(GamePhase.Play, domainState.Phase);
        Assert.NotNull(domainState.FirstPlayerPendingBatch);
        Assert.Equal("user-1", domainState.FirstPlayerPendingBatch!.UserId);
        Assert.Single(domainState.FirstPlayerPendingBatch.Cards);
        var card = domainState.FirstPlayerPendingBatch.Cards[0];
        Assert.Equal(GameCardDefinition.Produce, card.Card.Definition);
        Assert.Equal(GameCardDefinition.Victory, card.CraftedCardDefinition);
        Assert.Equal(2, card.ConsumedCards.Count);
        Assert.True(card.ReturnToHand);
    }
}
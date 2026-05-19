using Spx.Game.Domain;
using Xunit;

namespace Spx.Game.Domain.Tests;

public sealed class GameSessionEngineTests
{
    private static readonly Guid GameId = Guid.Parse("6FD75A29-6B90-43AA-B97A-80A0C5210D73");
    private static readonly GameSessionParticipant FirstPlayer = new(
        Guid.Parse("0C8999C0-D4D2-46B5-B287-5D211CC99A40")
    );
    private static readonly GameSessionParticipant SecondPlayer = new(
        Guid.Parse("92C6775C-95F1-4C3B-9025-8E37D126CD4B")
    );
    private static readonly GameSessionParticipant ReplacementPlayer = new(
        Guid.Parse("D5985582-CF06-447D-A5AD-2F85B86B0AB7")
    );

    [Fact]
    public void Initialize_allows_same_roster_twice_without_resetting_progress()
    {
        var state = CreateInitializedState();
        state.Phase = GamePhase.Play;
        state.FirstPlayerHand.Add(CreateCard(GameCardDefinition.Extract));

        GameSessionEngine.Initialize(
            state,
            new InitializeGameSessionCommand(FirstPlayer, SecondPlayer)
        );

        Assert.Equal(GamePhase.Play, state.Phase);
        Assert.Single(state.FirstPlayerHand);
        Assert.Equal(GameCardDefinition.Extract, state.FirstPlayerHand[0].Definition);
        Assert.Equal(FirstPlayer, state.FirstPlayer);
        Assert.Equal(SecondPlayer, state.SecondPlayer);
    }

    [Fact]
    public void Initialize_resets_state_for_conflicting_roster()
    {
        var state = CreateInitializedState();
        state.Phase = GamePhase.Play;
        state.FirstPlayerHand.Add(CreateCard(GameCardDefinition.Extract));

        GameSessionEngine.Initialize(
            state,
            new InitializeGameSessionCommand(FirstPlayer, ReplacementPlayer)
        );

        Assert.Equal(GamePhase.Acquire, state.Phase);
        Assert.Empty(state.FirstPlayerHand);
        Assert.Equal(5, state.VisibleMarketCards.Count);
        Assert.Equal(FirstPlayer, state.FirstPlayer);
        Assert.Equal(ReplacementPlayer, state.SecondPlayer);
    }

    [Fact]
    public void SubmitAcquire_moves_market_card_into_hand_and_waits_for_second_picker()
    {
        var state = CreateInitializedState();
        var firstPicker =
            state.CurrentAcquireFirstPlayerId == FirstPlayer.PlayerId ? FirstPlayer : SecondPlayer;
        var pickedCard = state.VisibleMarketCards[0];

        var result = AssertSucceeded(
            GameSessionEngine.SubmitAcquire(
                state,
                GameId,
                new SubmitAcquireCommand(
                    firstPicker.PlayerId,
                    state.RoundNumber,
                    pickedCard.CardInstanceId
                )
            )
        );

        Assert.Equal(GamePhase.Acquire, result.Session.Phase);
        Assert.True(result.Session.WaitingForOpponent);
        Assert.Equal(4, state.VisibleMarketCards.Count);
        Assert.Contains(
            GetHand(state, firstPicker.PlayerId),
            card => card.CardInstanceId == pickedCard.CardInstanceId
        );
    }

    [Fact]
    public void SubmitAcquire_keeps_acquire_phase_after_both_players_finish_first_acquire_round()
    {
        var state = CreateInitializedState();
        var firstPickerPlayerId = state.CurrentAcquireFirstPlayerId!.Value;
        var secondPickerPlayerId = state.CurrentAcquireSecondPlayerId!.Value;

        AssertSucceeded(
            GameSessionEngine.SubmitAcquire(
                state,
                GameId,
                new SubmitAcquireCommand(
                    firstPickerPlayerId,
                    state.RoundNumber,
                    state.VisibleMarketCards[0].CardInstanceId
                )
            )
        );

        AssertSucceeded(
            GameSessionEngine.SubmitAcquire(
                state,
                GameId,
                new SubmitAcquireCommand(
                    secondPickerPlayerId,
                    state.RoundNumber,
                    state.VisibleMarketCards[0].CardInstanceId
                )
            )
        );

        var nextView = GameSessionEngine.GetSessionView(
            state,
            GameId,
            new GetGameSessionQuery(firstPickerPlayerId)
        );

        Assert.NotNull(nextView);
        Assert.Equal(GamePhase.Acquire, state.Phase);
        Assert.Equal(2, state.AcquirePicksCompletedInPhase);
        Assert.True(nextView!.CanAcquireCard);
        Assert.Equal(3, state.VisibleMarketCards.Count);
    }

    [Fact]
    public void SubmitAcquire_enters_play_phase_after_four_acquire_picks()
    {
        var state = CreateInitializedState();

        for (var pick = 0; pick < 4; pick++)
        {
            var currentPlayerId = GetCurrentAcquirePlayerId(state);
            AssertSucceeded(
                GameSessionEngine.SubmitAcquire(
                    state,
                    GameId,
                    new SubmitAcquireCommand(
                        currentPlayerId,
                        state.RoundNumber,
                        state.VisibleMarketCards[0].CardInstanceId
                    )
                )
            );
        }

        Assert.Equal(GamePhase.Play, state.Phase);
        Assert.Equal(4, state.AcquirePicksCompletedInPhase);
        Assert.Single(state.VisibleMarketCards);
    }

    [Fact]
    public void SubmitPlayBatch_consumes_extract_outputs_in_same_batch_for_refine()
    {
        var state = CreateInitializedState();
        var firstExtract = CreateCard(GameCardDefinition.Extract);
        var secondExtract = CreateCard(GameCardDefinition.Extract);
        var refine = CreateCard(GameCardDefinition.Refine);

        state.Phase = GamePhase.Play;
        state.FirstPlayerHand = [firstExtract, secondExtract, refine];
        state.SecondPlayerHand = [];

        AssertSucceeded(
            GameSessionEngine.SubmitPlayBatch(
                state,
                GameId,
                new SubmitPlayBatchCommand(
                    FirstPlayer.PlayerId,
                    state.RoundNumber,
                    [
                        new GameBatchCardCommand(
                            firstExtract.CardInstanceId,
                            GameResourceColor.Red,
                            null,
                            null,
                            null,
                            []
                        ),
                        new GameBatchCardCommand(
                            secondExtract.CardInstanceId,
                            GameResourceColor.Blue,
                            null,
                            null,
                            null,
                            []
                        ),
                        new GameBatchCardCommand(
                            refine.CardInstanceId,
                            null,
                            null,
                            null,
                            null,
                            [
                                new GameCardReferenceCommand(
                                    null,
                                    firstExtract.CardInstanceId,
                                    GameCardDefinition.Red
                                ),
                                new GameCardReferenceCommand(
                                    null,
                                    secondExtract.CardInstanceId,
                                    GameCardDefinition.Blue
                                ),
                            ]
                        ),
                    ]
                ),
                DateTime.UtcNow
            )
        );

        var secondPlayerResult = AssertSucceeded(
            GameSessionEngine.SubmitPlayBatch(
                state,
                GameId,
                new SubmitPlayBatchCommand(SecondPlayer.PlayerId, state.RoundNumber, []),
                DateTime.UtcNow
            )
        );

        Assert.Equal(2, secondPlayerResult.Session.RoundNumber);
        Assert.NotEmpty(secondPlayerResult.GameplayEvents);
        Assert.Contains(
            state.FirstPlayerHand,
            card => card.Definition == GameCardDefinition.Purple
        );
        Assert.Contains(state.MarketDeck, card => card.Definition == GameCardDefinition.Extract);
        Assert.Contains(state.MarketDeck, card => card.Definition == GameCardDefinition.Refine);
    }

    [Fact]
    public void SubmitPlayBatch_allows_incomplete_refine_and_fizzles_during_resolution()
    {
        var state = CreateInitializedState();
        var refine = CreateCard(GameCardDefinition.Refine);
        var red = CreateCard(GameCardDefinition.Red);

        state.Phase = GamePhase.Play;
        state.FirstPlayerHand = [refine, red];
        state.SecondPlayerHand = [];

        AssertSucceeded(
            GameSessionEngine.SubmitPlayBatch(
                state,
                GameId,
                new SubmitPlayBatchCommand(
                    FirstPlayer.PlayerId,
                    state.RoundNumber,
                    [
                        new GameBatchCardCommand(
                            refine.CardInstanceId,
                            null,
                            null,
                            null,
                            null,
                            [new GameCardReferenceCommand(red.CardInstanceId, null, null)]
                        ),
                    ]
                ),
                DateTime.UtcNow
            )
        );

        var secondPlayerResult = AssertSucceeded(
            GameSessionEngine.SubmitPlayBatch(
                state,
                GameId,
                new SubmitPlayBatchCommand(SecondPlayer.PlayerId, state.RoundNumber, []),
                DateTime.UtcNow
            )
        );

        Assert.Equal(2, secondPlayerResult.Session.RoundNumber);
        Assert.Contains(
            secondPlayerResult.GameplayEvents,
            entry =>
                entry.Kind == GameplayEventKind.Fizzled
                && entry.SourceCardDefinition == GameCardDefinition.Refine
        );
        Assert.Contains(state.FirstPlayerHand, card => card.CardInstanceId == red.CardInstanceId);
        Assert.DoesNotContain(
            state.FirstPlayerHand,
            card => card.Definition == GameCardDefinition.Purple
        );
    }

    [Fact]
    public void SubmitPlayBatch_rejects_refine_inputs_that_are_already_refined()
    {
        var state = CreateInitializedState();
        var refine = CreateCard(GameCardDefinition.Refine);
        var orange = CreateCard(GameCardDefinition.Orange);
        var blue = CreateCard(GameCardDefinition.Blue);

        state.Phase = GamePhase.Play;
        state.FirstPlayerHand = [refine, orange, blue];
        state.SecondPlayerHand = [];

        var rejected = Assert.IsType<GameSessionCommandRejectedResult>(
            GameSessionEngine.SubmitPlayBatch(
                state,
                GameId,
                new SubmitPlayBatchCommand(
                    FirstPlayer.PlayerId,
                    state.RoundNumber,
                    [
                        new GameBatchCardCommand(
                            refine.CardInstanceId,
                            null,
                            null,
                            null,
                            null,
                            [
                                new GameCardReferenceCommand(orange.CardInstanceId, null, null),
                                new GameCardReferenceCommand(blue.CardInstanceId, null, null),
                            ]
                        ),
                    ]
                ),
                DateTime.UtcNow
            )
        );

        Assert.Contains("base resource", rejected.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void SubmitPlayBatch_allows_incomplete_extract_and_fizzles_during_resolution()
    {
        var state = CreateInitializedState();
        var extract = CreateCard(GameCardDefinition.Extract);

        state.Phase = GamePhase.Play;
        state.FirstPlayerHand = [extract];
        state.SecondPlayerHand = [];

        AssertSucceeded(
            GameSessionEngine.SubmitPlayBatch(
                state,
                GameId,
                new SubmitPlayBatchCommand(
                    FirstPlayer.PlayerId,
                    state.RoundNumber,
                    [new GameBatchCardCommand(extract.CardInstanceId, null, null, null, null, [])]
                ),
                DateTime.UtcNow
            )
        );

        var secondPlayerResult = AssertSucceeded(
            GameSessionEngine.SubmitPlayBatch(
                state,
                GameId,
                new SubmitPlayBatchCommand(SecondPlayer.PlayerId, state.RoundNumber, []),
                DateTime.UtcNow
            )
        );

        Assert.Contains(
            secondPlayerResult.GameplayEvents,
            entry =>
                entry.Kind == GameplayEventKind.Fizzled
                && entry.SourceCardDefinition == GameCardDefinition.Extract
        );
        Assert.DoesNotContain(
            state.FirstPlayerHand,
            card => GameCardCatalog.IsBaseResource(card.Definition)
        );
    }

    [Fact]
    public void SubmitPlayBatch_allows_incomplete_produce_and_fizzles_during_resolution()
    {
        var state = CreateInitializedState();
        var produce = CreateCard(GameCardDefinition.Produce);

        state.Phase = GamePhase.Play;
        state.FirstPlayerHand = [produce];
        state.SecondPlayerHand = [];

        AssertSucceeded(
            GameSessionEngine.SubmitPlayBatch(
                state,
                GameId,
                new SubmitPlayBatchCommand(
                    FirstPlayer.PlayerId,
                    state.RoundNumber,
                    [new GameBatchCardCommand(produce.CardInstanceId, null, null, null, null, [])]
                ),
                DateTime.UtcNow
            )
        );

        var secondPlayerResult = AssertSucceeded(
            GameSessionEngine.SubmitPlayBatch(
                state,
                GameId,
                new SubmitPlayBatchCommand(SecondPlayer.PlayerId, state.RoundNumber, []),
                DateTime.UtcNow
            )
        );

        Assert.Contains(
            secondPlayerResult.GameplayEvents,
            entry =>
                entry.Kind == GameplayEventKind.Fizzled
                && entry.SourceCardDefinition == GameCardDefinition.Produce
        );
        Assert.DoesNotContain(
            state.FirstPlayerHand,
            card => card.Definition == GameCardDefinition.Victory
        );
    }

    [Fact]
    public void SubmitPlayBatch_allows_missing_sabotage_target_and_fizzles_during_resolution()
    {
        var state = CreateInitializedState();
        var sabotage = CreateCard(GameCardDefinition.Sabotage);

        state.Phase = GamePhase.Play;
        state.FirstPlayerHand = [sabotage];
        state.SecondPlayerHand = [];

        AssertSucceeded(
            GameSessionEngine.SubmitPlayBatch(
                state,
                GameId,
                new SubmitPlayBatchCommand(
                    FirstPlayer.PlayerId,
                    state.RoundNumber,
                    [new GameBatchCardCommand(sabotage.CardInstanceId, null, null, null, null, [])]
                ),
                DateTime.UtcNow
            )
        );

        var secondPlayerResult = AssertSucceeded(
            GameSessionEngine.SubmitPlayBatch(
                state,
                GameId,
                new SubmitPlayBatchCommand(SecondPlayer.PlayerId, state.RoundNumber, []),
                DateTime.UtcNow
            )
        );

        Assert.Contains(
            secondPlayerResult.GameplayEvents,
            entry =>
                entry.Kind == GameplayEventKind.Fizzled
                && entry.SourceCardDefinition == GameCardDefinition.Sabotage
        );
    }

    [Fact]
    public void SubmitPlayBatch_allows_missing_replicate_target_and_fizzles_during_resolution()
    {
        var state = CreateInitializedState();
        var replicate = CreateCard(GameCardDefinition.Replicate);

        state.Phase = GamePhase.Play;
        state.FirstPlayerHand = [replicate];
        state.SecondPlayerHand = [];

        AssertSucceeded(
            GameSessionEngine.SubmitPlayBatch(
                state,
                GameId,
                new SubmitPlayBatchCommand(
                    FirstPlayer.PlayerId,
                    state.RoundNumber,
                    [new GameBatchCardCommand(replicate.CardInstanceId, null, null, null, null, [])]
                ),
                DateTime.UtcNow
            )
        );

        var secondPlayerResult = AssertSucceeded(
            GameSessionEngine.SubmitPlayBatch(
                state,
                GameId,
                new SubmitPlayBatchCommand(SecondPlayer.PlayerId, state.RoundNumber, []),
                DateTime.UtcNow
            )
        );

        Assert.Contains(
            secondPlayerResult.GameplayEvents,
            entry =>
                entry.Kind == GameplayEventKind.Fizzled
                && entry.SourceCardDefinition == GameCardDefinition.Replicate
        );
    }

    [Fact]
    public void SubmitPlayBatch_allows_incomplete_catalyst_and_fizzles_during_resolution()
    {
        var state = CreateInitializedState();
        var catalyst = CreateCard(GameCardDefinition.Catalyst);
        var red = CreateCard(GameCardDefinition.Red);

        state.Phase = GamePhase.Play;
        state.FirstPlayerHand = [catalyst, red];
        state.SecondPlayerHand = [];

        AssertSucceeded(
            GameSessionEngine.SubmitPlayBatch(
                state,
                GameId,
                new SubmitPlayBatchCommand(
                    FirstPlayer.PlayerId,
                    state.RoundNumber,
                    [
                        new GameBatchCardCommand(
                            catalyst.CardInstanceId,
                            null,
                            null,
                            null,
                            red.CardInstanceId,
                            []
                        ),
                    ]
                ),
                DateTime.UtcNow
            )
        );

        var secondPlayerResult = AssertSucceeded(
            GameSessionEngine.SubmitPlayBatch(
                state,
                GameId,
                new SubmitPlayBatchCommand(SecondPlayer.PlayerId, state.RoundNumber, []),
                DateTime.UtcNow
            )
        );

        Assert.Contains(
            secondPlayerResult.GameplayEvents,
            entry =>
                entry.Kind == GameplayEventKind.Fizzled
                && entry.SourceCardDefinition == GameCardDefinition.Catalyst
        );
        Assert.Contains(state.FirstPlayerHand, card => card.CardInstanceId == red.CardInstanceId);
    }

    [Fact]
    public void SubmitPlayBatch_allows_missing_corrupt_target_and_fizzles_during_resolution()
    {
        var state = CreateInitializedState();
        var corrupt = CreateCard(GameCardDefinition.Corrupt);

        state.Phase = GamePhase.Play;
        state.FirstPlayerHand = [corrupt];
        state.SecondPlayerHand = [];

        AssertSucceeded(
            GameSessionEngine.SubmitPlayBatch(
                state,
                GameId,
                new SubmitPlayBatchCommand(
                    FirstPlayer.PlayerId,
                    state.RoundNumber,
                    [new GameBatchCardCommand(corrupt.CardInstanceId, null, null, null, null, [])]
                ),
                DateTime.UtcNow
            )
        );

        var secondPlayerResult = AssertSucceeded(
            GameSessionEngine.SubmitPlayBatch(
                state,
                GameId,
                new SubmitPlayBatchCommand(SecondPlayer.PlayerId, state.RoundNumber, []),
                DateTime.UtcNow
            )
        );

        Assert.Contains(
            secondPlayerResult.GameplayEvents,
            entry =>
                entry.Kind == GameplayEventKind.Fizzled
                && entry.SourceCardDefinition == GameCardDefinition.Corrupt
        );
    }

    [Fact]
    public void SubmitPlayBatch_allows_missing_reclaim_target_and_fizzles_during_resolution()
    {
        var state = CreateInitializedState();
        var reclaim = CreateCard(GameCardDefinition.Reclaim);

        state.Phase = GamePhase.Play;
        state.FirstPlayerHand = [reclaim];
        state.SecondPlayerHand = [];

        AssertSucceeded(
            GameSessionEngine.SubmitPlayBatch(
                state,
                GameId,
                new SubmitPlayBatchCommand(
                    FirstPlayer.PlayerId,
                    state.RoundNumber,
                    [new GameBatchCardCommand(reclaim.CardInstanceId, null, null, null, null, [])]
                ),
                DateTime.UtcNow
            )
        );

        var secondPlayerResult = AssertSucceeded(
            GameSessionEngine.SubmitPlayBatch(
                state,
                GameId,
                new SubmitPlayBatchCommand(SecondPlayer.PlayerId, state.RoundNumber, []),
                DateTime.UtcNow
            )
        );

        Assert.Contains(
            secondPlayerResult.GameplayEvents,
            entry =>
                entry.Kind == GameplayEventKind.Fizzled
                && entry.SourceCardDefinition == GameCardDefinition.Reclaim
        );
    }

    [Fact]
    public void SubmitPlayBatch_completes_match_when_victory_is_produced()
    {
        var state = CreateInitializedState();
        var produce = CreateCard(GameCardDefinition.Produce);
        var red = CreateCard(GameCardDefinition.Red);
        var yellow = CreateCard(GameCardDefinition.Yellow);
        var blue = CreateCard(GameCardDefinition.Blue);
        var purple = CreateCard(GameCardDefinition.Purple);
        var green = CreateCard(GameCardDefinition.Green);
        var orange = CreateCard(GameCardDefinition.Orange);

        state.Phase = GamePhase.Play;
        state.FirstPlayerHand = [produce, red, yellow, blue, purple, green, orange];
        state.SecondPlayerHand = [];

        AssertSucceeded(
            GameSessionEngine.SubmitPlayBatch(
                state,
                GameId,
                new SubmitPlayBatchCommand(
                    FirstPlayer.PlayerId,
                    state.RoundNumber,
                    [
                        new GameBatchCardCommand(
                            produce.CardInstanceId,
                            null,
                            GameCardDefinition.Victory,
                            null,
                            null,
                            [
                                new GameCardReferenceCommand(red.CardInstanceId, null, null),
                                new GameCardReferenceCommand(yellow.CardInstanceId, null, null),
                                new GameCardReferenceCommand(blue.CardInstanceId, null, null),
                                new GameCardReferenceCommand(purple.CardInstanceId, null, null),
                                new GameCardReferenceCommand(green.CardInstanceId, null, null),
                                new GameCardReferenceCommand(orange.CardInstanceId, null, null),
                            ]
                        ),
                    ]
                ),
                DateTime.UtcNow
            )
        );

        var secondPlayerResult = AssertSucceeded(
            GameSessionEngine.SubmitPlayBatch(
                state,
                GameId,
                new SubmitPlayBatchCommand(SecondPlayer.PlayerId, state.RoundNumber, []),
                DateTime.UtcNow
            )
        );

        Assert.Equal(GamePhase.Completed, secondPlayerResult.Session.Phase);
        Assert.NotNull(secondPlayerResult.Session.Completion);
        Assert.Equal(GameCompletionReason.Victory, secondPlayerResult.Session.Completion!.Reason);
        Assert.Equal(FirstPlayer.PlayerId, secondPlayerResult.Session.Completion.Winner!.PlayerId);
        Assert.NotEmpty(secondPlayerResult.GameplayEvents);
        Assert.Contains(
            state.FirstPlayerHand,
            card => card.Definition == GameCardDefinition.Victory
        );
    }

    private static GameSessionState CreateInitializedState()
    {
        var state = new GameSessionState();
        GameSessionEngine.Initialize(
            state,
            new InitializeGameSessionCommand(FirstPlayer, SecondPlayer)
        );
        return state;
    }

    private static GameSessionCommandSucceededResult AssertSucceeded(
        GameSessionCommandResult result
    ) => Assert.IsType<GameSessionCommandSucceededResult>(result);

    private static GameCardState CreateCard(GameCardDefinition definition) =>
        new() { CardInstanceId = Guid.NewGuid(), Definition = definition };

    private static List<GameCardState> GetHand(GameSessionState state, Guid playerId) =>
        playerId == FirstPlayer.PlayerId ? state.FirstPlayerHand : state.SecondPlayerHand;

    private static Guid GetCurrentAcquirePlayerId(GameSessionState state) =>
        state.AcquirePicksCompletedInPhase % 2 == 0
            ? state.CurrentAcquireFirstPlayerId!.Value
            : state.CurrentAcquireSecondPlayerId!.Value;
}

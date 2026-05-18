using Spx.Contracts;
using Spx.Game.Domain;

namespace Spx.Grains;

internal static class GameSessionGrainContractMapper
{
    public static InitializeGameSessionCommand ToDomain(InitializeGameSessionGrainCommand command)
        => new(ToDomain(command.FirstPlayer), ToDomain(command.SecondPlayer));

    public static SubmitAcquireCommand ToDomain(SubmitAcquireGrainCommand command)
        => new(command.PlayerId, command.ExpectedRoundNumber, command.MarketCardInstanceId);

    public static SubmitPlayBatchCommand ToDomain(SubmitPlayBatchGrainCommand command)
        => new(command.PlayerId, command.ExpectedRoundNumber, command.Cards.Select(ToDomain).ToArray());

    public static GetGameSessionQuery ToDomain(GetGameSessionGrainQuery query)
        => new(query.PlayerId);

    public static AbandonGameSessionCommand ToDomain(AbandonGameSessionGrainCommand command)
        => new(command.PlayerId);

    public static GameSessionGrainCommandResult ToContract(GameSessionCommandResult result, Guid? pendingGameplayEventBatchId = null)
        => result switch
        {
            GameSessionCommandSucceededResult succeeded => new GameSessionGrainCommandSucceededResult(
                ToContract(succeeded.Session),
                succeeded.GameplayEvents,
                pendingGameplayEventBatchId),
            GameSessionCommandRejectedResult rejected => new GameSessionGrainCommandRejectedResult(rejected.ErrorMessage),
            _ => throw new InvalidOperationException("Unknown session command result type.")
        };

    public static GameSessionGrainView ToContract(GameSessionView session)
        => new(
            session.GameId,
            session.RoundNumber,
            session.Phase,
            ToContract(session.CurrentPlayer),
            ToContract(session.OpponentPlayer),
            session.VisibleMarketCards.Select(ToContract).ToArray(),
            session.MarketDeckCount,
            session.WaitingForOpponent,
            session.CanAcquireCard,
            session.CanLockBatch,
            session.MaxBatchSize,
            session.LastResolvedBatch is null ? null : ToContract(session.LastResolvedBatch),
            session.Completion is null ? null : ToContract(session.Completion));

    private static GameSessionParticipant ToDomain(GameSessionParticipantGrainView participant)
        => new(participant.PlayerId);

    private static GameBatchCardCommand ToDomain(GameBatchCardGrainCommand command)
        => new(
            command.CardInstanceId,
            command.ChosenResourceColor,
            command.CraftedCardDefinition,
            command.TargetResourceColor,
            command.TargetCardInstanceId,
            command.ConsumedCards.Select(ToDomain).ToArray());

    private static GameCardReferenceCommand ToDomain(GameCardReferenceGrainCommand command)
        => new(command.CardInstanceId, command.ProducedByCardInstanceId, command.ProducedCardDefinition);

    private static GamePlayerStateGrainView ToContract(GamePlayerStateView player)
        => new(
            ToContract(player.Participant),
            player.Hand.Select(ToContract).ToArray(),
            player.HasLockedBatch,
            player.LockedBatchCount,
            player.InitiativeScore,
            player.HasScoutOverride,
            player.PicksFirstInAcquirePhase,
            player.VisibleLockedCards.Select(ToContract).ToArray());

    private static GameSessionParticipantGrainView ToContract(GameSessionParticipant participant)
        => new(participant.PlayerId);

    private static GameCardInstanceGrainView ToContract(GameCardView card)
        => new(card.CardInstanceId, card.Definition, card.DisplayName, card.Category, card.ResourceColor);

    private static GameBatchCardGrainView ToContract(GameBatchCardView card)
        => new(
            ToContract(card.Card),
            card.ChosenResourceColor,
            card.CraftedCardDefinition,
            card.TargetResourceColor,
            card.TargetCardInstanceId,
            card.ConsumedCards.Select(ToContract).ToArray());

    private static GameCardReferenceGrainView ToContract(GameCardReferenceView card)
        => new(card.CardInstanceId, card.ProducedByCardInstanceId, card.ProducedCardDefinition);

    internal static GameResolvedBatchGrainView ToContract(GameResolvedBatchView batch)
        => new(batch.RoundNumber, batch.Players.Select(ToContract).ToArray(), batch.ResolvedAtUtc);

    private static GameResolvedPlayerBatchGrainView ToContract(GameResolvedPlayerBatchView batch)
        => new(ToContract(batch.Participant), batch.PlayedCards.Select(ToContract).ToArray(), batch.ProducedVictory);

    internal static GameCompletionGrainView ToContract(GameCompletionView completion)
        => new(completion.Reason, completion.Winner is null ? null : ToContract(completion.Winner), completion.CompletedAtUtc);
}
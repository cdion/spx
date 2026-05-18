using Orleans;
using Orleans.Runtime;
using Spx.Contracts;
using Spx.Game.Domain;

namespace Spx.Grains;

public sealed class GameSessionGrain(
    [PersistentState("game-session")] IPersistentState<GameSessionGrainState> sessionState)
    : Grain, IGameSessionGrain
{
    public async Task InitializeAsync(InitializeGameSessionGrainCommand command)
    {
        var workingState = GameSessionGrainStateMapper.ToDomainState(sessionState.State);
        GameSessionEngine.Initialize(workingState, GameSessionGrainContractMapper.ToDomain(command));
        sessionState.State = GameSessionGrainStateMapper.FromDomainState(workingState, sessionState.State.PendingGameplayEventBatches);
        await sessionState.WriteStateAsync();
    }

    public async Task<GameSessionGrainCommandResult> SubmitAcquireAsync(SubmitAcquireGrainCommand command)
    {
        var workingState = GameSessionGrainStateMapper.ToDomainState(sessionState.State);
        var result = GameSessionEngine.SubmitAcquire(
            workingState,
            this.GetPrimaryKey(),
            GameSessionGrainContractMapper.ToDomain(command));

        if (result is GameSessionCommandSucceededResult)
        {
            sessionState.State = GameSessionGrainStateMapper.FromDomainState(workingState, sessionState.State.PendingGameplayEventBatches);
            await sessionState.WriteStateAsync();
        }

        return GameSessionGrainContractMapper.ToContract(result);
    }

    public async Task<GameSessionGrainCommandResult> SubmitPlayBatchAsync(SubmitPlayBatchGrainCommand command)
    {
        var workingState = GameSessionGrainStateMapper.ToDomainState(sessionState.State);
        var result = GameSessionEngine.SubmitPlayBatch(
            workingState,
            this.GetPrimaryKey(),
            GameSessionGrainContractMapper.ToDomain(command),
            DateTime.UtcNow);

        Guid? pendingGameplayEventBatchId = null;

        if (result is GameSessionCommandSucceededResult succeeded)
        {
            var mappedState = GameSessionGrainStateMapper.FromDomainState(workingState, sessionState.State.PendingGameplayEventBatches);
            if (succeeded.GameplayEvents.Count > 0)
            {
                pendingGameplayEventBatchId = Guid.NewGuid();
                mappedState.PendingGameplayEventBatches.Add(new PendingGameplayEventBatchGrainState
                {
                    BatchId = pendingGameplayEventBatchId.Value,
                    GameId = this.GetPrimaryKey(),
                    LastResolvedBatch = succeeded.Session.LastResolvedBatch is null ? null : GameSessionGrainContractMapper.ToContract(succeeded.Session.LastResolvedBatch),
                    Completion = succeeded.Session.Completion is null ? null : GameSessionGrainContractMapper.ToContract(succeeded.Session.Completion),
                    GameplayEvents = [.. succeeded.GameplayEvents]
                });
            }

            sessionState.State = mappedState;
            await sessionState.WriteStateAsync();
        }

        return GameSessionGrainContractMapper.ToContract(result, pendingGameplayEventBatchId);
    }

    public Task<GameSessionGrainView?> GetPlayerViewAsync(GetGameSessionGrainQuery query)
        => Task.FromResult(
            GameSessionEngine.GetSessionView(
                GameSessionGrainStateMapper.ToDomainState(sessionState.State),
                this.GetPrimaryKey(),
                GameSessionGrainContractMapper.ToDomain(query)) is { } view
                    ? GameSessionGrainContractMapper.ToContract(view)
                    : null);

    public Task<IReadOnlyList<PendingGameplayEventBatchGrainView>> GetPendingGameplayEventBatchesAsync()
        => Task.FromResult<IReadOnlyList<PendingGameplayEventBatchGrainView>>(
        [
            .. sessionState.State.PendingGameplayEventBatches.Select(batch => new PendingGameplayEventBatchGrainView(
                batch.BatchId,
                batch.GameId,
                batch.LastResolvedBatch,
                batch.Completion,
                [.. batch.GameplayEvents]))
        ]);

    public Task AcknowledgeGameplayEventBatchesAsync(AcknowledgeGameplayEventBatchesGrainCommand command)
    {
        var batchIds = command.BatchIds.ToHashSet();
        sessionState.State.PendingGameplayEventBatches.RemoveAll(batch => batchIds.Contains(batch.BatchId));
        return sessionState.WriteStateAsync();
    }

    public async Task<GameSessionGrainView> AbandonAsync(AbandonGameSessionGrainCommand command)
    {
        var workingState = GameSessionGrainStateMapper.ToDomainState(sessionState.State);
        var view = GameSessionEngine.AbandonPlayer(workingState, this.GetPrimaryKey(), GameSessionGrainContractMapper.ToDomain(command), DateTime.UtcNow);
        sessionState.State = GameSessionGrainStateMapper.FromDomainState(workingState, sessionState.State.PendingGameplayEventBatches);
        await sessionState.WriteStateAsync();
        return GameSessionGrainContractMapper.ToContract(view);
    }
}

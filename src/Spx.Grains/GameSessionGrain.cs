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
        GameSessionEngine.Initialize(workingState, command);
        sessionState.State = GameSessionGrainStateMapper.FromDomainState(workingState, sessionState.State.PendingGameplayEventBatches);
        await sessionState.WriteStateAsync();
    }

    public async Task<GameSessionGrainCommandResult> SubmitAcquireAsync(SubmitAcquireGrainCommand command)
    {
        var workingState = GameSessionGrainStateMapper.ToDomainState(sessionState.State);
        var result = GameSessionEngine.SubmitAcquire(
            workingState,
            this.GetPrimaryKey(),
            command);

        if (result is GameSessionGrainCommandSucceededResult)
        {
            sessionState.State = GameSessionGrainStateMapper.FromDomainState(workingState, sessionState.State.PendingGameplayEventBatches);
            await sessionState.WriteStateAsync();
        }

        return result;
    }

    public async Task<GameSessionGrainCommandResult> SubmitPlayBatchAsync(SubmitPlayBatchGrainCommand command)
    {
        var workingState = GameSessionGrainStateMapper.ToDomainState(sessionState.State);
        var result = GameSessionEngine.SubmitPlayBatch(
            workingState,
            this.GetPrimaryKey(),
            command,
            DateTime.UtcNow);

        if (result is GameSessionGrainCommandSucceededResult succeeded)
        {
            var mappedState = GameSessionGrainStateMapper.FromDomainState(workingState, sessionState.State.PendingGameplayEventBatches);
            if (succeeded.GameplayEvents.Count > 0)
            {
                var batchId = Guid.NewGuid();
                mappedState.PendingGameplayEventBatches.Add(new PendingGameplayEventBatchGrainState
                {
                    BatchId = batchId,
                    Session = succeeded.Session,
                    GameplayEvents = [.. succeeded.GameplayEvents]
                });

                result = succeeded with { PendingGameplayEventBatchId = batchId };
            }

            sessionState.State = mappedState;
            await sessionState.WriteStateAsync();
        }

        return result;
    }

    public Task<GameSessionGrainView?> GetPlayerViewAsync(GetGameSessionGrainQuery query)
        => Task.FromResult(GameSessionEngine.GetSessionView(GameSessionGrainStateMapper.ToDomainState(sessionState.State), this.GetPrimaryKey(), query));

    public Task<IReadOnlyList<PendingGameplayEventBatchGrainView>> GetPendingGameplayEventBatchesAsync()
        => Task.FromResult<IReadOnlyList<PendingGameplayEventBatchGrainView>>(
        [
            .. sessionState.State.PendingGameplayEventBatches.Select((Func<PendingGameplayEventBatchGrainState, PendingGameplayEventBatchGrainView>)(batch => (PendingGameplayEventBatchGrainView)new Contracts.PendingGameplayEventBatchGrainView(
                batch.BatchId,
                batch.Session,
                (IReadOnlyList<GameplayEvent>)[.. batch.GameplayEvents])))
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
        var view = GameSessionEngine.AbandonPlayer(workingState, this.GetPrimaryKey(), command, DateTime.UtcNow);
        sessionState.State = GameSessionGrainStateMapper.FromDomainState(workingState, sessionState.State.PendingGameplayEventBatches);
        await sessionState.WriteStateAsync();
        return view;
    }
}
